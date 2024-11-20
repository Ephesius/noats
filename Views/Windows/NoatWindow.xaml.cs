using Noats.Models;
using Noats.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Noats.Views.Windows;

public partial class NoatWindow : Window
{
    private bool _isSelected = false;
    private readonly DispatcherTimer _resizeTimer;
    private readonly DispatcherTimer _textChangeTimer;
    private const double MinAspectRatio = 1.0;
    private const double MaxAspectRatio = 1.5;

    private readonly ThemeService _themeService;
    private ThemeDefinition _currentTheme;

    public NoatWindow(ThemeService themeService)
    {
        InitializeComponent();

        _themeService = new ThemeService();
        ApplyRandomTheme();

        MouseLeftButtonDown += NoatWindow_MouseLeftButtonDown;
        KeyDown += NoatWindow_KeyDown;
        Deactivated += NoatWindow_Deactivated;
        SizeChanged += NoatWindow_SizeChanged;
        ContentBox.TextChanged += ContentBox_TextChanged;

        // Setup resize throttling
        _resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _resizeTimer.Tick += ResizeTimer_Tick;

        // Setup text change throttling
        _textChangeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _textChangeTimer.Tick += TextChangeTimer_Tick;

        ContentBox.IsReadOnly = false;
        ContentBox.IsHitTestVisible = true;
        _isSelected = true;
        MainBorder.BorderThickness = new Thickness(2);
        MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082"));
        ContentBox.Focus();
    }

    private void ApplyRandomTheme()
    {
        _currentTheme = _themeService.GetRandomTheme();
        ApplyTheme(_currentTheme);
    }

    private void ApplyTheme(ThemeDefinition theme)
    {
        MainBorder.Background = FindResource($"{theme.Name}.Background") as SolidColorBrush;
        ContentBox.Foreground = FindResource($"{theme.Name}.Text") as SolidColorBrush;

        if (_isSelected)
        {
            MainBorder.BorderBrush = FindResource($"{theme.Name}.Selection") as SolidColorBrush;
        }
    }

    private void NoatWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSelected = true;
        MainBorder.BorderThickness = new Thickness(2);
        MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082"));
        Focus();
        DragMove();
    }

    private void NoatWindow_KeyDown(object sender, KeyEventArgs e)
    {
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
            MainBorder.BorderThickness = new Thickness(0);
        }
    }

    private void NoatWindow_Deactivated(object? sender, EventArgs e)
    {
        ExitEditMode();
        _isSelected = false;
        MainBorder.BorderThickness = new Thickness(0);
    }

    private void ExitEditMode()
    {
        ContentBox.IsReadOnly = true;
        ContentBox.IsHitTestVisible = false;
    }

    private void NoatWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _resizeTimer.Stop();
        _resizeTimer.Start();
    }

    private void ResizeTimer_Tick(object? sender, EventArgs e)
    {
        _resizeTimer.Stop();
        EnforceAspectRatio();
    }

    private void ContentBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _textChangeTimer.Stop();
        _textChangeTimer.Start();
    }

    private void TextChangeTimer_Tick(object? sender, EventArgs e)
    {
        Debug.WriteLine("Entered TextChangeTimer_Tick");
        _textChangeTimer.Stop();
        UpdateSizeBasedOnContent();
    }

    private void UpdateSizeBasedOnContent()
    {
        // Use Dispatcher.Invoke to ensure we're on the UI thread with the right priority
        Dispatcher.Invoke(() =>
        {
            //Force layout update
            ContentBox.Measure(new Size(Width, double.PositiveInfinity));

            // Get desired size for content
            var desiredSize = ContentBox.DesiredSize.Height + ContentBox.Padding.Top + ContentBox.Padding.Bottom;

            Debug.WriteLine($"Desired Height: {desiredSize}, Current Height: {Height}, Content Height: {ContentBox.ActualHeight}");

            // Maintain minimum size
            var newHeight = Math.Max(40, desiredSize);

            // Apply new size while respecting aspect ratio
            Height = newHeight;
            Width = Math.Max(Width, Height * MinAspectRatio);
        }, DispatcherPriority.Render);
    }

    private void EnforceAspectRatio()
    {
        var currentRatio = Width / Height;

        if (currentRatio < MinAspectRatio)
        {
            Height = Width / MinAspectRatio;
        }
        else if (currentRatio > MaxAspectRatio)
        {
            Height = Width / MaxAspectRatio;
        }
    }
}
