using System.Diagnostics;
using PortGuard.Core.Abstractions;

namespace PortGuard.Core.Services;

public class ProcessKiller : IProcessKiller
{
    public KillResult Kill(int processId, bool force = false)
    {
        try
        {
            var proc = Process.GetProcessById(processId);
            proc.Kill(entireProcessTree: force);

            // Wait briefly to confirm
            proc.WaitForExit(3000);
            return new KillResult(true, null, null);
        }
        catch (ArgumentException)
        {
            return new KillResult(false, "Process not found.", null);
        }
        catch (InvalidOperationException)
        {
            return new KillResult(false, "Process already exited.", null);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return new KillResult(false, ex.Message, "Administrator privileges required.");
        }
        catch (Exception ex)
        {
            return new KillResult(false, ex.Message, null);
        }
    }
}
