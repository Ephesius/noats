using Noats.Services;
using Noats.Views.Windows;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Noats;

public partial class App : Application
{
    private HotkeyService? _hotkeyService;
    private readonly ThemeService _themeService = new();
    private Window? _hiddenWindow;
    private readonly List<NoatWindow> _noats = [];

    public static new App Current => (App)Application.Current;

    public void RegisterNoat(NoatWindow noat)
    {
        _noats.Add(noat);
        noat.Closed += (s, _) => _noats.Remove((NoatWindow)s!);
    }

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
        _hotkeyService = new HotkeyService(_hiddenWindow, CreateNewNoat, HideAllNoats);
    }

    private void CreateNewNoat()
    {
        Dispatcher.Invoke(() =>
        {
            var noat = new NoatWindow(_themeService);
            RegisterNoat(noat);
            noat.Show();
        });
    }

    private void HideAllNoats()
    {
        Dispatcher.Invoke(() =>
        {
            foreach (var noat in _noats)
            {
                noat.Hide();
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Cleanup();
        base.OnExit(e);
    }
}
