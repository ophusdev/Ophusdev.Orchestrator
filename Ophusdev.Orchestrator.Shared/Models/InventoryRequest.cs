namespace Ophusdev.Orchestrator.Shared.Models
{
    public class InventoryRequest
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestId { get; set; }
    }
}
