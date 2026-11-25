// Motion - project memory, requirements and status live in PROJECT_NOTES.cs. Keep it updated when this file's behavior changes.
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Motion.Overlays;
using Motion.Services;

namespace Motion;

public partial class App : Application
{
    private const string MutexName = @"Local\Motion.SingleInstance";
    private const string ShowEventName = @"Local\Motion.ShowWindow";

    private Mutex? _mutex;
    private TrayService? _tray;
    private MainWindow? _window;

    public static new App Current => (App)Application.Current;

    public SettingsService Settings { get; } = new();
    public EngineState Engine { get; } = new();
    public RemapEngine Remap { get; private set; } = null!;
    public ProfileManager Profiles { get; private set; } = null!;
    public OverlayManager Overlays { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance: a second launch just asks the first one to show its window.
        _mutex = new Mutex(true, MutexName, out bool isFirst);
        var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        if (!isFirst)
        {
            showEvent.Set();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;

        Settings.Load();
        ActionHistory.Enabled = Settings.Current.RecordHistory;
        ThemeManager.Apply(Settings.Current.DarkMode); // before any window is created, so nothing flashes in the wrong theme
        Engine.SetEnabled(Settings.Current.EngineEnabled);
        RefreshStartupEntry();

        _tray = new TrayService(ShowMainWindow, enabled => Engine.SetEnabled(enabled), () => Engine.Enabled, ExitApp);
        Profiles = new ProfileManager(Settings);
        Overlays = new OverlayManager(Settings);
        Remap = new RemapEngine(Profiles, Settings, Overlays);
        Overlays.Execute = Remap.Execute;
        Overlays.ProfileWheel = () => Profiles.Active.Wheel;
        Profiles.Start();
        Remap.SetActive(Engine.Enabled);
        Engine.Changed += () =>
        {
            Settings.Current.EngineEnabled = Engine.Enabled;
            Settings.Save();
            _tray?.SetEnabledState(Engine.Enabled);
            Remap.SetActive(Engine.Enabled);
        };

        var listener = new Thread(() =>
        {
            while (showEvent.WaitOne())
                Dispatcher.BeginInvoke(new Action(ShowMainWindow));
        }) { IsBackground = true };
        listener.Start();

        bool launchedByWindows = e.Args.Contains("--autostart");
        if (!(launchedByWindows && Settings.Current.StartMinimized))
            ShowMainWindow();
        else
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(MemoryTrimmer.Trim));
    }

    public void ShowMainWindow()
    {
        if (_window is null)
        {
            _window = new MainWindow();
            _window.Closed += OnWindowClosed;
            _window.Show();
        }
        else
        {
            if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
            _window.Show();
        }
        _window.Activate();
    }

    public void ExitApp() => Shutdown();

    protected override void OnExit(ExitEventArgs e)
    {
        Remap?.Dispose();
        Profiles?.Dispose();
        _tray?.Dispose();
        try { _mutex?.ReleaseMutex(); } catch { /* not owned (second instance) */ }
        base.OnExit(e);
    }

    // The window is destroyed on close so the UI isn't kept in memory while Motion sits in the tray.
    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _window = null;
        if (!Settings.Current.MinimizeToTray) ExitApp();
        else Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(MemoryTrimmer.Trim)); // tray mode: hand memory back
    }

    // If the EXE was moved, keep the Windows startup entry pointing at the right place.
    private void RefreshStartupEntry()
    {
        try
        {
            if (Settings.Current.StartWithWindows) StartupService.Set(true);
        }
        catch (Exception ex)
        {
            Log.Error("Could not refresh startup entry", ex);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("Unhandled UI exception", e.Exception);
        e.Handled = true;
        MessageBox.Show($"Something went wrong, but Motion is still running.\n\nDetails were saved to:\n{Log.FilePath}",
            "Motion", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
