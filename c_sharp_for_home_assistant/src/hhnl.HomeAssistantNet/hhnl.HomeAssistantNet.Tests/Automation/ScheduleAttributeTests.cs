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

        [TestMethod]
        public void Simple_constructor_should_create_correct_cron_expression_for_selected_days()
        {
            // Act
            var result = new ScheduleAttribute(WeekDay.Monday | WeekDay.Tuesday | WeekDay.Wednesday | WeekDay.Thursday | WeekDay.Friday, 20);

            // Assert
            Assert.AreEqual("0 0 20 ? * MON,TUE,WED,THU,FRI", result.CronExpression);
        }
    }
}
