// Motion - project memory, requirements and status live in PROJECT_NOTES.cs. Keep it updated when this file's behavior changes.
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Media;
using Motion.Services;

namespace Motion;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Navigation icons: glyph characters from the bundled Font Awesome font; the nav button template shows Tag.
        NavHome.Tag = Icons.Glyph("house");
        NavDetect.Tag = Icons.Glyph("crosshairs");
        NavButtons.Tag = Icons.Glyph("hand-pointer");
        NavGestures.Tag = Icons.Glyph("arrows-up-down-left-right");
        NavWheel.Tag = Icons.Glyph("circle-nodes");
        NavActions.Tag = Icons.Glyph("bolt");
        NavProfiles.Tag = Icons.Glyph("layer-group");
        NavSettings.Tag = Icons.Glyph("gear");

        SavedIcon.Text = Icons.Glyph("check");
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this); // dark title bar in dark mode
        ThemeManager.Changed += OnThemeChanged;
        UpdateThemeButton();
        ShowPage("Home");
        RefreshStatus();
        RefreshDevice();

        App.Current.Engine.Changed += RefreshStatus;
        App.Current.Profiles.ActiveChanged += RefreshStatus;
        App.Current.Settings.Saved += OnSaved;
        Closed += (_, _) =>
        {
            ThemeManager.Changed -= OnThemeChanged;
            App.Current.Settings.Saved -= OnSaved;
            App.Current.Engine.Changed -= RefreshStatus;
            App.Current.Profiles.ActiveChanged -= RefreshStatus;
        };
    }

    private string _page = "Home";

    private void OnDeviceCardClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Navigate("Home");

    private void OnThemeClick(object sender, RoutedEventArgs e) => ThemeManager.SetDark(!ThemeManager.IsDark);

    private void UpdateThemeButton()
    {
        // the button shows the mode you would switch TO
        ThemeIcon.Text = Icons.Glyph(ThemeManager.IsDark ? "sun" : "moon");
        ThemeButton.ToolTip = ThemeManager.IsDark ? "Switch to light mode" : "Switch to dark mode";
    }

    private void OnThemeChanged()
    {
        UpdateThemeButton();
        ThemeManager.ApplyTitleBar(this);
        RefreshStatus();
        // code-built pages read their colors when built, so rebuild the visible one (after the click that caused the switch)
        Dispatcher.BeginInvoke(new Action(() => ShowPage(_page)), System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>Re-reads the mouse name for the sidebar card (call after renaming).</summary>
    public void RefreshDevice()
    {
        DeviceNames.Refresh();
        DeviceNameText.Text = DeviceNames.Display;
        DeviceSourceText.Text = DeviceNames.Source;
    }

    // Settings can be saved from any thread; the pill is updated on the UI thread.
    private void OnSaved() => Dispatcher.BeginInvoke(new Action(() =>
    {
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(700)) { BeginTime = TimeSpan.FromMilliseconds(1100) };
        SavedPill.BeginAnimation(OpacityProperty, null);
        SavedPill.Opacity = 1;
        SavedPill.BeginAnimation(OpacityProperty, fade);
    }));

    /// <summary>Lets pages jump to another page (for example Profiles -> Buttons).</summary>
    public void Navigate(string page)
    {
        var target = page switch
        {
            "Detect" => NavDetect,
            "Buttons" => NavButtons,
            "Gestures" => NavGestures,
            "Wheel" => NavWheel,
            "Actions" => NavActions,
            "Profiles" => NavProfiles,
            "Settings" => NavSettings,
            _ => NavHome
        };
        target.IsChecked = true;
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        // Checked fires once during InitializeComponent, before the pages exist.
        if (SettingsPage is null || SettingsContent is null) return;
        string page = NavSettings.IsChecked == true ? "Settings"
            : NavProfiles.IsChecked == true ? "Profiles"
            : NavActions.IsChecked == true ? "Actions"
            : NavWheel.IsChecked == true ? "Wheel"
            : NavGestures.IsChecked == true ? "Gestures"
            : NavDetect.IsChecked == true ? "Detect"
            : NavButtons.IsChecked == true ? "Buttons" : "Home";
        ShowPage(page);
    }

    private void ShowPage(string name)
    {
        _page = name;
        void Set(ScrollViewer page, string key) =>
            page.Visibility = name == key ? Visibility.Visible : Visibility.Collapsed;

        Set(HomePage, "Home");
        Set(DetectPage, "Detect");
        Set(ButtonsPage, "Buttons");
        Set(GesturesPage, "Gestures");
        Set(WheelPage, "Wheel");
        Set(ActionsPage, "Actions");
        Set(ProfilesPage, "Profiles");
        Set(SettingsPage, "Settings");

        switch (name)
        {
            case "Home": HomeContent.Reload(); break;
            case "Detect": DetectContent.Reload(); break;
            case "Buttons": ButtonsContent.Reload(); break;
            case "Gestures": GesturesContent.Reload(); break;
            case "Wheel": WheelContent.Reload(); break;
            case "Actions": ActionsContent.Reload(); break;
            case "Profiles": ProfilesContent.Reload(); break;
            case "Settings": SettingsContent.Reload(); break;
        }

        PageTitle.Text = name switch
        {
            "Home" => Greeting(),
            "Wheel" => "Actions Ring",
            "Actions" => "Smart Actions",
            "Profiles" => "App profiles",
            "Detect" => "Detect mouse",
            _ => name
        };
        Crumb.Text = name switch
        {
            "Home" => "Overview", "Wheel" => "Actions Ring", "Actions" => "Smart Actions", "Profiles" => "App profiles", "Detect" => "Detect mouse", _ => name
        };
        PageSub.Text = name switch
        {
            "Detect" => "Press your mouse buttons and see exactly what Windows receives.",
            "Buttons" => "Choose what each mouse button does. Not sure which is which? Use Detect mouse first.",
            "Gestures" => "Hold a button, move the mouse in a direction, and release to run an action.",
            "Wheel" => "Put your most-used commands a gesture away: a circular menu that opens around your cursor.",
            "Actions" => "Create repeatable workflows from simple steps, then use them on any button, gesture or ring item.",
            "Profiles" => "Give every app its own shortcuts and controls. Motion switches automatically when you change windows.",
            "Settings" => "Startup, scrolling, troubleshooting and backups. Changes save automatically.",
            _ => "Your workspace, your controls. Everything in one place."
        };
    }

    private static string Greeting()
    {
        int hour = DateTime.Now.Hour;
        return hour < 12 ? "Good morning." : hour < 18 ? "Good afternoon." : "Good evening.";
    }

    private void RefreshStatus()
    {
        bool on = App.Current.Engine.Enabled;
        StatusText.Text = on ? "Engine enabled" : "Engine paused"; // top bar, next to the theme switch
        StatusSub.Text = on ? $"Profile: {App.Current.Profiles.Active.Name}" : "Paused. Your mouse behaves normally.";
        StatusDot.Fill = (Brush)FindResource(on ? "GreenBrush" : "MutedBrush");
        StatusDot.Effect = on
            ? new System.Windows.Media.Effects.DropShadowEffect { Color = ((SolidColorBrush)StatusDot.Fill).Color, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.6 }
            : null; // soft glow like the prototype's status dot
    }
}
