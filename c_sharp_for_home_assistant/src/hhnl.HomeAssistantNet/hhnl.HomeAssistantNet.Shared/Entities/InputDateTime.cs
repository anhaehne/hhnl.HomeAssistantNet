using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_datetime", "InputDateTimes")]
    public class InputDateTime : ValueEntity<DateTime>
    {
        public InputDateTime(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool HasTime => GetAttributeOrDefault<bool>("has_time");

        public bool HasDate => GetAttributeOrDefault<bool>("has_date");

        protected override DateTime? Parse(string state)
        {
            // Date only or date + time.
            if(HasDate)
                return DateTime.Parse(state);

            // Time only
            return DateTime.ParseExact(state, "hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault);
        }

        public Task SetDate(DateTime date)
        {
            return HomeAssistantClient.CallServiceAsync("input_datetime", "set_datetime", UniqueId, new
            {
                date = date.ToString("yyyy-MM-dd"),
            });
        }

        public Task SetTime(DateTime date)
        {
            return HomeAssistantClient.CallServiceAsync("input_datetime", "set_datetime", UniqueId, new
            {
                time = date.ToString("HH:mm:ss"),
            });
        }

        public Task SetDateTime(DateTime date)
        {
            return HomeAssistantClient.CallServiceAsync("input_datetime", "set_datetime", UniqueId, new
            {
                dateTime = date.ToString("yyyy-MM-dd HH:mm:ss"),
            });
        }
    }
}
