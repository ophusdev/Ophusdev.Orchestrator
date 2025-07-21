using Ophusdev.Orchestrator.Shared.Models;
using Orchestrator.Repository.Model;

namespace Ophusdev.Orchestrator.Business.Abstraction
{
	public interface IBookingSagaOrchestrator
	{
		Task StartSagaAsync(string SagaId, BookingItem request);
		Task HandleInventoryResponseAsync(InventoryResponse message);
		Task HandlePaymentResponseAsync(PaymentResponse message);

		//Task HandleNotificationResponseAsync(NotificationResponse response);
	}
}