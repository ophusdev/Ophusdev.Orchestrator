using Booking.Repository;
using Microsoft.EntityFrameworkCore;
using Ophusdev.Orchestrator.Shared;
using Orchestrator.Repository.Abstraction;
using Orchestrator.Repository.Model;

namespace Orchestrator.Repository;

public class Repository(BookingDbContext reservationDbContext) : IRepository
{
    public async Task<int> SaveChangesAsync()
    {
        return await reservationDbContext.SaveChangesAsync();
    }

    public async Task<BookingItem> CreateBookingAsync(string sagaId, BookingInsertDto request, CancellationToken cancellationToken = default)
    {
        BookingItem booking = new BookingItem
        {
            SagaId = sagaId,
            BookingId = Guid.NewGuid().ToString(),
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            CreditCardNumber = request.CreditCardNumber,
            RoomId = request.RoomId,
            GuestId = request.GuestId,
            Status = BookingStatus.Pending
        };

        await reservationDbContext.Bookings.AddAsync(booking, cancellationToken);

        return booking;
    }

    public async Task<BookingItem> GetByBookingIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        return await reservationDbContext.Bookings.Where(b => b.BookingId == bookingId).FirstAsync();
    }

    public async Task UpdateAsync(BookingItem booking, CancellationToken cancellationToken = default)
    {
        reservationDbContext.Bookings.Update(booking);
        await reservationDbContext.SaveChangesAsync();
    }
}
