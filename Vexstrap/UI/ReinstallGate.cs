namespace Vexstrap.UI
{
    public static class ReinstallGate
    {
        public static bool IsRunning { get; private set; }

        public static event EventHandler? Changed;

        // Set must only be called on the UI thread.
        public static void Set(bool running)
        {
            if (IsRunning == running)
                return;

            IsRunning = running;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }
}
