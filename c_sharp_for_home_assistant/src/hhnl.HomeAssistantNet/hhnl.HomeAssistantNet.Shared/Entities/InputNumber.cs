using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_number", "InputNumbers")]
    public class InputNumber : ValueEntity<double>
    {
        public InputNumber(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public Task Setvalue(double value, CancellationToken cancellationToken = default)
        {
            return HomeAssistantClient.CallServiceAsync("input_number", "set_value", UniqueId, new
            {
                value = value.ToString(CultureInfo.InvariantCulture),
            }, cancellationToken);
        }
    }
}
