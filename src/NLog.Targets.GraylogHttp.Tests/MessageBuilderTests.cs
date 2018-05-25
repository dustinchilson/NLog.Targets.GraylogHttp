using System;
using Xunit;

namespace NLog.Targets.GraylogHttp.Tests
{
    public class MessageBuilderTests
    {
        [Fact]
#pragma warning disable CA1822 // Mark members as static
        public void SimpleMessageTest()
#pragma warning restore CA1822 // Mark members as static
        {
            var testMessage = new GraylogMessageBuilder(() => 10)
                .WithCustomProperty("facility", "Test")
                .WithProperty("short_message", "short_message")
                .WithProperty("host", "magic")
                .WithLevel(LogLevel.Debug)
                .WithCustomProperty("logger_name", "SimpleMessageTest")
                .Render();

            var expectedMessage = "{\"_facility\":\"Test\",\"short_message\":\"short_message\",\"host\":\"magic\",\"level\":\"7\",\"_logger_name\":\"SimpleMessageTest\",\"timestamp\":\"10\",\"version\":\"1.1\"}";

            Assert.Equal(expectedMessage, testMessage);
        }

        [Fact]
#pragma warning disable CA1822 // Mark members as static
        public void MessageWithHugePropertyTest()
#pragma warning restore CA1822 // Mark members as static
        {
            var testMessage = new GraylogMessageBuilder(() => 10)
                .WithCustomProperty("facility", "Test")
                .WithProperty("short_message", "short_message")
                .WithProperty("host", "magic")
                .WithLevel(LogLevel.Debug)
                .WithCustomProperty("logger_name", "SimpleMessageTest")
                .WithProperty("longstring", new string('*', 50000))
                .Render();

            var expectedMessage = $"{{\"_facility\":\"Test\",\"short_message\":\"short_message\",\"host\":\"magic\",\"level\":\"7\",\"_logger_name\":\"SimpleMessageTest\",\"longstring\":\"{new string('*', 16383)}\",\"timestamp\":\"10\",\"version\":\"1.1\"}}";

            Assert.Equal(expectedMessage, testMessage);
        }
    }
}
