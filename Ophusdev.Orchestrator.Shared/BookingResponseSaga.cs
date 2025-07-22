namespace Ophusdev.Orchestrator.Shared
{
    public class BookingResponseSaga
    {
        public required string SagaId { get; set; }
        public required string BookingId { get; set; }
        public BookingStatus Status { get; set; }
        public required string Message { get; set; }
    }
}
