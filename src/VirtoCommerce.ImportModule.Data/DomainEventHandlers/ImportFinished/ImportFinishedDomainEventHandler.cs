using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ImportModule.Core.Common;
using VirtoCommerce.ImportModule.Core.Domains;
using VirtoCommerce.ImportModule.Core.Domains.ImportProfileAggregate.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.ImportModule.Data.DomainEventHandlers.ImportFinished
{
    public class ImportFinishedDomainEventHandler : INotificationHandler<ImportFinishedDomainEvent>
    {
        private readonly ICrudService<ImportRunHistory> _crudService;
        private readonly ILoggerFactory _logger;
        private readonly IPushNotificationManager _pushNotifier;

        public ImportFinishedDomainEventHandler(
            ICrudService<ImportRunHistory> crudService,
            IPushNotificationManager pushNotifier,
            ILoggerFactory logger)
        {
            _crudService = crudService;
            _logger = logger;
            _pushNotifier = pushNotifier;
        }
        public async Task Handle(ImportFinishedDomainEvent importFinishedDomainEvent, CancellationToken cancellationToken)
        {
            var importRunHistory = ExType<ImportRunHistory>.New().CreateNew(importFinishedDomainEvent.ImportProfile, importFinishedDomainEvent.Notification);

            await _crudService.SaveChangesAsync(new[] { importRunHistory });
        }
    }
}
