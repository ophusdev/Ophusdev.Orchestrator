using Microsoft.Extensions.Logging;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared;
using Ophusdev.Orchestrator.Shared.Models;
using Orchestrator.Repository.Abstraction;
using Orchestrator.Repository.Model;

using Inventory.ClientHttp.Abstraction;
using Inventory.Shared;
using Ophusdev.Orchestrator.Shared.Exceptions;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class BookingService : IBookingService
    {
        private readonly IRepository _bookingRepository;
        private readonly IBookingSagaOrchestrator _sagaOrchestrator;
        private readonly ILogger<BookingService> _logger;
        private readonly IClientHttp _clientHttp;

        public BookingService(IRepository bookingRepository,
            IBookingSagaOrchestrator sagaOrchestrator,
            ILogger<BookingService> logger,
            IClientHttp clientHttp)
        {
            _bookingRepository = bookingRepository;
            _sagaOrchestrator = sagaOrchestrator;
            _logger = logger;
            _clientHttp = clientHttp;
        }

        private async Task<bool> ExistsRoom(int roomId)
        {
            try
            {
                RoomDto? room = await _clientHttp.ReadRoomAsync(roomId);

                if (room != null)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error to retrive info about room: room={room}, message={message}", roomId, e.Message);
            }

            return false;
        }

        public async Task<BookingResponse> CreateBookingAsync(BookingInsertDto request)
        {
            string sagaId = Guid.NewGuid().ToString();

            if (! await ExistsRoom(request.RoomId))
            {
                throw new RoomNotFoundException("Room is not available");
            }

            BookingItem booking = await _bookingRepository.CreateBookingAsync(sagaId, request);
            await _bookingRepository.SaveChangesAsync();

            _logger.LogInformation("Create saga transaction, sagaId={sagaId}", sagaId);

            await _sagaOrchestrator.StartSagaAsync(sagaId, booking);

            return new BookingResponse
            {
                BookingId = booking.BookingId,
                Status = booking.Status,
                Message = "Booking initiated successfully"
            };
        }

        public async Task ProcessInventoryRequestAsync(InventoryResponse message)
        {
            await _sagaOrchestrator.HandleInventoryResponseAsync(message);
        }

        public async Task ProcessPaymentRequestAsync(PaymentResponse message)
        {
            await _sagaOrchestrator.HandlePaymentResponseAsync(message);
        }

        public async Task<BookingItem> GetBookingAsync(string bookingId)
        {
            return await _bookingRepository.GetByBookingIdAsync(bookingId);
        }
    }
}
