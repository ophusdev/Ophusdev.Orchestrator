namespace Ophusdev.Orchestrator.Shared.Models
{
    public class InventoryResponse
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public int RoomId { get; set; }
        public bool Success { get; set; }
    }
}
