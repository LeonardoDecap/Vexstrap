namespace Vexstrap.Utility
{
    public class CleanupResult
    {
        public bool IsBlocked { get; set; }
        public bool VersionCleanupBlocked { get; set; }
        public int FailedEntries { get; set; }
        public long BytesFreed { get; set; }

        public bool Succeeded => !IsBlocked && !VersionCleanupBlocked && FailedEntries == 0;
    }

    static class Cleaner
    {
        private const string LOG_IDENT = "Cleaner";

        internal static readonly string[] RobloxProcessNames = { "RobloxPlayerBeta", "RobloxStudioBeta", "RobloxCrashHandler" };

        // folders inside Temp\Vexstrap that must never be touched
        private static readonly string[] ProtectedTempFolders = { "Updates", "Logs" };

        /// <summary>
        /// Cleans temp files, the Roblox download cache and old Roblox versions.
        /// Returns a CleanupResult detailing freed bytes, failures, and blocked status.
        /// </summary>
        public static CleanupResult Run()
        {
            if (IsBlocked())
            {
                App.Logger.WriteLine(LOG_IDENT, "Skipped: Roblox or another Vexstrap instance is running.");
                return new CleanupResult { IsBlocked = true };
            }

            long before = MeasureTargets();

            int tempFailures = ClearContents(Paths.Temp, ProtectedTempFolders);
            int downloadFailures = ClearContents(Paths.Downloads);

            // keeps the current Roblox version, and skips versions that are still running
            var (versionFailures, versionBlocked) = Bootstrapper.CleanupVersionsFolder();

            long after = MeasureTargets();
            long freed = Math.Max(0, before - after);

            int totalFailures = tempFailures + downloadFailures + versionFailures;
            App.Logger.WriteLine(LOG_IDENT, $"Cleanup freed {FormatBytes(freed)}. Failures: {totalFailures}, VersionBlocked: {versionBlocked}");

            return new CleanupResult
            {
                IsBlocked = false,
                VersionCleanupBlocked = versionBlocked,
                FailedEntries = totalFailures,
                BytesFreed = freed
            };
        }

        /// <summary>
        /// Migrates LastCleanupUtc from Settings.json to State.json if not yet migrated.
        /// </summary>
        public static void EnsureTimestampMigrated()
        {
            if (App.State.Prop.CleanupTimestampMigrated)
                return;

            if (App.Settings.Prop.LastCleanupUtc != default)
            {
                App.State.Prop.LastCleanupUtc = App.Settings.Prop.LastCleanupUtc;
                App.Settings.Prop.LastCleanupUtc = default;
                App.Settings.Save();
            }

            App.State.Prop.CleanupTimestampMigrated = true;
            App.State.Save();

            App.Logger.WriteLine(LOG_IDENT, "Migrated LastCleanupUtc from Settings.json to State.json.");
        }

        /// <summary>
        /// Runs cleanup if the chosen schedule says it is due. Called once at startup.
        /// </summary>
        public static void RunScheduledIfDue()
        {
            EnsureTimestampMigrated();

            var settings = App.Settings.Prop;

            if (settings.CleanupSchedule == CleanupFrequency.Off)
                return;

            // first time on (or never run): start the clock, do not clean yet
            if (App.State.Prop.LastCleanupUtc == default)
            {
                App.State.Prop.LastCleanupUtc = DateTime.UtcNow;
                App.State.Save();
                App.Logger.WriteLine(LOG_IDENT, "Scheduled cleanup armed. First run is one interval from now.");
                return;
            }

            DateTime due = settings.CleanupSchedule switch
            {
                CleanupFrequency.Daily => App.State.Prop.LastCleanupUtc.AddDays(1),
                CleanupFrequency.Weekly => App.State.Prop.LastCleanupUtc.AddDays(7),
                _ => App.State.Prop.LastCleanupUtc.AddMonths(1),
            };

            if (DateTime.UtcNow < due)
                return;

            CleanupResult result = Run();

            if (result.IsBlocked || result.VersionCleanupBlocked)
            {
                App.Logger.WriteLine(LOG_IDENT, "Scheduled cleanup is due but was blocked. Will retry on next launch.");
                return;
            }

            if (!result.Succeeded)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Scheduled cleanup encountered {result.FailedEntries} failures. Timestamp not advanced.");
                return;
            }

            App.State.Prop.LastCleanupUtc = DateTime.UtcNow;
            App.State.Save();
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / 1024.0 / 1024 / 1024:0.00} GB";

            return $"{bytes / 1024.0 / 1024:0.0} MB";
        }

        private static bool IsBlocked()
        {
            foreach (string name in RobloxProcessNames)
            {
                if (System.Diagnostics.Process.GetProcessesByName(name).Length > 0)
                    return true;
            }

            // this instance counts as one; anything more is another Vexstrap window or the background updater
            return System.Diagnostics.Process.GetProcessesByName(App.ProjectName).Length > 1;
        }

        private static long MeasureTargets()
        {
            return DirectorySize(Paths.Temp) + DirectorySize(Paths.Downloads) + DirectorySize(Paths.Versions);
        }

        private static long DirectorySize(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return 0;

            long total = 0;

            try
            {
                foreach (FileInfo file in new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        total += file.Length;
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }

            return total;
        }

        private static int ClearContents(string dir, params string[] keep)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return 0;

            int failedCount = 0;

            string[] entries;
            try
            {
                entries = Directory.GetFileSystemEntries(dir);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not enumerate entries in {dir}: {ex.Message}");
                return 1;
            }

            foreach (string entry in entries)
            {
                string name = Path.GetFileName(entry);

                if (keep.Contains(name, StringComparer.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (Directory.Exists(entry))
                        Directory.Delete(entry, true);
                    else
                        File.Delete(entry);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    App.Logger.WriteLine(LOG_IDENT, $"Could not remove {entry}: {ex.Message}");
                }
            }

            return failedCount;
        }
    }
}