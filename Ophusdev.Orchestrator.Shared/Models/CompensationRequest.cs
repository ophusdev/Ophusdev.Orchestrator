namespace Ophusdev.Orchestrator.Shared.Models
{
    public class CompensationRequest
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public CompensationType Type { get; set; }
    }

    public enum CompensationType
    {
        Inventory,
        Reservation,
        Payment
    }
}
