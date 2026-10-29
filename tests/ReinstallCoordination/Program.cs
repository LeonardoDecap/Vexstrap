global using System.Text.Json;
global using Vexstrap.Resources;
global using Vexstrap.Utility;
using System.Diagnostics;
using Vexstrap.UI;

// Each run uses its own kernel object and file, never the installed app's locks.
string testId = args.Length > 1 ? args[1] : Guid.NewGuid().ToString("N");
Vexstrap.App.ProjectName = $"Vexstrap-ReinstallTest-{testId}";
string lockPath = Path.Combine(Path.GetTempPath(), Vexstrap.App.ProjectName, "operation.lock");
Vexstrap.Paths.Base = Path.GetDirectoryName(lockPath)!;

if (args.Length != 0)
{
    if (args[0] == "marker")
    {
        ReinstallGate.HoldMarker();
        Console.WriteLine("ready");
        Console.Out.Flush();
        Thread.Sleep(30000);
    }
    else
    {
        using var held = args[0].StartsWith("guard-")
            ? ReinstallOperationLock.TryStart(lockPath, args[0] == "guard-exclusive")
            : ReinstallOperationLock.TryAcquire(lockPath, args[0] == "exclusive");
        Console.WriteLine(held is null ? "blocked" : "acquired");
        Console.Out.Flush();
        if (held is not null) Thread.Sleep(30000);
    }
    return;
}

Process Start(string mode) => Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"{mode} {testId}")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    CreateNoWindow = true
})!;
void Assert(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
void Stop(Process p) { if (!p.HasExited) p.Kill(); p.WaitForExit(); p.Dispose(); }
var processes = new List<Process>();
Process Run(string mode, string expected)
{
    var p = Start(mode);
    processes.Add(p);
    Assert(p.StandardOutput.ReadLine() == expected, $"{mode} process reports {expected}");
    return p;
}
void Remove(Process p) { Stop(p); processes.Remove(p); }

try
{
    Assert(!ReinstallGate.IsRunning, "marker initially absent");
    var first = Run("marker", "ready");
    var blocked = Run("guard-shared", "blocked"); Remove(blocked);
    var guardedReinstall = Run("guard-exclusive", "acquired"); Remove(guardedReinstall);
    var second = Run("marker", "ready");
    Remove(first);
    Assert(ReinstallGate.IsRunning, "marker survives Settings process termination");
    Remove(second);
    Assert(!ReinstallGate.IsRunning, "marker disappears after reinstall process termination");

    first = Run("shared", "acquired");
    second = Run("shared", "acquired");
    blocked = Run("exclusive", "blocked"); Remove(blocked);
    Remove(first);
    blocked = Run("exclusive", "blocked"); Remove(blocked);
    Remove(second);
    var writer = Run("exclusive", "acquired");
    blocked = Run("shared", "blocked"); Remove(blocked);
    blocked = Run("exclusive", "blocked"); Remove(blocked);
    Remove(writer);
    first = Run("shared", "acquired"); Remove(first);
    writer = Run("exclusive", "acquired"); Remove(writer);
    Console.WriteLine("PASS shared launches coexist, reinstall excludes operations, and locks recover after termination");

    var state = new Vexstrap.JsonManager<TestState>();
    const string incompleteJson = "{\"Value\":";
    File.WriteAllText(state.FileLocation, incompleteJson);
    ReinstallGate.Set(true);
    Assert(!state.Load(alertFailure: false), "incomplete JSON fails to load during reinstall");
    Assert(File.ReadAllText(state.FileLocation) == incompleteJson, "failed load does not replace the state file");
    ReinstallGate.Set(false);
    File.WriteAllText(state.FileLocation, "{\"Value\":42}");
    Assert(state.Load(alertFailure: false, repairOnFailure: false) && state.Prop.Value == 42,
        "state reload succeeds after the write finishes");

    string mutexName = $"ReinstallTest-{testId}";
    var processLock = new InterProcessLock(mutexName);
    Assert(processLock.IsAcquired, "inter-process mutex can be acquired");
    processLock.Dispose();
    processLock.Dispose();
    Assert(!Vexstrap.Utilities.DoesMutexExist("Vexstrap-" + mutexName), "disposing the mutex closes its handle and is repeatable");
}
finally
{
    foreach (var p in processes) Stop(p);
    if (Directory.Exists(Path.GetDirectoryName(lockPath)!))
        Directory.Delete(Path.GetDirectoryName(lockPath)!, recursive: true);
}

namespace Vexstrap
{
    internal static class App
    {
        public static string ProjectName = "";
        public static TestLaunchSettings LaunchSettings = new();
        public static TestLogger Logger = new();
    }
    internal sealed class TestLaunchSettings { public TestFlag SettingsReinstallFlag { get; } = new(); }
    internal sealed class TestFlag { public bool Active => false; }
    internal sealed class TestLogger
    {
        public void WriteLine(string source, string message) { }
        public void WriteException(string source, Exception exception) { }
    }
    internal static class Paths
    {
        public static string Base = "";
        public static string Temp => Path.GetTempPath();
    }
    internal static class Frontend
    {
        public static void ShowMessageBox(string text, System.Windows.MessageBoxImage icon) =>
            throw new InvalidOperationException("Unexpected dialog: " + text);
    }
    internal sealed class Settings { }
    internal sealed class FastFlagManager { }
    internal static class Utilities
    {
        public static bool DoesMutexExist(string name)
        {
            try { using var mutex = Mutex.OpenExisting(name); return true; }
            catch (WaitHandleCannotBeOpenedException) { return false; }
        }
    }
}

public sealed class TestState { public int Value { get; set; } }
namespace System.Windows { internal enum MessageBoxImage { Warning } }
namespace Vexstrap.Resources
{
    internal static class Strings
    {
        public static string JsonManager_SettingsLoadFailed => "Settings load failed";
        public static string JsonManager_FastFlagsLoadFailed => "Fast flags load failed";
        public static string Bootstrapper_JsonManagerSaveFailed => "{0}: {1}";
    }
}
namespace Vexstrap.Utility
{
    internal static class MD5Hash
    {
        public static string FromString(string value) => Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
        public static string FromFile(string path) => Convert.ToHexString(System.Security.Cryptography.MD5.HashData(File.ReadAllBytes(path)));
    }
}
