using Ophusdev.Orchestrator.Shared;

namespace Orchestrator.Repository.Model;


public class BookingItem
{
    public int Id { get; set; }
    public required string BookingId { get; set; }
    public required string SagaId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public required string CreditCardNumber{ get; set; }
    public int RoomId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int GuestId { get; set; }
}

