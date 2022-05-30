using System;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Tests.Functional
{
    public class PushNotificationManagerStub : IPushNotificationManager
    {
        public PushNotificationSearchResult SearchNotifies(string userId, PushNotificationSearchCriteria criteria)
        {
            throw new NotImplementedException();
        }

        public void Send(PushNotification notification)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(PushNotification notification)
        {
            return Task.CompletedTask;
        }
    }
}
