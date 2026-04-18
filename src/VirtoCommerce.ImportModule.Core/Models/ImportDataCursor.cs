using System;
using System.Text;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.Core.Models
{
    public abstract record ImportDataCursor
    {
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        // Pipeline's ProcessedCount at cursor capture; authoritative on restore.
        // Populated by IImportDataReader<TCursor> DIM bridge (Task 3), not by concrete readers.
        public int ProcessedCount { get; init; }

        public virtual string Serialize(JsonSerializerSettings settings = null)
        {
            var json = JsonConvert.SerializeObject(this, settings);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static T Deserialize<T>(string cursor, JsonSerializerSettings settings = null)
            where T : ImportDataCursor
        {
            if (string.IsNullOrEmpty(cursor))
            {
                return null;
            }

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (FormatException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public virtual bool IsValid(ImportContext context)
        {
            var lifetimeDays = context.ImportProfile.Settings
                .GetValue<int>(ImportCursorSettings.LifetimeDays);
            return DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(lifetimeDays);
        }
    }
}
