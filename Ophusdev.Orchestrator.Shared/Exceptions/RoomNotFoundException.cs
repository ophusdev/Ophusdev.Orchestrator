namespace Ophusdev.Orchestrator.Shared.Exceptions
{
    public class RoomNotFoundException: Exception
    {
        public RoomNotFoundException()
        {
        }

        public RoomNotFoundException(string message)
            : base(message)
        {
        }

        public RoomNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
