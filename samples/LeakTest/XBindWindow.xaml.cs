using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace WinUI3Leak;

public sealed partial class XBindWindow : Window, INotifyPropertyChanged
{
    // Large buffer to make memory leak clearly visible in diagnostics
    private readonly byte[] _largeBuffer = new byte[10 * 1024 * 1024]; // 10 MB

    private int _clickCount;
    public int ClickCount
    {
        get => _clickCount;
        set { _clickCount = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> DummyItems { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public XBindWindow()
    {
        InitializeComponent();
        LeakTracker.OnXBindCreated();

        for (int i = 0; i < 100; i++)
            DummyItems.Add($"x:Bind Item {i}");

        // Touch the buffer so it won't be optimized away
        _largeBuffer[0] = 1;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        ClickCount++;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    ~XBindWindow()
    {
        LeakTracker.OnXBindFinalized();
        System.Diagnostics.Debug.WriteLine("*** XBindWindow FINALIZED ***");
    }
}
