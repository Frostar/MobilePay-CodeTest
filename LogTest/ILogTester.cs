
namespace LogTest
{
    using System;
    using Xunit;

    public class ILogTester : IDisposable
    {
        public ILogTester()
        {
        }

        public void Dispose()
        {
        }

        [Fact]
        void ConstructAsyncLog_defaultPath()
        {
            ILog log = new AsyncLog();
            Assert.NotNull(log);
        }

        [Fact]
        void ConstructAsyncLog_customPath()
        {
            ILog log = new AsyncLog(@"C:\test\unittest");
            Assert.NotNull(log);
        }

    }
}
