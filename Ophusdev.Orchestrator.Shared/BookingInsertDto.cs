using System.ComponentModel.DataAnnotations;

namespace Ophusdev.Orchestrator.Shared
{
    public class BookingInsertDto
    {
        [Required]
        public DateTime CheckInDate { get; set; }
        [Required]
        public DateTime CheckOutDate { get; set; }
        [Required]
        public required string CreditCardNumber { get; set; }
        [Required]
        public int RoomId { get; set; }
        [Required]
        public int GuestId { get; set; }
    }
}
