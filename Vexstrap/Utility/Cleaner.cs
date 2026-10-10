namespace Vexstrap.Utility
{
    static class Cleaner
    {
        private const string LOG_IDENT = "Cleaner";

        private static readonly string[] RobloxProcessNames = { "RobloxPlayerBeta", "RobloxStudioBeta" };

        // folders inside Temp\Vexstrap that must never be touched
        private static readonly string[] ProtectedTempFolders = { "Updates", "Logs" };

        /// <summary>
        /// Cleans temp files, the Roblox download cache and old Roblox versions.
        /// Returns bytes freed, or null if cleanup was skipped because Roblox or another Vexstrap window is running.
        /// </summary>
        public static long? Run()
        {
            if (IsBlocked())
            {
                App.Logger.WriteLine(LOG_IDENT, "Skipped: Roblox or another Vexstrap instance is running.");
                return null;
            }

            long before = MeasureTargets();

            ClearContents(Paths.Temp, ProtectedTempFolders);
            ClearContents(Paths.Downloads);

            // keeps the current Roblox version, and skips versions that are still running
            Bootstrapper.CleanupVersionsFolder();

            long after = MeasureTargets();
            long freed = Math.Max(0, before - after);

            App.Logger.WriteLine(LOG_IDENT, $"Cleanup freed {FormatBytes(freed)}.");

            return freed;
        }

        /// <summary>
        /// Runs cleanup if the chosen schedule says it is due. Called once at startup.
        /// </summary>
        public static void RunScheduledIfDue()
        {
            var settings = App.Settings.Prop;

            if (settings.CleanupSchedule == CleanupFrequency.Off)
                return;

            // first time on (or never run): start the clock, do not clean yet
            if (settings.LastCleanupUtc == default)
            {
                settings.LastCleanupUtc = DateTime.UtcNow;
                App.Settings.Save();
                App.Logger.WriteLine(LOG_IDENT, "Scheduled cleanup armed. First run is one interval from now.");
                return;
            }

            DateTime due = settings.CleanupSchedule switch
            {
                CleanupFrequency.Daily => settings.LastCleanupUtc.AddDays(1),
                CleanupFrequency.Weekly => settings.LastCleanupUtc.AddDays(7),
                _ => settings.LastCleanupUtc.AddMonths(1),
            };

            if (DateTime.UtcNow < due)
                return;

            long? freed = Run();

            if (freed is null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Scheduled cleanup is due but was skipped. Will retry on next launch.");
                return;
            }

            settings.LastCleanupUtc = DateTime.UtcNow;
            App.Settings.Save();
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

        private static void ClearContents(string dir, params string[] keep)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            foreach (string entry in Directory.GetFileSystemEntries(dir))
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
                    App.Logger.WriteLine(LOG_IDENT, $"Could not remove {entry}: {ex.Message}");
                }
            }
        }
    }
}