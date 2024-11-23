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

    private const int HOTKEY_NEW = 9000;
    private const int HOTKEY_HIDE = 9001;
    private const int HOTKEY_UNHIDE = 9002;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_J = 0x4A;
    private const uint VK_H = 0x48;
    private const uint VK_U = 0x55;

    private IntPtr _windowHandle;
    private readonly Window _window;
    private readonly Action _createNoatAction;
    private readonly Action _hideAllAction;
    private readonly Action _unhideAllAction;

    public HotkeyService(Window window,
        Action createNoatAction,
        Action hideAllAction,
        Action unhideAllAction)
    {
        _window = window;
        _createNoatAction = createNoatAction;
        _hideAllAction = hideAllAction;
        _unhideAllAction = unhideAllAction;
        Initialize();
    }

    private void Initialize()
    {
        var helper = new WindowInteropHelper(_window);
        _windowHandle = helper.Handle;
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        RegisterHotKey(_windowHandle, HOTKEY_NEW, MOD_CONTROL, VK_J);
        RegisterHotKey(_windowHandle, HOTKEY_HIDE, MOD_CONTROL, VK_H);
        RegisterHotKey(_windowHandle, HOTKEY_UNHIDE, MOD_CONTROL, VK_U);
    }

    private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message != 0x0312) return;

        switch (msg.wParam.ToInt32())
        {
            case HOTKEY_NEW:
                _createNoatAction.Invoke();
                handled = true;
                break;
            case HOTKEY_HIDE:
                _hideAllAction.Invoke();
                handled = true;
                break;
            case HOTKEY_UNHIDE:
                _unhideAllAction.Invoke();
                handled = true;
                break;
        }
    }

    public void Cleanup()
    {
        UnregisterHotKey(_windowHandle, HOTKEY_NEW);
        UnregisterHotKey(_windowHandle, HOTKEY_HIDE);
        UnregisterHotKey(_windowHandle, HOTKEY_UNHIDE);
        ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;
    }
}
