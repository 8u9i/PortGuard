using PortGuard.Core.Services;
using PortGuard.Windows;
using Xunit;

namespace PortGuard.Tests;

public class NativePortEnumeratorTests
{
    [Fact]
    public void EnumerateAll_ReturnsPorts()
    {
        var enumerator = new NativePortEnumerator();
        var ports = enumerator.EnumerateAll();

        Assert.NotNull(ports);
    }

    [Fact]
    public void EnumerateFiltered_ByPort_Works()
    {
        var enumerator = new NativePortEnumerator();
        var ports = enumerator.EnumerateFiltered(port: 99999, null, null);
        Assert.Empty(ports);
    }
}

public class ProcessKillerTests
{
    [Fact]
    public void Kill_NonexistentPid_ReturnsFailure()
    {
        var killer = new ProcessKiller();
        var result = killer.Kill(99999999);
        Assert.False(result.Success);
    }
}
