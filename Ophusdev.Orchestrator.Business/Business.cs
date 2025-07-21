using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared;
using Orchestrator.Repository.Model;

namespace Ophusdev.Orchestrator.Business
{
    public class Business(IBookingService bookingService) : IBusiness
    {
        public async Task<BookingResponse> CreateBookingSaga(BookingInsertDto bookingDto, CancellationToken cancellationToken = default)
        {
            return await bookingService.CreateBookingAsync(bookingDto);
        }

        public async Task<BookingResponse> GetBookingStatus(string bookingId, CancellationToken cancellationToken = default)
        {
            BookingItem booking = await bookingService.GetBookingAsync(bookingId);

            return new BookingResponse { BookingId = booking.BookingId, Status = booking.Status, Message = "" };
        }
    }
}
