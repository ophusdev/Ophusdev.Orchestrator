using Ophusdev.Orchestrator.Shared;

namespace Ophusdev.Orchestrator.Business.Abstraction
{
    public interface IBusiness
    {
        Task<BookingResponse> CreateBookingSaga(BookingInsertDto bookingDto, CancellationToken cancellationToken = default);
        Task<BookingResponse> GetBookingStatus(string bookingId, CancellationToken cancellationToken = default);
    }
}
