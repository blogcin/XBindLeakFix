using System;
using Microsoft.UI.Xaml;

namespace WinUI3Leak;

public sealed partial class NormalWindow : Window
{
    // Same large buffer to make comparison fair
    private readonly byte[] _largeBuffer = new byte[10 * 1024 * 1024]; // 10 MB

    private int _clickCount;

    public NormalWindow()
    {
        InitializeComponent();
        LeakTracker.OnNormalCreated();

        _largeBuffer[0] = 1;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        _clickCount++;
        StatusText.Text = _clickCount.ToString();
    }

    ~NormalWindow()
    {
        LeakTracker.OnNormalFinalized();
        System.Diagnostics.Debug.WriteLine("*** NormalWindow FINALIZED ***");
    }
}
