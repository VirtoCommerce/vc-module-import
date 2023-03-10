using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.ImportModule.Core.Notifications;

public class ImportCompletedEmailNotification: EmailNotification
{
    public virtual ImportRunHistory ImportRunHistory { get; set; }

    public virtual Member Member { get; set; }

    public ImportCompletedEmailNotification() : this(nameof(ImportCompletedEmailNotification))
    {
    }
    
    public ImportCompletedEmailNotification(string type) : base(type)
    {
    }
}
