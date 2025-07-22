using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared.Models;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class NotificationWorker : BackgroundService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationWorker> _logger;
        private readonly int _queueTimeout = 15;

        public NotificationWorker(
            INotificationService notificationService, 
            ILogger<NotificationWorker> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    List<NotificationRequest> notificationToRetry = _notificationService.GetNotificationToRetry();

                    for (int i = 0; i < notificationToRetry.Count; i++)
                    {
                        NotificationRequest notification = notificationToRetry[i];

                        _logger.LogInformation("Try to notify again: bookingId={bookingId}", notification.BookingId);

                        _notificationService.ProduceAsyncSimulated(notification);

                        _notificationService.RemoveNotificationToRetry(notification.BookingId);
                    }

                    _logger.LogInformation("sleep for {_queueTimeout} seconds", _queueTimeout);

                    await Task.Delay(_queueTimeout * 1000);
                }
            }, stoppingToken);
        }
    }
}
