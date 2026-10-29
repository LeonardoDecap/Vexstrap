using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Vexstrap;
using Vexstrap.UI;
using Vexstrap.UI.Elements.Settings;
using Vexstrap.UI.Elements.Settings.Pages;
using Vexstrap.UI.ViewModels.Settings;

internal static class Program
{
    private static string ProjectName => (string)typeof(App).GetField(nameof(App.ProjectName))!.GetRawConstantValue()!;
    private static Exception? _dispatcherFailure;

    private sealed class TestApp : App
    {
        // Starting the dispatcher in this harness must not run the real installer.
        protected override void OnStartup(StartupEventArgs e) { }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("-settingsreinstall"))
        {
            if (ProjectName != "Vexstrap-QA")
                return 2;
            string id = Environment.GetEnvironmentVariable("VEXSTRAP_TEST_REINSTALL_ID")!;
            ReinstallGate.HoldMarker();
            ReinstallGate.MarkStarted();
            File.WriteAllText(Environment.GetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH")!, "{\"ForceReinstall\":true}");
            using var ready = EventWaitHandle.OpenExisting(id + "-ready");
            using var release = EventWaitHandle.OpenExisting(id + "-release");
            ready.Set();
            return release.WaitOne(TimeSpan.FromSeconds(30)) ? 0 : 1;
        }
        if (args.Length == 1 && args[0] == "quick-reinstall")
        {
            if (ProjectName != "Vexstrap-QA")
                return 2;
            ReinstallGate.HoldMarker();
            ReinstallGate.MarkStarted();
            File.WriteAllText(Environment.GetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH")!, "{\"ForceReinstall\":false}");
            return 0;
        }
        if (args.Length == 1 && args[0] == "hold-marker")
        {
            if (ProjectName != "Vexstrap-QA")
                return 2;
            ReinstallGate.HoldMarker();
            Console.WriteLine("ready");
            Console.Out.Flush();
            Thread.Sleep(30000);
            return 0;
        }
        if (args.Length == 1 && (args[0] == "blocked-player" || args[0] == "blocked-background" || args[0] == "blocked-uninstall"))
        {
            if (ProjectName != "Vexstrap-QA")
                return 2;
            string[] launchArgs = args[0] switch
            {
                "blocked-player" => new[] { "-player", "-quiet" },
                "blocked-uninstall" => new[] { "-uninstall", "-quiet" },
                _ => new[] { "-backgroundupdater", "Player", "-quiet" }
            };
            typeof(App).GetProperty(nameof(App.LaunchSettings))!.SetValue(null, new LaunchSettings(launchArgs));
            LaunchHandler.ProcessLaunchArgs();
            return 99; // A blocked operation must terminate rather than reach here.
        }
        try { Run(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Run()
    {
        if (ProjectName != "Vexstrap-QA")
            throw new InvalidOperationException("Run this test with -p:DefineConstants=QA_BUILD to isolate its marker from the installed app.");
        string testDirectory = Path.Combine(Path.GetTempPath(), "Vexstrap-SettingsGateTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        try
        {
            var app = new TestApp();
            app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ThemeType.Dark });
            app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Vexstrap;component/UI/Style/Dark.xaml") });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Vexstrap;component/UI/Style/Default.xaml") });
            foreach (string converter in new[] { "StringFormatConverter", "RangeConverter", "EnumNameConverter" })
                app.Resources[converter] = Activator.CreateInstance(typeof(App).Assembly.GetType("Vexstrap.UI.Converters." + converter)!, nonPublic: true)!;
            app.DispatcherUnhandledException += (_, e) => { _dispatcherFailure = e.Exception; e.Handled = true; };
            typeof(App).GetProperty(nameof(App.LaunchSettings))!.SetValue(null, new LaunchSettings(Array.Empty<string>()));
            typeof(App).Assembly.GetType("Vexstrap.Paths")!.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, new object[] { testDirectory });
            App.State.Save();
            App.State.Load();
            // Locked old versions must not be reported as a successful cleanup.
            string currentVersion = Path.Combine(testDirectory, "Versions", "version-current");
            string oldVersion = Path.Combine(testDirectory, "Versions", "version-old");
            Directory.CreateDirectory(currentVersion);
            Directory.CreateDirectory(oldVersion);
            string oldExecutable = Path.Combine(oldVersion, "RobloxPlayerBeta.exe");
            File.WriteAllText(oldExecutable, "test fixture, not an executable");
            App.PlayerState.Prop.VersionGuid = "version-current";
            using (var heldFile = File.Open(oldExecutable, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var cleanup = Bootstrapper.CleanupVersionsFolder();
                Assert(cleanup.IsBlocked && Directory.Exists(oldVersion),
                    "cleanup reports a locked old version as blocked and preserves it");
                Assert(Directory.Exists(currentVersion), "cleanup preserves the current version");
            }
            App.PlayerState.Prop.VersionGuid = "";
            foreach (string icon in new[] { "Error", "Information", "Question", "Warning" })
            {
                var resource = Application.GetResourceStream(new Uri($"pack://application:,,,/Vexstrap;component/Resources/MessageBox/{icon}.png"));
                using var stream = resource.Stream;
                Assert(stream.Length > 0, $"{icon} dialog image is packaged in the application");
            }
            var appearance = (AppearanceViewModel)new AppearancePage().DataContext;
            var visibleIcons = appearance.ThemeIcons.Concat(appearance.LegacyIcons).Concat(appearance.CustomIcons)
                .Select(entry => entry.IconType).ToHashSet();
            Assert(appearance.Icons.All(entry => visibleIcons.Contains(entry.IconType)),
                "every supported icon can be selected on the appearance page");
            typeof(App).GetProperty(nameof(App.ReinstallGenerationAtLoad))!
                .SetValue(null, ReinstallGate.ReadGeneration());

            var lockType = typeof(App).Assembly.GetType("Vexstrap.Utility.ReinstallOperationLock")!;
            var acquire = lockType.GetMethod("TryAcquire", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(bool) }, null)!;
            var startOperation = typeof(LaunchHandler).GetMethod("TryStartRobloxOperation", BindingFlags.Static | BindingFlags.NonPublic)!;
            var operationField = typeof(LaunchHandler).GetField("_robloxOperationLock", BindingFlags.Static | BindingFlags.NonPublic)!;
            bool StartOperation() => (bool)startOperation.Invoke(null, null)!;
            IDisposable? Acquire(bool exclusive) => (IDisposable?)acquire.Invoke(null, new object[] { exclusive });
            void ReleaseOperation()
            {
                ((IDisposable?)operationField.GetValue(null))?.Dispose();
                operationField.SetValue(null, null);
            }

            ReinstallGate.Set(true);
            Assert(!StartOperation(), "normal Roblox launch is blocked by the reinstall marker");
            ReinstallGate.Set(false);
            using (var held = Acquire(exclusive: true))
                Assert(held is not null && !StartOperation(), "normal launch is blocked by an active exclusive reinstall");
            using (var held = Acquire(exclusive: false))
            {
                Assert(held is not null && StartOperation(), "ordinary launches can coexist");
                ReleaseOperation();
            }
            typeof(App).GetProperty(nameof(App.LaunchSettings))!.SetValue(null, new LaunchSettings(new[] { "-player", "-nolaunch", "-settingsreinstall" }));
            ReinstallGate.Set(true);
            Assert(StartOperation(), "the reinstall child can start while its marker is present");
            using (var blocked = Acquire(exclusive: false))
                Assert(blocked is null, "the reinstall child excludes ordinary launches");
            ReleaseOperation();
            ReinstallGate.Set(false);
            typeof(App).GetProperty(nameof(App.LaunchSettings))!.SetValue(null, new LaunchSettings(Array.Empty<string>()));

            ReinstallGate.Set(true);
            var window = new MainWindow(false);
            var root = (UIElement)window.FindName("RootGrid");
            var save = (UIElement)window.FindName("SaveButton");
            var close = (UIElement)window.FindName("SettingsCloseButton");
            var status = (TextBlock)window.FindName("ReinstallStatusText");
            Assert(!root.IsEnabled && !save.IsEnabled && !close.IsEnabled && status.Visibility == Visibility.Visible,
                "Settings opens locked while reinstall is running");

            App.SettingsReloadRequired = true;
            App.FastFlagsReloadRequired = true;
            File.WriteAllText(App.Settings.FileLocation, "{\"CheckForUpdates\":false}");
            Directory.CreateDirectory(Path.GetDirectoryName(App.FastFlags.FileLocation)!);
            File.WriteAllText(App.FastFlags.FileLocation, "{\"FFlagRecoveryTest\":\"true\"}");
            File.WriteAllText(App.State.FileLocation, "{\"ForceReinstall\":true}");
            ReinstallGate.Set(false);
            Assert(root.IsEnabled && save.IsEnabled && close.IsEnabled && status.Visibility == Visibility.Collapsed,
                "Settings unlocks when reinstall finishes");
            Assert(App.State.Prop.ForceReinstall, "Settings reloads state before unlocking");
            Assert(!App.Settings.Prop.CheckForUpdates && App.FastFlags.GetValue("FFlagRecoveryTest") == "true",
                "a reopened Settings window reloads settings and fast flags before unlocking");
            var transient = new JsonManager<TestLoadState>("TransientRecoveryTest");
            File.WriteAllText(transient.FileLocation, "{\"Value\":");
            Assert(!transient.Load(alertFailure: false, repairOnFailure: false), "incomplete file load fails without repair");
            File.Delete(transient.FileLocation);
            var reload = typeof(VexstrapViewModel).GetMethod("TryReloadJsonState", BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(TestLoadState));
            object?[] reloadArgs = { transient, null };
            Assert(!(bool)reload.Invoke(null, reloadArgs)!, "a file lost after an incomplete read is not treated as new");
            Assert(!App.State.HasFileOnDiskChanged(), "successful reload updates the state file fingerprint");

            var behaviourPage = new BehaviourPage();
            var behaviour = (BehaviourViewModel)behaviourPage.DataContext;
            var frameControl = (Frame)window.FindName("RootFrame");
            frameControl.Content = behaviourPage;
            var navigationFrame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(() => navigationFrame.Continue = false));
            Dispatcher.PushFrame(navigationFrame);
            Assert(ReferenceEquals(frameControl.Content, behaviourPage), "test Behaviour page is active");
            var reinstallOption = (UIElement)behaviourPage.FindName("ForceReinstallOption");
            Assert(!reinstallOption.IsEnabled, "reinstall option is disabled when no installation exists");
            var refreshedProperties = new HashSet<string?>();
            behaviour.PropertyChanged += (_, e) => refreshedProperties.Add(e.PropertyName);
            ReinstallGate.Set(true);
            File.WriteAllText(App.PlayerState.FileLocation, "{\"VersionGuid\":\"version-test\"}");
            File.WriteAllText(App.State.FileLocation, "{\"ForceReinstall\":false}");
            ReinstallGate.Set(false);
            Assert(refreshedProperties.Contains(nameof(BehaviourViewModel.IsRobloxInstallationMissing)) &&
                   refreshedProperties.Contains(nameof(BehaviourViewModel.ForceRobloxReinstallation)),
                "recovery notifies the Behaviour page that installation state changed");
            Assert(reinstallOption.IsEnabled, "reinstall option updates after an installation appears");

            // Exercise the actual Settings command and child-process exit handler.
            // The child is this test executable, so it only writes the isolated state file.
            App.Settings.Prop.CheckForUpdates = true; // An unsaved edit in an already-open window.
            string eventId = "Vexstrap-SettingsGateTest-" + Guid.NewGuid().ToString("N");
            using (var ready = new EventWaitHandle(false, EventResetMode.ManualReset, eventId + "-ready"))
            using (var release = new EventWaitHandle(false, EventResetMode.ManualReset, eventId + "-release"))
            {
                Environment.SetEnvironmentVariable("VEXSTRAP_TEST_REINSTALL_ID", eventId);
                Environment.SetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH", App.State.FileLocation);
                try
                {
                    typeof(VexstrapViewModel).GetMethod("LaunchReinstall", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(new VexstrapViewModel(), new object[] { false });
                    Assert(ready.WaitOne(TimeSpan.FromSeconds(10)), "Settings launches the isolated reinstall child");
                    Assert(!save.IsEnabled && ReinstallGate.IsRunning, "Settings stays locked while its child is active");
                    release.Set();
                    var frame = new DispatcherFrame();
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                    var deadline = DateTime.UtcNow.AddSeconds(10);
                    timer.Tick += (_, _) => { if (!ReinstallGate.IsRunning || _dispatcherFailure is not null || DateTime.UtcNow >= deadline) frame.Continue = false; };
                    timer.Start();
                    Dispatcher.PushFrame(frame);
                    timer.Stop();
                    if (_dispatcherFailure is not null)
                        throw new InvalidOperationException("Settings dispatcher failed", _dispatcherFailure);
                    Assert(!ReinstallGate.IsRunning && save.IsEnabled && App.State.Prop.ForceReinstall,
                        "child exit refreshes state and unlocks Settings");
                    Assert(App.Settings.Prop.CheckForUpdates,
                        "recovery preserves unsaved settings in the original window");
                }
                finally
                {
                    release.Set();
                    Environment.SetEnvironmentVariable("VEXSTRAP_TEST_REINSTALL_ID", null);
                    Environment.SetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH", null);
                }
            }

            Environment.SetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH", App.State.FileLocation);
            try
            {
                using var fastChild = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "quick-reinstall")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                fastChild.WaitForExit();
                Assert(fastChild.ExitCode == 0 && !ReinstallGate.IsOperationRunning && ReinstallGate.IsRunning,
                    "a completed reinstall is detected even when the window missed its active period");
                typeof(MainWindow).GetMethod("ReinstallGate_Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(window, new object?[] { window, EventArgs.Empty });
                Assert(!ReinstallGate.IsRunning && save.IsEnabled && !App.State.Prop.ForceReinstall,
                    "a missed reinstall refreshes state before Settings can save");
            }
            finally { Environment.SetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH", null); }

            behaviour.ForceRobloxReinstallation = true;
            Assert(App.PendingForceReinstall == true, "a force-reinstall edit is marked pending");
            Environment.SetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH", App.State.FileLocation);
            try
            {
                using var fastChild = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "quick-reinstall")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                fastChild.WaitForExit();
                Assert(fastChild.ExitCode == 0 && ReinstallGate.IsRunning, "a later reinstall is detected");
                typeof(MainWindow).GetMethod("ReinstallGate_Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(window, new object?[] { window, EventArgs.Empty });
                Assert(App.State.Prop.ForceReinstall && App.PendingForceReinstall == true,
                    "state recovery preserves an unsaved force-reinstall choice");
            }
            finally { Environment.SetEnvironmentVariable("VEXSTRAP_TEST_STATE_PATH", null); }
            App.PendingForceReinstall = null;
            App.State.Prop.ForceReinstall = false;

            string generationPath = (string)typeof(ReinstallGate).GetProperty("GenerationPath", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            using (var blockedGeneration = new FileStream(generationPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                typeof(MainWindow).GetMethod("ReinstallGate_Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(window, new object?[] { window, EventArgs.Empty });
                Assert(ReinstallGate.RecoveryPending && !save.IsEnabled,
                    "Settings stays locked while the reinstall generation cannot be read");
            }
            typeof(MainWindow).GetField("_nextRefreshAttempt", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(window, DateTime.MinValue);
            typeof(MainWindow).GetMethod("ReinstallGate_Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(window, new object?[] { window, EventArgs.Empty });
            Assert(!ReinstallGate.IsRunning && save.IsEnabled, "Settings unlocks after the generation becomes readable");

            ReinstallGate.Set(true);
            File.WriteAllText(App.State.FileLocation, "{\"ForceReinstall\":");
            using (var held = Acquire(exclusive: true)!)
            {
                ReinstallGate.Set(false);
                Assert(!root.IsEnabled && !save.IsEnabled && close.IsEnabled && ReinstallGate.RecoveryPending,
                    "Settings remains unsavable when refresh cannot start");
            }

            void RetryRefresh()
            {
                typeof(MainWindow).GetField("_nextRefreshAttempt", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, DateTime.MinValue);
                typeof(MainWindow).GetMethod("ReinstallGate_Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(window, new object?[] { window, EventArgs.Empty });
            }
            RetryRefresh();
            Assert(ReinstallGate.RecoveryPending && !save.IsEnabled && File.ReadAllText(App.State.FileLocation) == "{\"ForceReinstall\":",
                "failed refresh keeps Settings locked and preserves the file");
            File.Delete(App.State.FileLocation);
            RetryRefresh();
            Assert(ReinstallGate.RecoveryPending && !save.IsEnabled && !File.Exists(App.State.FileLocation),
                "a missing previously loaded state file is not silently recreated");
            File.WriteAllText(App.State.FileLocation, "{\"ForceReinstall\":false}");
            RetryRefresh();
            Assert(!ReinstallGate.RecoveryPending && save.IsEnabled && !App.State.Prop.ForceReinstall,
                "Settings retries and unlocks after state becomes readable");

            // QA_BUILD isolates the named marker from any installed Vexstrap process.
            if (ProjectName == "Vexstrap-QA")
            {
                var holders = new List<Process>();
                Process HoldMarker()
                {
                    var p = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "hold-marker")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    })!;
                    holders.Add(p);
                    Assert(p.StandardOutput.ReadLine() == "ready", "separate process holds reinstall marker");
                    return p;
                }
                void StopHolder(Process p) { p.Kill(); p.WaitForExit(); p.Dispose(); holders.Remove(p); }
                try
                {
                    var settingsProcess = HoldMarker();
                    var reinstallProcess = HoldMarker();
                    foreach (var (mode, expected) in new[] { ("blocked-player", 1602), ("blocked-background", 0), ("blocked-uninstall", 1602) })
                    {
                        using var launch = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, mode)
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true
                        })!;
                        launch.WaitForExit();
                        Assert(launch.ExitCode == expected, $"{mode} route exits without changing the installation");
                    }
                    StopHolder(settingsProcess);
                    var reopened = new MainWindow(false);
                    var reopenedSave = (UIElement)reopened.FindName("SaveButton");
                    Assert(!reopenedSave.IsEnabled, "reopened Settings remains locked after original Settings is killed");
                    File.WriteAllText(App.State.FileLocation, "{\"ForceReinstall\":true}");
                    StopHolder(reinstallProcess);
                    typeof(MainWindow).GetMethod("ReinstallGate_Changed", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(reopened, new object?[] { reopened, EventArgs.Empty });
                    Assert(reopenedSave.IsEnabled && App.State.Prop.ForceReinstall,
                        "reopened Settings refreshes and unlocks after reinstall process exits");
                }
                finally
                {
                    foreach (var p in holders) { if (!p.HasExited) p.Kill(); p.WaitForExit(); p.Dispose(); }
                }
            }
            App.FastFlags.Save(); // Keep the hidden test windows free of unsaved-change prompts.
            app.Shutdown();
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static void Assert(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Console.WriteLine("PASS " + description);
    }

    public sealed class TestLoadState { public int Value { get; set; } }
}
