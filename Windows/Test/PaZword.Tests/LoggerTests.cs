using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using System.Globalization;

namespace PaZword.Tests
{
    [TestClass]
    public class LoggerTests : MefBaseTest
    {
        [TestMethod]
        public void LoggerSizeLimit()
        {
            ILogger logger = ExportProvider.GetExport<ILogger>();

            for (int i = 0; i < 2000; i++)
            {
                logger.LogEvent("foo", i.ToString(CultureInfo.InvariantCulture));
            }

            var logs = logger.GetAllLogs().ToString();
            Assert.IsTrue(logs.StartsWith("foo ; 1000", System.StringComparison.Ordinal));
            Assert.IsTrue(logs.EndsWith("foo ; 1999\r\n", System.StringComparison.Ordinal));
        }
    }
}
