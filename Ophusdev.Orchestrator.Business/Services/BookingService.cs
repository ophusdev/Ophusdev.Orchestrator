using Inventory.ClientHttp.Abstraction;
using Inventory.Shared;
using Microsoft.Extensions.Logging;
using Ophusdev.Kafka.Abstraction;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared;
using Ophusdev.Orchestrator.Shared.Models;
using Orchestrator.Repository.Abstraction;
using Orchestrator.Repository.Model;
using System.Collections.Concurrent;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class BookingService : IBookingService
    {
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IRepository _bookingRepository;
        private readonly ILogger<BookingService> _logger;
        private readonly IClientHttp _clientHttp;
        private readonly ITopicTranslator _topicTranslator;
        private readonly INotificationService _notificationService;
        private readonly int _taskDelay = 60;

        private static readonly ConcurrentDictionary<string, TaskCompletionSource<BookingResponse>> _completionSources = new();

        public BookingService(
            IKafkaProducer kafkaProducer,
            IRepository bookingRepository,
            ILogger<BookingService> logger,
            ITopicTranslator topicTranslator,
            IClientHttp clientHttp,
            INotificationService notificationService)
        {
            _kafkaProducer = kafkaProducer;
            _bookingRepository = bookingRepository;
            _logger = logger;
            _clientHttp = clientHttp;
            _topicTranslator = topicTranslator;
            _notificationService = notificationService;
        }

        private async Task<RoomDto?> ReadRoomSync(int roomId)
        {
            try
            {
                RoomDto? room = await _clientHttp.ReadRoomAsync(roomId);

                return room;
            }
            catch (Exception e)
            {
                _logger.LogError("Error to retrive info about room: room={room}, message={message}", roomId, e.Message);
            }

            return null;
        }

        public async Task<BookingResponse> CreateBookingAsync(BookingInsertDto request, CancellationToken cancellationToken = default)
        {
            string sagaId = Guid.NewGuid().ToString();

            RoomDto? room = await ReadRoomSync(request.RoomId);

            if (room == null)
            {
                return new BookingResponse { BookingId = null, Status = BookingStatus.Failed, Message = "Room not found" };
            }

            BookingItem booking = await _bookingRepository.CreateBookingAsync(sagaId, request);
            await _bookingRepository.SaveChangesAsync();

            var tcs = new TaskCompletionSource<BookingResponse>();

            _completionSources.TryAdd(sagaId, tcs);

            _logger.LogInformation("Create saga transaction, sagaId={sagaId}", sagaId);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_taskDelay));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var inventoryRequest = new InventoryRequest
                {
                    SagaId = sagaId,
                    BookingId = booking.BookingId,
                    RoomId = booking.RoomId,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    GuestId = booking.GuestId
                };

                string topicName = _topicTranslator.GetTopicName("TOPIC_INVENTORY_REQUEST");

                await _kafkaProducer.ProduceAsync(topicName, inventoryRequest);

                await using (linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token)))
                {
                    var response = await tcs.Task; // This will await until TrySetResult or TrySetCanceled is called
                    _logger.LogInformation("Saga completed: sagaId={sagaId}, status={sagaId}", sagaId, response.Status);
                    return response;
                }
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("Saga timeout: sagaId={sagaId}", sagaId);
                    return new BookingResponse { BookingId = booking.BookingId, Status =BookingStatus.Failed, Message = "Booking saga timed out" };
                }
                throw;
            }
            finally
            {
                // Clean up the completion source, regardless of outcome
                _completionSources.TryRemove(sagaId, out _);
            }
        }

        public async Task ProcessInventoryRequestAsync(InventoryResponse message)
        {
            var booking = await _bookingRepository.GetByBookingIdAsync(message.BookingId);

            if (booking == null) return;

            if (message.Success)
            {
                booking.Status = BookingStatus.InventoryConfirmed;
                await _bookingRepository.UpdateAsync(booking);

                RoomDto? room = await ReadRoomSync(booking.RoomId);

                // We have a strange business in our hotel
                decimal pricePerNight = room.PricePerNight * (decimal)1.5;

                var paymentRequest = new PaymentRequest
                {
                    SagaId = message.SagaId,
                    BookingId = message.BookingId,
                    GuestId = booking.GuestId,
                    Amount = pricePerNight,
                    CreditCardNumber = booking.CreditCardNumber
                };

                string topicName = _topicTranslator.GetTopicName("TOPIC_PAYMENT_REQUEST");

                await _kafkaProducer.ProduceAsync(topicName, paymentRequest);

                // Here we don't notify saga because need to wait for Payment
            }
            else
            {
                _logger.LogWarning("Reserve room failed, abort booking");

                booking.Status = BookingStatus.Failed;
                await _bookingRepository.UpdateAsync(booking);

                await NotifySagaAsync(message.SagaId, message.BookingId, booking.Status, "");
            }
        }

        public async Task ProcessPaymentRequestAsync(PaymentResponse message)
        {
             var booking = await _bookingRepository.GetByBookingIdAsync(message.BookingId);

            if (booking == null) return;

            if (message.Success)
            {
                _logger.LogInformation("Payment success, book room={roomId}", booking.RoomId);

                booking.Status = BookingStatus.Paid;
                
                await _bookingRepository.UpdateAsync(booking);

                await NotifySagaAsync(message.SagaId, message.BookingId, booking.Status, "");

                _notificationService.ProduceAsyncSimulated(new NotificationRequest
                {
                    SagaId = message.SagaId,
                    BookingId = message.BookingId,
                    GuestId = booking.GuestId,
                });
            }
            else
            {
                _logger.LogWarning("Payment failed, abort booking");

                booking.Status = BookingStatus.Failed_Payment;
                
                await _bookingRepository.UpdateAsync(booking);

                _logger.LogWarning("Payment failed, start compensation on inventory");

                await CompensateInventoryAsync(booking.SagaId, message.BookingId);

                await NotifySagaAsync(message.SagaId, message.BookingId, booking.Status, "");

            }
        }

        public async Task<BookingItem> GetBookingAsync(string bookingId)
        {
            return await _bookingRepository.GetByBookingIdAsync(bookingId);
        }

        private async Task CompensateInventoryAsync(string sagaId, string bookingId)
        {
            var compensationRequest = new CompensationRequest
            {
                SagaId = bookingId,
                BookingId = bookingId,
                Type = CompensationType.Inventory
            };

            string topicName = _topicTranslator.GetTopicName("TOPIC_COMPENSATION_REQUEST");

            await _kafkaProducer.ProduceAsync(topicName, compensationRequest);
        }

        public async Task CompleteSaga(BookingResponseSaga message)
        {
            if (_completionSources.TryGetValue(message.SagaId, out var tcs))
            {
                BookingResponse response = new BookingResponse
                {
                    BookingId = message.BookingId,
                    Status = message.Status,
                    Message = message.Message
                };

                tcs.TrySetResult(response);

                _logger.LogInformation("Completed tcs: sagaId={sagaId}", message.SagaId);
            }

            _logger.LogInformation("Not found tcs: sagaId={sagaId}", message.SagaId);
        }

        private async Task NotifySagaAsync(string sagaId, string bookingId, BookingStatus status, string message = "")
        {
            string topicName = _topicTranslator.GetTopicName("TOPIC_SAGA_RESPONSE");

            await _kafkaProducer.ProduceAsync(topicName, new BookingResponseSaga
            {
                SagaId = sagaId,
                BookingId = bookingId,
                Status = status,
                Message = message
            });
        }
    }
}
