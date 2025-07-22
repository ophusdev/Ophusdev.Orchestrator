using Ophusdev.Orchestrator.Shared;
using Ophusdev.Orchestrator.Shared.Models;
using Orchestrator.Repository.Model;

namespace Ophusdev.Orchestrator.Business.Abstraction
{
    public interface IBookingService
    {
        Task<BookingResponse> CreateBookingAsync(BookingInsertDto request, CancellationToken cancellationToken = default);
        Task<BookingItem> GetBookingAsync(string bookingId);
        Task ProcessInventoryRequestAsync(InventoryResponse message);
        Task ProcessPaymentRequestAsync(PaymentResponse message);
        Task CompleteSaga(BookingResponseSaga message);
    }
}