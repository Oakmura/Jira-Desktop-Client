#define PROFILING

#if PROFILING

using System.Diagnostics;

namespace JiraClient.Utilities
{
    public class ScopeTimer : IDisposable
    {
        private Stopwatch mStopWatch = new Stopwatch();
        private string mOperation;
        public ScopeTimer(string operation)
        {
            mOperation = operation;
            mStopWatch.Start();
        }

        public void Dispose()
        {
            mStopWatch.Stop();
            TimeSpan ts = mStopWatch.Elapsed;
            string elapsedTime = $"{mOperation} Took {ts.Hours:00}시간 {ts.Minutes:00}분 {ts.Seconds:00}초 {ts.Milliseconds:000}ms {ts.Nanoseconds}ns";
            Logger.Log(MessageType.Info, elapsedTime);
        }
    }
}

#else

public class ScopeTimer : IDisposable
{
    public ScopeTimer(string operation) { }
    public void Dispose() { }
}

#endif