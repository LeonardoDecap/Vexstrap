namespace Vexstrap.Utility
{
    /// <summary>Coordinates bootstrapper processes without serializing ordinary launches.</summary>
    internal static class ReinstallOperationLock
    {
        internal static FileStream? TryAcquire(string path, bool exclusive)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate,
                    exclusive ? FileAccess.ReadWrite : FileAccess.Read,
                    exclusive ? FileShare.None : FileShare.Read);
            }
            catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33)
            {
                return null;
            }
        }

        internal static FileStream? TryStart(string path, bool reinstall)
        {
            if (!reinstall && UI.ReinstallGate.IsRunning)
                return null;

            FileStream? handle = TryAcquire(path, reinstall);
            if (handle is not null && !reinstall && UI.ReinstallGate.IsRunning)
            {
                handle.Dispose();
                return null;
            }

            return handle;
        }

        internal static FileStream? TryStart(bool reinstall) =>
            TryStart(Path.Combine(Paths.Temp, "Locks", "RobloxOperation.lock"), reinstall);

        internal static FileStream? TryAcquire(bool exclusive) =>
            TryAcquire(Path.Combine(Paths.Temp, "Locks", "RobloxOperation.lock"), exclusive);
    }
}
