using System.Diagnostics;
using PortGuard.Core.Models;

namespace PortGuard.Core.Services;

public class ProcessInfoResolver
{
    private readonly Dictionary<int, (string name, string path, long memory, int threads, DateTime start)> _cache = new();

    public ProcessInfo Resolve(int processId)
    {
        if (_cache.TryGetValue(processId, out var cached))
        {
            return new ProcessInfo(processId, cached.name, cached.path, cached.memory, cached.threads, cached.start);
        }

        try
        {
            // Use timeout from ThreadPool to avoid hanging on system/terminating processes
            var tcs = new TaskCompletionSource<ProcessInfo>();
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var task = Task.Run(() =>
            {
                try
                {
                    using var proc = Process.GetProcessById(processId);
                    string name = proc.ProcessName;
                    string path;
                    try { path = proc.MainModule?.FileName ?? ""; }
                    catch { path = ""; }

                    long memory = 0;
                    int threads = 0;
                    DateTime start = DateTime.MinValue;
                    try
                    {
                        memory = proc.WorkingSet64;
                        threads = proc.Threads.Count;
                        start = proc.StartTime;
                    }
                    catch { }

                    return new ProcessInfo(processId, name, path, memory, threads, start);
                }
                catch (ArgumentException)
                {
                    return new ProcessInfo(processId, "<exited>", "", 0, 0, DateTime.MinValue);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return new ProcessInfo(processId, "<system>", "", 0, 0, DateTime.MinValue);
                }
            }, cts.Token);

            if (task.Wait(500, cts.Token))
            {
                var info = task.Result;
                _cache[processId] = (info.Name, info.Path, info.MemoryBytes, info.ThreadCount, info.StartTime);
                return info;
            }
        }
        catch { }

        var fallback = new ProcessInfo(processId, $"PID {processId}", "", 0, 0, DateTime.MinValue);
        _cache[processId] = (fallback.Name, fallback.Path, 0, 0, DateTime.MinValue);
        return fallback;
    }

    public void ClearCache() => _cache.Clear();

    public void Invalidate(int processId) => _cache.Remove(processId);
}
