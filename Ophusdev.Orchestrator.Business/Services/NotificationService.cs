using Microsoft.Extensions.Logging;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared.Models;
using System.Collections.Generic;

namespace Ophusdev.Orchestrator.Business.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<BookingService> _logger;
        private readonly Random _random = new Random();
        private readonly Dictionary<string, NotificationRequest> _firstAttemptFailed = new Dictionary<string, NotificationRequest>();

        public NotificationService(
            ILogger<BookingService> logger)
        {
            _logger = logger;
        }

        public void ProduceAsyncSimulated(NotificationRequest message)
        {
            _logger.LogInformation("Try to notify booking: bookingId={bookinkgId}", message.BookingId);

            // The first time the send goes in error
            if (!_firstAttemptFailed.ContainsKey(message.BookingId))
            {
                _firstAttemptFailed.TryAdd(message.BookingId, message);

                _logger.LogWarning("Notification failed: bookingId={bookinkgId}", message.BookingId);
            }
            else
            {
                _logger.LogInformation("Notification sended: bookingId={bookinkgId}", message.BookingId);
            }
        }

        public List<NotificationRequest> GetNotificationToRetry()
        {
            return _firstAttemptFailed.Values.ToList();
        }

        public bool RemoveNotificationToRetry(string bookingId)
        {
            _firstAttemptFailed.Remove(bookingId);

            return true;
        }
    }
}