namespace LogTest
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncLog : ILog
    {
        internal bool _exitCondition = false;
        internal ConcurrentQueue<Task> _logTasks = new ConcurrentQueue<Task>();
        internal DateTime _currentDate = new DateTime();
        internal string _currentLogFile;
        internal string _logDirectoryPath;
        internal Thread _executionThread;

        public AsyncLog(string path = @"C:\LogTest")
        {
            _logDirectoryPath = path;
            if (!Directory.Exists(_logDirectoryPath))
            {
                Directory.CreateDirectory(_logDirectoryPath);
            }

            _currentDate = DateTime.Today;
            _currentLogFile = GetLogFileNameFormat(_currentDate);

            this._executionThread = new Thread(this.ExecutionLoop);
            this._executionThread.Start();
        }

        ~AsyncLog()
        {
            // Let _executionThread exist at first comming possiblity
            _exitCondition = true;
            if(_executionThread.IsAlive)
            {
                _executionThread.Join();
            }

        }

        internal void ExecutionLoop()
        {
            while (!_exitCondition)
            {
                // Check if we are at new date and update current log file name if so
                CheckAndUpdateDate(DateTime.Today);

                if(!_logTasks.IsEmpty)
                {
                    // Try to get element from log task queue
                    bool result = _logTasks.TryDequeue(out Task logTask);
                    if(result)
                    {
                        // Succesfull got a logging task. Lets execute it and wait for to complete.
                        logTask.Start();
                        logTask.Wait();
                    }
                }
            }
        }

        internal string GetLogFileNameFormat(DateTime date)
        {
            return date.ToString("yyyyMMdd") + ".log";
        }

        internal void CheckAndUpdateDate(DateTime now)
        {
            if (_currentDate == now)
            {
                // Same date as current. We should not do anything
                return;
            }
            else
            {
                // Date has changed since last date update. Update relevant variables
                _currentDate = now;
                _currentLogFile = GetLogFileNameFormat(_currentDate);
            }
        }

        internal void Write2File(LogLine log)
        {
            // Try to write out LogLine linetext til logging file.
            try
            {
                using (var fs = new FileStream(Path.Combine(_logDirectoryPath, _currentLogFile), FileMode.Append, FileAccess.Write))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(log.LineText());
                    sw.Flush();
                    sw.Close();
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public string CurrentLogFile { get => _currentLogFile; set => _currentLogFile = value; }

        public void StopWithoutFlush()
        {
            // Setting condition to exit execution loop
            _exitCondition = true;
        }

        public void StopWithFlush()
        {
            // Wait for existing task in Queue to complete 
            while (!_logTasks.IsEmpty) { };

            // Setting condition to exit execution loop
            _exitCondition = true;
        }

        public void Write(string text)
        {
            // Add a new logging task to logTask queue.
            _logTasks.Enqueue(new Task(() => Write2File(new LogLine() { Text = text, Timestamp = DateTime.Now })));
        }
    }
}