using Noats.Models;
using Noats.Services;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Noats.Views.Windows;

public partial class NoatWindow : Window
{
    public new string Content
    {
        get => ContentBox.Text;
        set => ContentBox.Text = value;
    }

    public System.Windows.Point Position
    {
        get => new(Left, Top);
        set { Left = value.X; Top = value.Y; }
    }

    private bool _isSelected = false;
    private readonly DispatcherTimer _updateTimer;
    private const double MinAspectRatio = 1.0;
    private const double MaxAspectRatio = 1.5;

    private readonly ThemeService _themeService;
    private readonly ThemeDefinition _currentTheme;

    public string CurrentThemeName => _currentTheme.Name;

    public NoatWindow(ThemeService themeService, ThemeDefinition? initialTheme = null)
    {
        InitializeComponent();

        _themeService = themeService;
        _currentTheme = initialTheme ?? _themeService.GetRandomTheme();
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
        ValidateWindowPosition();
    }

    private void ValidateWindowPosition()
    {
        // Get working area of nearest screen
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)Left, (int)Top));
        var workingArea = screen.WorkingArea;

        // Ensure at least 20px of the window is visible
        if (Left + Width - 20 < workingArea.Left)
            Left = workingArea.Left;
        if (Left + 20 > workingArea.Right)
            Left = workingArea.Right - Width;

        if (Top + Height - 20 < workingArea.Top)
            Top = workingArea.Top;

        if (Top + 20 > workingArea.Bottom)
            Top = workingArea.Bottom - Height;
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

    public void ApplyTheme(ThemeDefinition theme)
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
        ValidateWindowPosition();
    }

    private async void NoatWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Enter edit mode on selected noat
        if (_isSelected && e.Key == Key.E && ContentBox.IsReadOnly)
        {
            ContentBox.IsReadOnly = false;
            ContentBox.IsHitTestVisible = true;
            ContentBox.Focus();
            e.Handled = true;
        }
        // Deselect any and all selected noats
        else if (e.Key == Key.Escape)
        {
            ExitEditMode();
            _isSelected = false;
            UpdateSelectionState();
            await App.Current.SaveStateAsync();
            e.Handled = true;
        }
        // Delete selected noat
        else if (_isSelected && e.Key == Key.Delete && ContentBox.IsReadOnly)
        {
            Close();
            e.Handled = true;
        }
        // Hide selected noat
        else if (_isSelected && e.Key == Key.H && ContentBox.IsReadOnly)
        {
            Hide();
            _isSelected = false;
            UpdateSelectionState();
            await App.Current.SaveStateAsync();
            e.Handled = true;
        }
        // Duplicate selected noat
        else if (_isSelected && e.Key == Key.D && ContentBox.IsReadOnly)
        {
            var newNoat = new NoatWindow(_themeService)
            {
                Content = this.Content,
                Position = new System.Windows.Point(this.Left + 20, this.Top + 20),
                Width = this.Width,
                Height = this.Height
            };

            //newNoat.ApplyTheme(this._currentTheme);
            App.Current.RegisterNoat(newNoat);
            newNoat.Show();
            e.Handled = true;
        }
    }

    private async void NoatWindow_Deactivated(object? sender, EventArgs e)
    {
        ExitEditMode();
        _isSelected = false;
        UpdateSelectionState();
        await App.Current.SaveStateAsync();
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
            // First, measure desired content height
            ContentBox.Measure(new System.Windows.Size(Width, double.PositiveInfinity));
            var desiredHeight = Math.Max(40,
                ContentBox.DesiredSize.Height + ContentBox.Padding.Top + ContentBox.Padding.Bottom);

            // Calculate width bounds based on aspect ratio constraints
            var minWidth = desiredHeight * MinAspectRatio;
            var maxWidth = desiredHeight * MaxAspectRatio;

            // Ensure width stays within aspect ratio bounds
            var newWidth = Math.Max(minWidth, Math.Min(Width, maxWidth));

            // Set final dimensions in one go to avoid jumping
            Width = newWidth;
            Height = desiredHeight;

        }, DispatcherPriority.Render);
    }

    private void ContentBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _updateTimer.Stop();
        _updateTimer.Start();
    }
}
