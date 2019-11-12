using Xunit;

namespace NLog.Targets.GraylogHttp.Tests
{
    public class StringExtTests
    {
        [Fact]
        public void TruncateTest()
        {
            var testString = new string('*', 50000).Truncate(1);
            Assert.Equal("*", testString);
        }
    }
}
