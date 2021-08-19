using DevInstance.SampleWebApp.Shared.Utils;
using Moq;
using System;

namespace DevInstance.SampleWebApp.Shared.TestUtils
{
    public class TimerProviderMock
    {
        public static DateTime TEST_TIME = new DateTime(2020, 6, 7, 7, 0, 0);

        public static ITimeProvider CreateTimerProvider()
        {
            return CreateTimerProvider(TEST_TIME);
        }
        public static ITimeProvider CreateTimerProvider(DateTime time)
        {
            var timeProvider = new Mock<ITimeProvider>();
            timeProvider.Setup(t => t.CurrentTime).Returns(time);
            return timeProvider.Object;
        }
    }
}
