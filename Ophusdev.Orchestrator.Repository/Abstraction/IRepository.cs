using Ophusdev.Orchestrator.Shared;
using Orchestrator.Repository.Model;

namespace Orchestrator.Repository.Abstraction;

public interface IRepository
{
    Task<int> SaveChangesAsync();
    Task<BookingItem> CreateBookingAsync(string sagaId, BookingInsertDto request, CancellationToken cancellationToken = default);
    Task<BookingItem> GetByBookingIdAsync(string bookingId, CancellationToken cancellationToken = default);
    Task UpdateAsync(BookingItem booking, CancellationToken cancellationToken = default);
}
