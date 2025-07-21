using System.ComponentModel.DataAnnotations;

namespace Ophusdev.Orchestrator.Shared
{
    public enum BookingStatus
    {
        [Display(Name = "Pending")]
        Pending,
        [Display(Name = "InventoryConfirmed")]
        InventoryConfirmed,
        [Display(Name = "Paid")]
        Paid,
        [Display(Name = "Failed Payment")]
        Failed_Payment,
        [Display(Name = "Completed")]
        Completed,
        [Display(Name = "Failed")]
        Failed
    }

    public class BookingDto
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string FiscalCode { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public required string CreditCardNumber { get; set; }
        public int RoomId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int GuestId { get; set; }
    }
}
