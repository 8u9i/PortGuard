namespace PortGuard.Core.Models;

public record ProcessInfo(
    int Pid,
    string Name,
    string Path,
    long MemoryBytes,
    int ThreadCount,
    DateTime StartTime
);
