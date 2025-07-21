namespace Ophusdev.Orchestrator.Shared.Models
{
    public class PaymentResponse
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public bool Success { get; set; }
    }
}
