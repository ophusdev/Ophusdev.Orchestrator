namespace Ophusdev.Orchestrator.Business.Abstraction
{
    public interface IKafkaHandlerRegistry
    {
        // This delegate will be used internally by the registry to store the full handler.
        // It takes thea IBookingService instance, the raw topic string, and the raw message string.
        public delegate Task RawKafkaMessageHandler(IBookingService bookingService, string topic, string message);

        IDictionary<string, RawKafkaMessageHandler> GetHandlers();

        void RegisterTypedHandler<TMessage>(string topicKey, Func<IBookingService, TMessage, Task> handler);
    }
}
