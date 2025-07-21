using Ophusdev.Kafka.Abstraction;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Business.Services;
using Ophusdev.Orchestrator.Shared.Models;

namespace Ophusdev.Orchestrator.Api.Extensions
{
    public static class AddKafkaTopicsExtensions
    {
        public static IServiceCollection AddKafkaTopicHandlers(this IServiceCollection services)
        {
            services.AddSingleton<IKafkaHandlerRegistry>(sp =>
            {
                var topicTranslator = sp.GetRequiredService<ITopicTranslator>();
                var loggerRegistry = sp.GetRequiredService<ILogger<KafkaHandlerRegistry>>(); 
                var registry = new KafkaHandlerRegistry(topicTranslator, loggerRegistry); 

                var sagaConsumerLogger = sp.GetRequiredService<ILogger<SagaConsumerService>>();

                registry.RegisterTypedHandler<InventoryResponse>(
                    "TOPIC_INVENTORY_RESPONSE",
                    async (bookingSagaOrchestrator, response) =>
                    {
                        await bookingSagaOrchestrator.ProcessInventoryRequestAsync(response);
                    }
                );

                registry.RegisterTypedHandler<PaymentResponse>(
                    "TOPIC_PAYMENT_RESPONSE",
                    async (bookingSagaOrchestrator, response) =>
                    {
                        await bookingSagaOrchestrator.ProcessPaymentRequestAsync(response);
                    }
                );

                return registry;
            });

            return services;
        }
    }
}
