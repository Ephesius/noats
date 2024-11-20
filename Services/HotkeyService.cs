using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Noats.Services;

public class HotkeyService
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_J = 0x4A;

    private IntPtr _windowHandle;
    private readonly Window _window;
    private readonly Action _createNoatAction;

    public HotkeyService(Window window, Action createNoatAction)
    {
        _window = window;
        _createNoatAction = createNoatAction;
        Initialize();
    }

    private void Initialize()
    {
        var helper = new WindowInteropHelper(_window);
        _windowHandle = helper.Handle;
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_J);
    }

    private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == 0x0312 && msg.wParam.ToInt32() == HOTKEY_ID)
        {
            _createNoatAction.Invoke();
            handled = true;
        }
    }

    public void Cleanup()
    {
        UnregisterHotKey(_windowHandle, HOTKEY_ID);
        ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;
    }
}
