namespace Ophusdev.Orchestrator.Shared.Models
{
    public class PaymentRequest
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public int GuestId { get; set; }
        public decimal Amount { get; set; }
        public required string CreditCardNumber { get; set; }
    }
}
