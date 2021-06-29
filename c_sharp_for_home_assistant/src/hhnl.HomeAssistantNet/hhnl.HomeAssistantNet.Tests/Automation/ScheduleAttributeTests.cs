using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace hhnl.HomeAssistantNet.Tests.Automation
{
    [TestClass]
    public class ScheduleAttributeTests
    {
        [TestMethod]
        public void Simple_constructor_should_create_correct_cron_expression()
        {
            // Act
            var result = new ScheduleAttribute(WeekDay.All, 20, 14, 15);

            // Assert
            Assert.AreEqual("15 14 20 ? * SUN,MON,TUE,WED,THU,FRI,SAT", result.CronExpression);
        }
    }
}
