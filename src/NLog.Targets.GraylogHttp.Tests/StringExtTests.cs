using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NLog.Targets.GraylogHttp.Tests
{
    public class StringExtTests
    {
        [Fact]
#pragma warning disable CA1822 // Mark members as static
        public void TruncateTest()
#pragma warning restore CA1822 // Mark members as static
        {
            var testString = new string('*', 50000).Truncate(1);
            Assert.Equal("*", testString);
        }
    }
}
