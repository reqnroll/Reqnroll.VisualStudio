#nullable disable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.UI.Controls;

namespace Reqnroll.VisualStudio.UI;

public class DialogWindow : Window
{
    private readonly IVsUIShell _vsUiShell;
    public EventHandler<RequestNavigateEventArgs> LinkClicked;

    static DialogWindow()
    {
        //DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogWindow), new FrameworkPropertyMetadata(typeof(DialogWindow)));
    }

    public DialogWindow() : this(null)
    {
    }

    public DialogWindow(IVsUIShell vsUiShell)
    {
        _vsUiShell = vsUiShell;
    }

    public bool IsHostedInVs => _vsUiShell != null;

    public bool? ShowModal()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (IsHostedInVs)
        {
            int num = WindowHelper.ShowModal(this);
            if (num == 0)
                return null;
            return num == 1;
        }

        return ShowDialog();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AddHandler(MarkDownTextBlock.LinkClickedEvent, new RequestNavigateEventHandler(OnLinkClicked));
        AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnMouseLeftButtonDown));
    }

    protected virtual void OnLinkClicked(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        Process.Start(e.Uri.ToString());
        LinkClicked?.Invoke(sender, e);
    }

    public void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        // Begin dragging the window
        DragMove();
    }

    protected void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    protected void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    #region VsDialogWindow

    public static readonly DependencyProperty HasMaximizeButtonProperty =
        DependencyProperty.Register(nameof(HasMaximizeButton), typeof(bool), typeof(DialogWindow),
            new FrameworkPropertyMetadata(Boxes.BooleanFalse, OnWindowStyleChanged));

    public static readonly DependencyProperty HasMinimizeButtonProperty =
        DependencyProperty.Register(nameof(HasMinimizeButton), typeof(bool), typeof(DialogWindow),
            new FrameworkPropertyMetadata(Boxes.BooleanFalse, OnWindowStyleChanged));

    public static readonly DependencyProperty HasDialogFrameProperty =
        DependencyProperty.Register(nameof(HasDialogFrame), typeof(bool), typeof(DialogWindow),
            new FrameworkPropertyMetadata(Boxes.BooleanTrue, OnWindowStyleChanged));

    public static readonly DependencyProperty HasHelpButtonProperty = DependencyProperty.Register(nameof(HasHelpButton),
        typeof(bool), typeof(DialogWindow), new FrameworkPropertyMetadata(Boxes.BooleanFalse, OnWindowStyleChanged));

    public static readonly DependencyProperty IsCloseButtonEnabledProperty =
        DependencyProperty.Register(nameof(IsCloseButtonEnabled), typeof(bool), typeof(DialogWindow),
            new PropertyMetadata(Boxes.BooleanTrue, OnWindowStyleChanged));

    private HwndSource _hwndSource;

    public bool HasMaximizeButton
    {
        get => (bool) GetValue(HasMaximizeButtonProperty);
        set => SetValue(HasMaximizeButtonProperty, Boxes.Box(value));
    }

    public bool HasMinimizeButton
    {
        get => (bool) GetValue(HasMinimizeButtonProperty);
        set => SetValue(HasMinimizeButtonProperty, Boxes.Box(value));
    }

    public bool HasDialogFrame
    {
        get => (bool) GetValue(HasDialogFrameProperty);
        set => SetValue(HasDialogFrameProperty, Boxes.Box(value));
    }

    public bool HasHelpButton
    {
        get => (bool) GetValue(HasHelpButtonProperty);
        set => SetValue(HasHelpButtonProperty, Boxes.Box(value));
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the close title bar button should be enabled.
    /// </summary>
    public bool IsCloseButtonEnabled
    {
        get => (bool) GetValue(IsCloseButtonEnabledProperty);
        set => SetValue(IsCloseButtonEnabledProperty, value);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = (HwndSource) PresentationSource.FromVisual(this);
        if (_hwndSource != null)
        {
            _hwndSource.AddHook(WndProcHook);
            UpdateWindowStyle();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_hwndSource != null)
        {
            _hwndSource.Dispose();
            _hwndSource = null;
        }

        base.OnClosed(e);
    }

    private static void OnWindowStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        ((DialogWindow) obj).UpdateWindowStyle();
    }

    private void UpdateWindowStyle()
    {
        if (_hwndSource == null)
            return;
        IntPtr handle = _hwndSource.Handle;
        if (handle == IntPtr.Zero)
            return;
        int windowLong1 = NativeMethods.GetWindowLong(handle, -16);
        int num1 = !HasMaximizeButton ? windowLong1 & -65537 : windowLong1 | 65536;
        int num2 = !HasMinimizeButton ? num1 & -131073 : num1 | 131072;
        NativeMethods.SetWindowLong(handle, -16, num2);
        int windowLong2 = NativeMethods.GetWindowLong(handle, -20);
        int num3 = !HasDialogFrame ? windowLong2 & -2 : windowLong2 | 1;
        int num4 = !HasHelpButton ? num3 & -1025 : num3 | 1024;
        NativeMethods.SetWindowLong(handle, -20, num4);
        NativeMethods.SendMessage(handle, 128, new IntPtr(1), IntPtr.Zero);
        NativeMethods.SendMessage(handle, 128, new IntPtr(0), IntPtr.Zero);
        IntPtr systemMenu = NativeMethods.GetSystemMenu(handle, false);
        if (systemMenu != IntPtr.Zero)
        {
            uint num5 = IsCloseButtonEnabled ? 0U : 1U;
            NativeMethods.EnableMenuItem(systemMenu, 61536U, 0U | num5);
        }

        NativeMethods.SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 35);
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 274 && wParam.ToInt32() == 61824)
        {
            InvokeDialogHelp();
            handled = true;
        }

        if (msg == 83)
        {
            InvokeDialogHelp();
            handled = true;
        }

        if (msg == 26 && wParam.ToInt32() == 67 || msg == 21)
        {
            OnDialogThemeChanged();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height,
            uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hwnd, bool bRevert);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnableMenuItem(IntPtr menu, uint uIDEnableItem, uint uEnable);
    }

    protected virtual void OnDialogThemeChanged()
    {
    }

    protected virtual void InvokeDialogHelp()
    {
    }

    #endregion
}
