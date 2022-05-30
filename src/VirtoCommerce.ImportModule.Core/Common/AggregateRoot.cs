using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.ImportModule.Core.Common
{
    public abstract class AggregateRoot : AuditableEntity, IAggregateRoot, ICloneable
    {
        private readonly List<INotification> _domainEvents = new List<INotification>();

        [JsonIgnore]
        [SwaggerIgnore]
        /// <summary>
        /// Domain events occurred.
        /// </summary>
        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents?.AsReadOnly();

        public void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Add domain event.
        /// </summary>
        /// <param name="domainEvent">Domain event.</param>
        protected void AddDomainEvent(INotification domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }


        protected void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleValidationException(rule);
            }
        }
    }
}
