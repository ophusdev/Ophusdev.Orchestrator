﻿namespace Ophusdev.Orchestrator.Shared
{
    public class BookingResponse
    {
        public  string? BookingId { get; set; }
        public BookingStatus Status { get; set; }
        public required string Message { get; set; }
    }
}
