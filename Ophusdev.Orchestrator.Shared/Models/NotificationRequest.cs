namespace Ophusdev.Orchestrator.Shared.Models
{
    public class NotificationRequest
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public int GuestId { get; set; }
    }
}
