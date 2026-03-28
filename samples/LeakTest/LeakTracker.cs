using System;
using System.IO;
using System.Threading;

namespace WinUI3Leak;

public static class LeakTracker
{
    private static int _xBindCreated;
    private static int _xBindFinalized;
    private static int _normalCreated;
    private static int _normalFinalized;

    private static readonly string LogPath = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "leak_test.log");

    public static int XBindCreated => _xBindCreated;
    public static int XBindFinalized => _xBindFinalized;
    public static int NormalCreated => _normalCreated;
    public static int NormalFinalized => _normalFinalized;

    public static void OnXBindCreated() => Interlocked.Increment(ref _xBindCreated);
    public static void OnXBindFinalized() => Interlocked.Increment(ref _xBindFinalized);
    public static void OnNormalCreated() => Interlocked.Increment(ref _normalCreated);
    public static void OnNormalFinalized() => Interlocked.Increment(ref _normalFinalized);

    public static void Init()
    {
        File.WriteAllText(LogPath, $"=== Leak Test: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
    }

    public static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try { File.AppendAllText(LogPath, line + "\n"); } catch { }
    }
}
