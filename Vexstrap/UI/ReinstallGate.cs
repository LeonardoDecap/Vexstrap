namespace Vexstrap.UI
{
    public static class ReinstallGate
    {
        private static string MarkerName => $"{App.ProjectName}-SettingsReinstall";
        private static bool _localRunning;
        private static Mutex? _marker;
        private static bool _monitorGeneration;
        private static string? _observedGeneration;
        private static string GenerationPath => Path.Combine(Paths.Temp, "Locks", "ReinstallGeneration");

        public static bool IsOperationRunning => _localRunning || Utilities.DoesMutexExist(MarkerName);
        public static bool RecoveryPending { get; set; }
        public static bool IsRunning => IsOperationRunning || RecoveryPending || HasUnseenGeneration();

        public static event EventHandler? Changed;

        // A handle in each process keeps the marker alive if Settings is killed.
        public static void HoldMarker() => _marker ??= new Mutex(false, MarkerName);

        public static void ReleaseMarker()
        {
            _marker?.Dispose();
            _marker = null;
        }

        public static void MarkStarted()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GenerationPath)!);
            File.WriteAllText(GenerationPath, Guid.NewGuid().ToString("N"));
        }

        public static string? ReadGeneration()
        {
            try { return File.ReadAllText(GenerationPath); }
            catch (FileNotFoundException) { return string.Empty; }
            catch (DirectoryNotFoundException) { return string.Empty; }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        public static void BeginMonitoringGeneration(string? initialGeneration)
        {
            _observedGeneration = initialGeneration;
            _monitorGeneration = true;
        }

        internal static bool HasUnseenGeneration()
        {
            if (!_monitorGeneration)
                return false;
            string? generation = ReadGeneration();
            return generation is null || !String.Equals(generation, _observedGeneration, StringComparison.Ordinal);
        }

        public static bool ObserveGenerationChange()
        {
            if (!_monitorGeneration)
                return false;
            string? generation = ReadGeneration();
            bool changed = generation is null || !String.Equals(generation, _observedGeneration, StringComparison.Ordinal);
            if (generation is not null)
                _observedGeneration = generation;
            return changed;
        }

        // Set is called on the Settings UI thread.
        public static void Set(bool running)
        {
            _localRunning = running;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }
}
