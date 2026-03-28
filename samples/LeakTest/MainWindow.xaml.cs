using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace WinUI3Leak;

public sealed partial class MainWindow : Window
{
    private const int WindowCount = 5;
    private const int DelayMs = 2000;

    private readonly List<WeakReference> _xBindWeak = new();
    private readonly List<WeakReference> _normalWeak = new();

    public MainWindow()
    {
        InitializeComponent();
        LeakTracker.Init();

        LeakTracker.Log("========================================");
        LeakTracker.Log("Testing PATCHED XBindWindow.g.cs");
        LeakTracker.Log("  StopTracking() now does: Activated -=, dataRoot = null");
        LeakTracker.Log("  GetBindingConnector now adds: Closed += StopTracking()");
        LeakTracker.Log("========================================");

        Status("Auto test in 1s...");
        Schedule(1000, RunTest);
    }

    private void RunTest()
    {
        Status("Phase 1: Opening windows...");
        LeakTracker.Log($"[Phase 1] Opening {WindowCount} x:Bind + {WindowCount} Normal windows");

        var xBindWindows = new List<Window>();
        var normalWindows = new List<Window>();

        for (int i = 0; i < WindowCount; i++)
        {
            var xw = new XBindWindow();
            xBindWindows.Add(xw);
            _xBindWeak.Add(new WeakReference(xw));
            xw.Activate();

            var nw = new NormalWindow();
            normalWindows.Add(nw);
            _normalWeak.Add(new WeakReference(nw));
            nw.Activate();
        }

        Schedule(DelayMs, () =>
        {
            Status("Phase 2: Closing...");
            LeakTracker.Log("[Phase 2] Closing all windows");

            foreach (var w in xBindWindows) try { w.Close(); } catch { }
            foreach (var w in normalWindows) try { w.Close(); } catch { }
            xBindWindows.Clear();
            normalWindows.Clear();

            Schedule(DelayMs, () =>
            {
                Status("Phase 3: GC...");
                DoGC("GC Pass 1");

                Schedule(DelayMs, () =>
                {
                    DoGC("GC Pass 2");

                    int xAlive = 0, nAlive = 0;
                    foreach (var wr in _xBindWeak) if (wr.IsAlive) xAlive++;
                    foreach (var wr in _normalWeak) if (wr.IsAlive) nAlive++;

                    LeakTracker.Log("");
                    LeakTracker.Log("[WeakRef] x:Bind alive: " + xAlive + "/" + _xBindWeak.Count);
                    LeakTracker.Log("[WeakRef] Normal alive: " + nAlive + "/" + _normalWeak.Count);

                    LeakTracker.Log("");
                    LeakTracker.Log("========== VERDICT ==========");

                    int xLeak = LeakTracker.XBindCreated - LeakTracker.XBindFinalized;
                    int nLeak = LeakTracker.NormalCreated - LeakTracker.NormalFinalized;

                    LeakTracker.Log(xLeak == 0
                        ? $"  [PASS] x:Bind (PATCHED): all {LeakTracker.XBindCreated} finalized!"
                        : $"  [FAIL] x:Bind: {xLeak}/{LeakTracker.XBindCreated} LEAKED");

                    LeakTracker.Log(nLeak == 0
                        ? $"  [PASS] Normal: all {LeakTracker.NormalCreated} finalized"
                        : $"  [FAIL] Normal: {nLeak}/{LeakTracker.NormalCreated} leaked");

                    if (xLeak == 0 && nLeak == 0)
                        LeakTracker.Log("  >> T4 template fix VERIFIED!");

                    LeakTracker.Log("=============================");
                    LeakTracker.Log("Test complete.");

                    Status("Done. Check leak_test.log");
                    Schedule(2000, Close);
                });
            });
        });
    }

    private void DoGC(string label)
    {
        long before = GC.GetTotalMemory(false);
        for (int i = 0; i < 5; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
        long after = GC.GetTotalMemory(false);
        LeakTracker.Log($"[{label}] {Fmt(before)} -> {Fmt(after)} (freed {Fmt(before - after)})");
        LeakTracker.Log($"  x:Bind: created={LeakTracker.XBindCreated}, finalized={LeakTracker.XBindFinalized}");
        LeakTracker.Log($"  Normal: created={LeakTracker.NormalCreated}, finalized={LeakTracker.NormalFinalized}");
    }

    private void Schedule(int ms, Action action)
    {
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        t.Tick += (_, _) => { t.Stop(); action(); };
        t.Start();
    }

    private void Status(string msg) => StatusText.Text = msg;

    private static string Fmt(long b) =>
        b < 0 ? $"-{Fmt(-b)}" : b >= 1_048_576 ? $"{b / 1_048_576.0:F2} MB" : $"{b / 1_024.0:F2} KB";
}
