using MediatR;
using VirtoCommerce.ImportModule.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Core.Domains.ImportProfileAggregate.Events
{
    public class ImportStartedDomainEvent : INotification
    {
        public ImportPushNotification Notification { get; set; }
        public ImportProfile ImportProfile { get; set; }
    }
}
