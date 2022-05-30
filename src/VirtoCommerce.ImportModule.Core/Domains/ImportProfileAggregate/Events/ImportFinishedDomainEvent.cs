using System;
using MediatR;
using VirtoCommerce.ImportModule.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Core.Domains.ImportProfileAggregate.Events
{
    public class ImportFinishedDomainEvent : INotification
    {
        public Exception Exception { get; set; }
        public ImportPushNotification Notification { get; set; }
        public ImportProfile ImportProfile { get; set; }
    }
}
