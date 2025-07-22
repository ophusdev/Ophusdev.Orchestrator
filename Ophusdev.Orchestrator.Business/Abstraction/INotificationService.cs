using Ophusdev.Orchestrator.Shared.Models;

namespace Ophusdev.Orchestrator.Business.Abstraction
{
    public interface INotificationService
    {
        /// <summary>
        /// This class simulate the other microservice deployed.
        /// For simplicity we don't deploy a real service but simulate it
        /// </summary>
        /// <param name="message">message received</param>
        /// <returns></returns>
        void ProduceAsyncSimulated(NotificationRequest message);

        /// <summary>
        /// get the notifiction in errors.
        /// Simulate the query on db using a local dictionary
        /// </summary>
        /// <returns></returns>
        public List<NotificationRequest> GetNotificationToRetry();

        public bool RemoveNotificationToRetry(string bookingId);
        
    }
}
