namespace LogTest
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class AsyncLogTester : IDisposable
    {
        private string testDir = @"C:\test\unittest";
        private string testFileName = "test.log";

        public AsyncLogTester()
        {
        }

        public void Dispose()
        {
            Console.WriteLine("Tear Down:");
            // Clean Up
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);

            testDir = @"C:\test\unittest";
        }

         private void WriteFile(string path, string name, string content)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            try
            {
                using (var fs = new FileStream(Path.Combine(path, name), FileMode.Append, FileAccess.Write))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(content);
                    sw.Flush();
                    sw.Close();
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        [Fact]
        void ClassConstruction_DefaultPath()
        {
            testDir = @"C:\LogTest";
            AsyncLog log = new AsyncLog();
            Assert.NotNull(log);
            Assert.Equal(testDir, log._logDirectoryPath);
        }

        [Fact]
        void ClassConstruction_CustomPath()
        {
            testDir = @"C:\test\unittest";
            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            Assert.Equal(testDir, log._logDirectoryPath);
        }

        [Fact]
        void ClassConstruction_CurrentDate()
        {
            DateTime today = DateTime.Today;

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            Assert.Equal(today, log._currentDate);
        }

        [Fact]
        void ClassConstruction_ExecutionThread()
        {
            DateTime today = DateTime.Today;

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            Assert.True(log._executionThread.IsAlive);
            Assert.False(log._exitCondition);
        }

        [Fact]
        void GetLogFileName()
        {
            DateTime date = new DateTime(2019, 12, 24, 00, 00, 00);

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            Assert.Equal("20191224.log", log.GetLogFileNameFormat(date));
        }

        [Fact]
        void CheckAndUpdateDate_SameDay()
        {
            DateTime dateA = new DateTime(2019, 12, 24, 00, 00, 00);
            DateTime dateB = new DateTime(2019, 12, 24, 23, 59, 59);

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            
            // Stop execution thread. It might call what we are testing
            log._exitCondition = true;
            Thread.Sleep(20);

            log._currentDate = dateA;
            log._currentLogFile = "20191224.log";

            log.CheckAndUpdateDate(dateB);
            Assert.Equal(dateB, log._currentDate);
            Assert.Equal("20191224.log", log._currentLogFile);
        }

        [Fact]
        void CheckAndUpdateDate_NewDay()
        {
            DateTime dateA = new DateTime(2019, 12, 24, 12, 34, 56);
            DateTime dateB = dateA.AddDays(1);

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);

            // Stop execution thread. It might call what we are testing
            log._exitCondition = true;
            Thread.Sleep(20);

            log._currentDate = dateA;

            log.CheckAndUpdateDate(dateB);
            Assert.Equal(dateB, log._currentDate);
            Assert.Equal("20191225.log", log._currentLogFile);
        }

        [Fact]
        void Write2File_newFile()
        {
            DateTime date = new DateTime(2019, 12, 24, 12, 34, 56);
            LogLine ll = new LogLine() { Text = "abcABC123 ", Timestamp = date};
            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            log._currentLogFile = testFileName;

            log.Write2File(ll);
            Assert.True(File.Exists(Path.Combine(testDir, testFileName)));

            string lines = File.ReadAllText(Path.Combine(testDir, testFileName));

            Assert.Contains("2019-12-24 12:34:56\tabcABC123", lines);
        }

        [Fact]
        void Write2File_appendFile()
        {
            WriteFile(testDir, testFileName, "Some other content\nThis could be anything\n0123456789\n");
            DateTime date = new DateTime(2019, 12, 24, 12, 34, 56);
            LogLine ll = new LogLine() { Text = "abcABC123 ", Timestamp = date};
            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            log._currentLogFile = testFileName;

            log.Write2File(ll);
            Assert.True(File.Exists(Path.Combine(testDir, testFileName)));

            string lines = File.ReadAllText(Path.Combine(testDir, testFileName));

            Assert.Contains("Some other content", lines);
            Assert.Contains("2019-12-24 12:34:56\tabcABC123", lines);
        }

        [Fact]
        void StopWithoutFlush()
        {
            ConcurrentQueue<Task> testTasks = new ConcurrentQueue<Task>();

            int j = 0;
            for(int i=0; i<=50; i++)
            {
                // Populate with slow Tasks for testing.
                testTasks.Enqueue(new Task(() =>
                {
                    WriteFile(testDir, testFileName, j++.ToString());
                    Thread.Sleep(20);
                }));
            }

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            Assert.False(log._exitCondition);
            Assert.Empty(log._logTasks);
            log._logTasks = testTasks;

            // Run for a little while
            Thread.Sleep(200);
            log.StopWithoutFlush();
            Assert.True(log._exitCondition); Assert.NotEmpty(log._logTasks); 

            // Ensure last running task is completed
            Thread.Sleep(25);
            string lines = File.ReadAllText(Path.Combine(testDir, testFileName));
            Assert.DoesNotContain("50", lines);
        }

        [Fact]
        void StopWithFlush()
        {
            ConcurrentQueue<Task> testTasks = new ConcurrentQueue<Task>();

            int j = 0;
            for(int i=0; i<=50; i++)
            {
                // Populate with slow Tasks for testing.
                testTasks.Enqueue(new Task(() =>
                {
                    WriteFile(testDir, testFileName, j++.ToString());
                    Thread.Sleep(20);
                }));
            }

            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            Assert.False(log._exitCondition);
            Assert.Empty(log._logTasks);
            log._logTasks = testTasks;
            Assert.NotEmpty(log._logTasks);

            // Run for a little while
            Thread.Sleep(200);
            log.StopWithFlush();
            Assert.True(log._exitCondition);
            Assert.Empty(log._logTasks);

            // Ensure last running task is completed
            Thread.Sleep(25);

            string lines = File.ReadAllText(Path.Combine(testDir, testFileName));
            Assert.Contains("50", lines);
        }

        [Fact]
        void Write()
        {
            AsyncLog log = new AsyncLog(testDir);
            Assert.NotNull(log);
            log._exitCondition = true;

            Assert.Empty(log._logTasks);

            log.Write("abcABC123");

            Assert.NotEmpty(log._logTasks);
            bool success = log._logTasks.TryPeek(out Task task);
            Assert.NotNull(task);
            Assert.True(success);
        }
    }
}
