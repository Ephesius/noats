using Noats.Models;
using Noats.Services;
using Noats.Views.Windows;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Noats;

public partial class App : System.Windows.Application
{
    private HotkeyService? _hotkeyService;
    private readonly ThemeService _themeService = new();
    private readonly StorageService _storageService = new();
    private Window? _hiddenWindow;
    private readonly List<NoatWindow> _noats = [];

    public static new App Current => (App)System.Windows.Application.Current;

    public void RegisterNoat(NoatWindow noat)
    {
        _noats.Add(noat);
        noat.Closed += (s, _) =>
        {
            _noats.Remove((NoatWindow)s!);
            Task.Run(async () => await SaveStateAsync());
        };
    }

    public async Task SaveStateAsync()
    {
        // Capture window state on UI thread
        var noatStates = Dispatcher.Invoke(() => _noats.Select(n => new NoatState
        {
            Content = n.Content,
            X = n.Left,
            Y = n.Top,
            Width = n.Width,
            Height = n.Height,
            IsVisible = n.IsVisible,
            ThemeName = n.CurrentThemeName,
            LastModified = DateTime.UtcNow
        }).ToList());

        var state = new AppState
        {
            Noats = noatStates
        };

        await _storageService.SaveStateAsync(state);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        SaveStateAsync().Wait();
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
        _hotkeyService = new HotkeyService(_hiddenWindow,
            CreateNewNoat,
            HideAllNoats,
            UnhideAllNoats,
            LoadSavedState
            );
    }

    private async void LoadSavedState()
    {
        try
        {
            // Step 1: Load the saved state from disk
            var state = await _storageService.LoadStateAsync();

            // Step 2: Switch to UI thread for all window operations
            await Dispatcher.InvokeAsync(() =>
            {
                // Step 3: Safely close existing windows
                var existingWindows = _noats.ToList();
                _noats.Clear();

                foreach (var window in existingWindows)
                {
                    window.Closed -= OnNoatClosed;
                    window.Close();
                }

                // Step 4: Create new windows from saved state
                foreach (var noatState in state.Noats)
                {
                    var theme = noatState.ThemeName != null
                        ? _themeService.GetThemeByName(noatState.ThemeName)
                        : null;

                    var noat = new NoatWindow(_themeService, theme);
                    RegisterNoat(noat);

                    // Set all properties
                    noat.Content = noatState.Content;
                    noat.Width = noatState.Width;
                    noat.Height = noatState.Height;
                    noat.Left = noatState.X;
                    noat.Top = noatState.Y;

                    // Show the window if it was visible
                    if (noatState.IsVisible)
                    {
                        noat.Show();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load state: {ex}");
        }
    }

    private void OnNoatClosed(object? sender, EventArgs e)
    {
        if (sender is NoatWindow noat)
        {
            _noats.Remove(noat);
            Task.Run(async () => await SaveStateAsync());
        }
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

    private async void HideAllNoats()
    {
        Dispatcher.Invoke(() =>
        {
            foreach (var noat in _noats)
            {
                noat.Hide();
            }
        });
        await SaveStateAsync();
    }

    private async void UnhideAllNoats()
    {
        Dispatcher.Invoke(() =>
        {
            foreach (var noat in _noats)
            {
                noat.Show();
            }
        });
        await SaveStateAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Cleanup();
        base.OnExit(e);
    }
}
