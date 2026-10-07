using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Vexstrap
{
    public class FlagInjector
    {
        [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")] static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")] static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr hObject);

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const string OFFSETS_URL = "https://raw.githubusercontent.com/LeonardoDecap/vexstrap-offsets/main/offsets.json";
        private static readonly HttpClient _http = new();
        private static CancellationTokenSource? _cts;

        public static async Task StartAsync(Dictionary<string, string> flags)
        {
            _cts = new CancellationTokenSource();
            await Task.Run(() => InjectLoop(flags, _cts.Token));
        }

        public static void Stop() => _cts?.Cancel();

        private static async Task InjectLoop(Dictionary<string, string> flags, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var offsets = await FetchOffsets();
                    var procs = Process.GetProcessesByName("RobloxPlayerBeta");
                    if (procs.Length == 0) { await Task.Delay(5000, token); continue; }

                    var proc = procs[0];
                    IntPtr handle = OpenProcess(PROCESS_ALL_ACCESS, false, proc.Id);
                    if (handle == IntPtr.Zero) { await Task.Delay(5000, token); continue; }

                    foreach (var flag in flags)
                    {
                        if (!offsets.ContainsKey(flag.Key)) continue;
                        long offset = Convert.ToInt64(offsets[flag.Key], 16);
                        IntPtr addr = new IntPtr(proc.MainModule!.BaseAddress.ToInt64() + offset);
                        byte[] val = Encoding.UTF8.GetBytes(flag.Value + "\0");
                        WriteProcessMemory(handle, addr, val, val.Length, out _);
                    }

                    CloseHandle(handle);
                }
                catch { }

                await Task.Delay(300000, token); // re-inject every 5 mins
            }
        }

        private static async Task<Dictionary<string, string>> FetchOffsets()
        {
            var json = await _http.GetStringAsync(OFFSETS_URL);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
    }
}
