using Noats.Services;
using Noats.Views.Windows;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Noats;

public partial class App : Application
{
    private HotkeyService? _hotkeyService;
    private ThemeService _themeService = new();
    private Window? _hiddenWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create hidden window to host hotkeys
        _hiddenWindow = new Window
        {
            Width = 0,
            Height = 0,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = null,
            Visibility = Visibility.Hidden
        };
        _hiddenWindow.Show();

        // Initialize hotkey service
        _hotkeyService = new HotkeyService(_hiddenWindow, CreateNewNoat);
    }

    private void CreateNewNoat()
    {
        Dispatcher.Invoke(() =>
        {
            var noat = new NoatWindow(_themeService);
            noat.Show();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Cleanup();
        base.OnExit(e);
    }
}
