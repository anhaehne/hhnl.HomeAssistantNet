using hhnl.HomeAssistantNet.Shared.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
