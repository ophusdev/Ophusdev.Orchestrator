using Microsoft.Extensions.Logging;
using Ophusdev.Kafka.Abstraction;
using Ophusdev.Kafka.Extensions;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared;
using Ophusdev.Orchestrator.Shared.Models;
using Orchestrator.Repository.Abstraction;
using Orchestrator.Repository.Model;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class BookingSagaOrchestrator : IBookingSagaOrchestrator
    {
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IRepository _bookingRepository;
        private readonly ILogger _logger;
        private readonly ITopicTranslator _topicTranslator;

        public BookingSagaOrchestrator(IKafkaProducer kafkaProducer,
            IRepository bookingRepository,
            ILogger<BookingSagaOrchestrator> logger,
            ITopicTranslator topicTranslator)
        {
            _kafkaProducer = kafkaProducer;
            _bookingRepository = bookingRepository;
            _logger = logger;
            _topicTranslator = topicTranslator;
        }

        public async Task StartSagaAsync(string SagaId, BookingItem request)
        {
            var inventoryRequest = new InventoryRequest
            {
                SagaId = SagaId,
                BookingId = request.BookingId,
                RoomId = request.RoomId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                GuestId = request.GuestId
            };

            string topicName = _topicTranslator.GetTopicName("TOPIC_INVENTORY_REQUEST");

            await _kafkaProducer.ProduceAsync(topicName, inventoryRequest);
        }

        public async Task HandleInventoryResponseAsync(InventoryResponse message)
        {
            //var response = JsonSerializer.Deserialize<InventoryResponse>(message);

            var booking = await _bookingRepository.GetByBookingIdAsync(message.BookingId);
           
            if (booking == null) return;

            if (message.Success)
            {
                booking.Status = BookingStatus.InventoryConfirmed;
                await _bookingRepository.UpdateAsync(booking);

                var paymentRequest = new PaymentRequest
                {
                    SagaId = message.SagaId,
                    BookingId = message.BookingId,
                    GuestId = booking.GuestId,
                    Amount = 200, // TODO: calculate it
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

        public async Task HandlePaymentResponseAsync(PaymentResponse message)
        {
            //var response = JsonSerializer.Deserialize<PaymentResponse>(message);

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
