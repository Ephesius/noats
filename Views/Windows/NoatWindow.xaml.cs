using Noats.Models;
using Noats.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Noats.Views.Windows;

public partial class NoatWindow : Window
{
    private bool _isSelected = false;
    private readonly DispatcherTimer _updateTimer;
    private const double MinAspectRatio = 1.0;
    private const double MaxAspectRatio = 1.5;

    private readonly ThemeService _themeService;
    private readonly ThemeDefinition _currentTheme;

    public NoatWindow(ThemeService themeService)
    {
        InitializeComponent();

        _themeService = themeService;
        _currentTheme = _themeService.GetRandomTheme(); // Ensure _currentTheme is initialized
        ApplyTheme(_currentTheme);

        MouseLeftButtonDown += NoatWindow_MouseLeftButtonDown;
        PreviewKeyDown += NoatWindow_PreviewKeyDown;
        Deactivated += NoatWindow_Deactivated;
        SizeChanged += NoatWindow_SizeChanged;
        ContentBox.TextChanged += ContentBox_TextChanged;

        // Setup resize throttling
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _updateTimer.Tick += UpdateTimer_Tick;

        ContentBox.IsReadOnly = false;
        ContentBox.IsHitTestVisible = true;
        _isSelected = true;
        UpdateSelectionState();
        ContentBox.Focus();
    }

    private void UpdateSelectionState()
    {
        if (_isSelected)
        {
            MainBorder.BorderThickness = new Thickness(2);
            MainBorder.BorderBrush = FindResource($"{_currentTheme.Name}.Selection") as SolidColorBrush;
        }
        else
        {
            MainBorder.BorderThickness = new Thickness(0);
            MainBorder.BorderBrush = null;
        }
    }

    private void ApplyTheme(ThemeDefinition theme)
    {
        MainBorder.Background = FindResource($"{theme.Name}.Background") as SolidColorBrush;
        ContentBox.Foreground = FindResource($"{theme.Name}.Text") as SolidColorBrush;

        UpdateSelectionState();
    }

    private void NoatWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSelected = true;
        UpdateSelectionState();
        Focus();
        DragMove();
    }

    private void NoatWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Rest of the handler stays exactly the same
        if (_isSelected && e.Key == Key.E && ContentBox.IsReadOnly)
        {
            ContentBox.IsReadOnly = false;
            ContentBox.IsHitTestVisible = true;
            ContentBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ExitEditMode();
            _isSelected = false;
            UpdateSelectionState();
        }
        else if (_isSelected && e.Key == Key.Delete && ContentBox.IsReadOnly)
        {
            Close();
            e.Handled = true;
        }
        else if (_isSelected && e.Key == Key.H && ContentBox.IsReadOnly)
        {
            Hide();
            _isSelected = false;
            UpdateSelectionState();
            e.Handled = true;
        }
    }

    private void NoatWindow_Deactivated(object? sender, EventArgs e)
    {
        ExitEditMode();
        _isSelected = false;
        UpdateSelectionState();
    }

    private void ExitEditMode()
    {
        ContentBox.IsReadOnly = true;
        ContentBox.IsHitTestVisible = false;
    }

    private void NoatWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _updateTimer.Stop();
        _updateTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        _updateTimer.Stop();
        UpdateLayout();
    }

    private new void UpdateLayout()
    {
        Dispatcher.Invoke(() =>
        {
            // Update size based on content
            ContentBox.Measure(new Size(Width, double.PositiveInfinity));
            var desiredSize = ContentBox.DesiredSize.Height + ContentBox.Padding.Top + ContentBox.Padding.Bottom;
            var newHeight = Math.Max(40, desiredSize);

            // Apply new size and enforce aspect ratio
            Height = newHeight;
            Width = Math.Max(Width, Height * MinAspectRatio);

            var currentRatio = Width / Height;
            if (currentRatio < MinAspectRatio)
            {
                Height = Width / MinAspectRatio;
            }
            else if (currentRatio > MaxAspectRatio)
            {
                Height = Width / MaxAspectRatio;
            }
        }, DispatcherPriority.Render);
    }

    private void ContentBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _updateTimer.Stop();
        _updateTimer.Start();
    }
}
