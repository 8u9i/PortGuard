namespace PortGuard.Core.Abstractions;

public record KillResult(bool Success, string? ErrorMessage, string? RequiredPrivileges);

public interface IProcessKiller
{
    KillResult Kill(int processId, bool force = false);
}
