using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ophusdev.Kafka.Abstraction;
using Ophusdev.Orchestrator.Business.Abstraction;


namespace Ophusdev.Orchestrator.Business.Services
{
    public class SagaConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IKafkaConsumer _kafkaConsumer;
        private readonly ILogger<SagaConsumerService> _logger;
        private readonly IDictionary<string, IKafkaHandlerRegistry.RawKafkaMessageHandler> _topicHandlers;

        public SagaConsumerService(
            ITopicTranslator topicTranslator,
            IServiceProvider serviceProvider,
            IKafkaConsumer kafkaConsumer,
            ILogger<SagaConsumerService> logger,
            IKafkaHandlerRegistry handlerRegistry)
        {
            _serviceProvider = serviceProvider;
            _kafkaConsumer = kafkaConsumer;
            _logger = logger;
            _topicHandlers = handlerRegistry.GetHandlers();
        }

        private async Task ProcessKafkaMessageAsync(string topic, string message)
        {
            _logger.LogInformation("Received message on topic: {topic}", topic);

            // I need to create a new scope for each message processing to avoid the Dispose() of services
            using var scope = _serviceProvider.CreateScope();
            try
            {
                IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                // Look up the handler in the dictionary and invoke it
                if (_topicHandlers.TryGetValue(topic, out var handler))
                {
                    _logger.LogInformation("Found handler for topic: topic={topic}", topic);

                     await handler(bookingService, topic, message);
                }
                else
                {
                    _logger.LogWarning("No handler registered for topic: topic={topic}", topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message on topic: topic={topic}, message={message}", topic, message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string[] topicsToSubscribe = _topicHandlers.Keys.ToArray();

            try
            {
                _logger.LogInformation("Subscribing to Kafka topics: topics={topics}", string.Join(", ", topicsToSubscribe));

                await _kafkaConsumer.Subscribe(
                    topicsToSubscribe,
                    ProcessKafkaMessageAsync,
                    stoppingToken
                );

                _logger.LogInformation("Successfully subscribed to topics: topics={topics}", string.Join(", ", topicsToSubscribe));

                // TODO: capire se ci vuole
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SagaConsumerService is stopping due to cancellation.");
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: message={message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in SagaConsumerService: message={message}", ex.Message);
            }
        }
    }
}
