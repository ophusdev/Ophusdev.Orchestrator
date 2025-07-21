using Microsoft.Extensions.Logging;
using Ophusdev.Kafka.Abstraction;
using Ophusdev.Orchestrator.Business.Abstraction;
using System.Text.Json;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class KafkaHandlerRegistry : IKafkaHandlerRegistry
    {
        private readonly Dictionary<string, IKafkaHandlerRegistry.RawKafkaMessageHandler> _handlers = new Dictionary<string, IKafkaHandlerRegistry.RawKafkaMessageHandler>();
        private readonly ITopicTranslator _topicTranslator;
        private readonly ILogger<KafkaHandlerRegistry> _logger;

        public KafkaHandlerRegistry(ITopicTranslator topicTranslator, ILogger<KafkaHandlerRegistry> logger)
        {
            _topicTranslator = topicTranslator;
            _logger = logger;
        }

        public void RegisterTypedHandler<TMessage>(string topicKey, Func<IBookingService, TMessage, Task> handler)
        {
            string topicName = _topicTranslator.GetTopicName(topicKey);

            IKafkaHandlerRegistry.RawKafkaMessageHandler rawHandler = async (bookingService, topic, message) =>
            {
                try
                {
                    var typedMessage = JsonSerializer.Deserialize<TMessage>(message);
                    if (typedMessage != null)
                    {
                        await handler(bookingService, typedMessage);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message, topic: topic={topic}, type={messageType}, message={message}}",
                            topic, typeof(TMessage).Name, message);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error processing message, topic: topic={topic}, type={messageType}, message={message}",
                        topic, typeof(TMessage).Name, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message, topic: topic={topic}, type={messageType}, message={message}",
                        topic, typeof(TMessage).Name, message);
                }
            };

            if (_handlers.ContainsKey(topicName))
            {
                _logger.LogWarning("Overwriting handler: topic={topicName}.", topicName);
                _handlers[topicName] = rawHandler;
            }
            else
            {
                _handlers.Add(topicName, rawHandler);
            }
        }

        public IDictionary<string, IKafkaHandlerRegistry.RawKafkaMessageHandler> GetHandlers()
        {
            return _handlers;
        }
    }
}