using Inventory.ClientHttp.Abstraction;
using Inventory.Shared;
using Microsoft.Extensions.Logging;
using Ophusdev.Kafka.Abstraction;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared;
using Ophusdev.Orchestrator.Shared.Exceptions;
using Ophusdev.Orchestrator.Shared.Models;
using Orchestrator.Repository.Abstraction;
using Orchestrator.Repository.Model;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class BookingService : IBookingService
    {
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IRepository _bookingRepository;
        private readonly ILogger<BookingService> _logger;
        private readonly IClientHttp _clientHttp;
        private readonly ITopicTranslator _topicTranslator;

        public BookingService(
            IKafkaProducer kafkaProducer,
            IRepository bookingRepository,
            ILogger<BookingService> logger,
            ITopicTranslator topicTranslator,
            IClientHttp clientHttp)
        {
            _kafkaProducer = kafkaProducer;
            _bookingRepository = bookingRepository;
            _logger = logger;
            _clientHttp = clientHttp;
            _topicTranslator = topicTranslator;
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

        public async Task<BookingResponse> CreateBookingAsync(BookingInsertDto request)
        {
            string sagaId = Guid.NewGuid().ToString();

            RoomDto? room = await ReadRoomSync(request.RoomId);

            if (room == null)
            {
                throw new RoomNotFoundException("Room is not available");
            }

            BookingItem booking = await _bookingRepository.CreateBookingAsync(sagaId, request);
            await _bookingRepository.SaveChangesAsync();

            _logger.LogInformation("Create saga transaction, sagaId={sagaId}", sagaId);

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

            return new BookingResponse
            {
                BookingId = booking.BookingId,
                Status = booking.Status,
                Message = "Booking initiated successfully"
            };
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
            }
            else
            {
                _logger.LogWarning("Reserve room failed, abort booking");

                booking.Status = BookingStatus.Failed;
                await _bookingRepository.UpdateAsync(booking);
            }
        }

        public async Task ProcessPaymentRequestAsync(PaymentResponse message)
        {
             var booking = await _bookingRepository.GetByBookingIdAsync(message.BookingId);

            if (booking == null) return;

            if (message.Success)
            {
                // TODO: chiamare un altro servizio per scopi didattici
                _logger.LogInformation("Payment success, book room={roomId}", booking.RoomId);

                booking.Status = BookingStatus.Paid;
                
                await _bookingRepository.UpdateAsync(booking);
            }
            else
            {
                _logger.LogWarning("Payment failed, abort booking");

                booking.Status = BookingStatus.Failed_Payment;
                
                await _bookingRepository.UpdateAsync(booking);

                _logger.LogWarning("Payment failed, start compensation on inventory");

                await CompensateInventoryAsync(booking.SagaId, message.BookingId);
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
    }
}
