using hhnl.HomeAssistantNet.Automations.Triggers;
using hhnl.HomeAssistantNet.Shared.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace hhnl.HomeAssistantNet.TestProject
{
    public class AttributeTests
    {
        [Automation]
        [Schedule(Every.Day)]
        [Schedule(Every.Month)]
        public async Task ScheduleTest()
        {

        }
    }
}
