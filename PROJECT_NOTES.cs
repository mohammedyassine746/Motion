// =====================================================================================================================
// MOTION - PROJECT MEMORY AND INSTRUCTIONS  (for the next developer or AI)
// =====================================================================================================================
// This file holds the project's memory. It contains no code and is safe to keep in the build.
//
// RULE: every time you change the project, UPDATE THIS FILE (status, decisions, limitations, changelog at the bottom).
// RULE: read this whole file before editing anything. Continue the project; never rewrite or restart it.
//
// ---------------------------------------------------------------------------------------------------------------------
// 1. WHAT MOTION IS
// ---------------------------------------------------------------------------------------------------------------------
// A lightweight, native Windows desktop utility (C#, WPF, .NET 10, x64, Visual Studio Community 2026) that customizes
// mouse behavior: button remapping, gestures, a radial wheel, click-toggle horizontal scrolling, per-app profiles.
// It runs in the system tray with near-zero idle cost. Everything is stored locally; nothing is sent anywhere.
// Not a web app: no WebView. HTML prototypes (Dis_sys.html design system, radial menu prototype) were ADAPTED to native WPF.
// Design: from motion_options_plus_design_concept.html: lime accent (#D9FF43 / #B9DC28), dark theme by default + light mode (Services/ThemeManager.cs swaps brush resources; XAML must use DynamicResource for brushes), rounded cards, Font Awesome icons only (no emoji).
//
// ---------------------------------------------------------------------------------------------------------------------
// 2. HOW THE OWNER WANTS TO WORK  (follow these)
// ---------------------------------------------------------------------------------------------------------------------
// - Owner builds in Visual Studio on their own PC and pastes compile errors back. The AI environment has NO .NET, so
//   nothing here has ever been compiled or run by the AI. Never claim untested behavior works; say what is untested.
// - To save tokens: when fixing errors, edit and send ONLY the files that need changing. For feature drops, send a full zip
//   of the project (folder "Motion", open Motion.csproj in Visual Studio). Unzip over the old folder; settings carry over.
// - Keep explanations brief and to the point.
// - Owner has no mouse purchased yet (considering a Havit GAMS64WB) and wants hardware-independent support.
// - No community marketplace, no multi-computer control, no haptics. Save/transfer via export/import and backup/restore only.
//
// ---------------------------------------------------------------------------------------------------------------------
// 3. REQUIREMENTS (distilled from the owner's four briefs) AND STATUS
// ---------------------------------------------------------------------------------------------------------------------
// Legend: [x] implemented (untested unless noted)  [~] partial  [ ] not started
//
// Foundation
// [x] Single instance (second launch shows the first window), tray icon (Open / Pause / Exit), settings persistence with
//     .bak backup and schema migration, Windows startup (HKCU Run, "--autostart"), start minimized, close-to-tray.
// [x] Window is destroyed on close (tray mode keeps no UI), memory trimmed after close (MemoryTrimmer).
// [x] Self-contained single-file publish settings in Motion.csproj (win-x64). GC tuned for small idle footprint.
// [x] Log file %AppData%\Motion\motion.log (errors/info only; verbose debug logging not built yet).
//
// Input
// [x] Global low-level mouse hook on its own thread (MouseHook). Pausing the engine removes the hook completely.
// [x] Buttons 1-5 + wheel + tilt wheel decoded. Own injected input is ignored (no feedback loops).
// [x] Remapping of Middle/Button 4/Button 5 to shortcuts, media keys, clicks, launch program/URL, disable, wheel, scroll toggle.
// [x] LEFT button is protected (never remapped). RIGHT button can only be a gesture trigger (its click is replayed).
// [x] Detect Mode on the Buttons page (nothing is remapped while detecting) + Raw Input device list (RawDevices).
// [~] Raw Input is used only for device capability info. Windows reports at most 5 buttons + wheels, so DPI/vendor buttons
//     are normally NOT visible to any app; Motion says so in the UI. No vendor/HID support exists.
//
// Gestures  [x]  hold a trigger button and move; 4 or 8 directions; threshold; strictness; trail; per-profile direction actions;
//                click-without-movement behavior (open wheel / toggle horizontal scroll / normal click / nothing).
// [x] Capture shortcuts by pressing keys (v0.4: keyboard button in action picker + Action Builder; any number of keys; UNTESTED).
// [x] Gesture presets (v0.4: Navigation, Browser, Windows, Productivity, Creative, Media; UNTESTED).
// [ ] Live sensitivity/gesture test area, conflict/priority controls between gestures and wheel.
//
// Radial wheel  [x]  native WPF (Overlays/RadialWindow.cs), cursor-anchored, nested subwheels, hub Back/Close, hover feedback,
//                    animation, click item, outside-click/Esc close, size/animation settings, editor with drag-and-drop reorder.
// [x] Shared SYSTEM wheel: always the first slice of every wheel, edited once (AppSettings.SystemWheel).
// [x] Every profile has its own wheel (Profile.Wheel). Max 8 slices per level (root: System + 7 of the profile's own); RadialNode.EnforceLimit moves extras into "More" on load.
// [~] One wheel per profile only (brief mentions "wheel(s)"). Wheel size/animation are global, no per-profile override yet.
// [ ] System wheel cursor effects (Magnifier, Spotlight, Halo, Highlight, Brush/Trail, Click Ripple, Pointer Emphasis).
//
// Profiles  [x]  ordinary list; Default (first, no rules) + ready-made Illustrator, Word, Google Docs, Chrome, Gaming (editable
//                like any profile); multiple apps per profile (exe name + optional window-title text); automatic switching by
//                foreground app (event-driven WinEvent hook); manual lock; priority = list order; drag/drop .exe/.lnk to add an
//                app; export/import/backup/restore; Font Awesome icon per profile (searchable picker).
// [~] Preset shortcuts are the apps' standard Windows defaults but the vendor help pages are script-rendered and could not be
//     machine-verified. Confirmed from search results: Illustrator Ctrl+Z / Shift+Ctrl+Z, Paste in Front Ctrl+F, Back Ctrl+B.
//     ASK THE OWNER TO REVIEW presets against Adobe, Microsoft and Google help pages.
// [ ] Custom image as profile icon (only Font Awesome icons). [ ] Profile-specific wheel presets beyond the five above.
//
// Settings
// [~] Settings are global (gestures, wheel look, horizontal scroll). Profile-level overrides with "reset to global" are NOT built.
// [ ] Settings search / universal search / command palette. [ ] Per-category reset. [ ] Live sensitivity test.
//
// Action Builder / Smart Actions  [~]  v0.4 (UNTESTED): Views/ActionsView.cs page; AppSettings.Sequences; ActionTypes.Sequence (Value = id);
//   steps: Shortcut, Launch, Mouse click, Wait; reorder/enable/duplicate/delete; name+icon; test run (3 s delay); saved actions appear as
//   "Run: name" in every action dropdown (buttons, gestures, wheel items). ActionRunner runs them on a worker thread, no nesting, no re-entry.
//   [ ] UI-automation steps, app-specific commands, Illustrator "New Business Card" (needs dialog keystrokes + Wait steps; fragile by nature).
//   [ ] Profile export/import/backup does NOT include saved actions yet (profiles reference them by id).
//
// Advanced scrolling  [x]  click-toggle horizontal scroll (assign "Toggle horizontal scrolling" to a button); wheel scrolls
//                          horizontally while active; speed, reverse, indicator pill, auto-off timeout.
// Extras  [ ] interactive mouse map  [ ] favorites/recent actions  [x] action history (Settings > Troubleshooting, off by default, v0.4)
//         [ ] battery level (only if hardware reports it)  [ ] RGB (see section 9)
//
// ---------------------------------------------------------------------------------------------------------------------
// 4. ARCHITECTURE MAP (namespace Motion.*)
// ---------------------------------------------------------------------------------------------------------------------
// App.xaml(.cs)               startup, single instance, owns SettingsService, EngineState, ProfileManager, RemapEngine,
//                             OverlayManager, TrayService; closes window to tray; trims memory.
// Models/AppSettings.cs       settings.json root (SchemaVersion 5) + EnsureProfiles() = migration + seeding. Edit migrations HERE.
// Models/Profile.cs           Profile (rules, ButtonMappings, Gestures, Wheel, Icon), AppRule.
// Models/ActionDef.cs         ActionTypes constants + ActionDef (Type, Value). Every feature stores actions in this one shape.
// Models/FeatureSettings.cs   GestureSettings, HScrollSettings, WheelSettings, RadialNode (+ default menus), GestureDefaults.
// Models/ProfilePresets.cs    the ready-made profiles.
// Services/MouseHook.cs       WH_MOUSE_LL on a dedicated thread; MouseEvent; moves reported only while TrackMoves is true.
// Services/RemapEngine.cs     THE brain: decides swallow/pass/act for every event; gestures, scroll toggle, wheel open/close.
// Services/ProfileManager.cs  profile list operations, active-window tracking (SetWinEventHook), import/export/backup.
// Services/ActionCatalog.cs   dropdown presets for buttons / right button / gestures / wheel items.
// Services/ActionRunner.cs, InputSender.cs, ShortcutParser.cs   execute actions via SendInput; "Ctrl+Shift+T", "VolumeUp", "Ctrl+Plus".
// Services/RawDevices.cs      lists mice via Raw Input (capabilities only).
// Services/Icons.cs           Font Awesome loader (see section 6).  Services/NativeMethods.cs  all P/Invoke in one place.
// Services/SettingsService.cs, StartupService.cs, TrayService.cs, EngineState.cs, Log.cs, MemoryTrimmer.cs, GestureMath.cs
// Overlays/*                  RadialWindow (wheel), TrailWindow (gesture trail), IndicatorWindow (scroll pill), OverlayManager
//                             (creates/destroys them on demand, thread-safe for the hook thread), OverlayHelper (DPI-aware
//                             placement, no-activate click-through styles).
// Views/*                     pages (code-built with Views/Ui.cs helpers): Buttons, Gestures, Wheel, Profiles, Settings; HomeView
//                             is XAML. ActionPicker = shared action dropdown. MainWindow = sidebar + pages.
// Themes/Theme.xaml           colors, card/button/toggle/select/input styles, scrollbars, nav template.
// Assets/Fonts, Assets/icons.txt   bundled Font Awesome.
//
// ---------------------------------------------------------------------------------------------------------------------
// 5. KEY DESIGN DECISIONS (do not undo without reason)
// ---------------------------------------------------------------------------------------------------------------------
// - One action shape (ActionDef) everywhere, so buttons, gestures and wheel items share the same picker and runner.
// - Hook callbacks must stay fast: no UI, no disk, no waiting. Work is queued to the ThreadPool or marshalled to the UI thread.
// - Idle cost: no timers, no polling. Overlay windows are created only when needed and destroyed afterwards.
// - Overlays never take focus (WS_EX_NOACTIVATE). The wheel closes on Esc via a temporary RegisterHotKey, and on outside click
//   by the hook (the click is passed through).
// - Foreground window changes: Motion's own windows never change the active profile.
// - Settings are backward compatible: add properties with defaults; migrate in AppSettings.EnsureProfiles(); bump CurrentVersion.
// - Profile order is priority. Default profile is always index 0 and has no rules.
//
// ---------------------------------------------------------------------------------------------------------------------
// 6. ICONS (Font Awesome Free 6.5.1, bundled - the owner rejected hand-drawn icons)
// ---------------------------------------------------------------------------------------------------------------------
// History: v0.2 used the FontAwesome.Sharp NuGet package and icons did not work for the owner. v0.3 replaced it with the real
// font files (Assets/Fonts/fa-solid-900.ttf, fa-brands-400.ttf) loaded as WPF Resources via pack URIs.
// Font family names: "Font Awesome 6 Free Solid" and "Font Awesome 6 Brands Regular".
// Assets/icons.txt (name TAB hex TAB S|B) was generated from the Font Awesome 6.5.1 CSS and validated against the fonts' glyph
// tables, so every listed glyph exists. Use Icons.Make(name, size, brush) / Icons.Glyph(name) / Ui.Icon(...).
// If an icon is blank: the name is not in icons.txt (check spelling; kebab-case or PascalCase both work).
// If ALL icons are blank: WPF could not load the font - check Motion.csproj <Resource> items and the family names above.
// Credits: Font Awesome Free - icons CC BY 4.0, fonts SIL OFL 1.1, code MIT.
//
// ---------------------------------------------------------------------------------------------------------------------
// 7. KNOWN LIMITATIONS
// ---------------------------------------------------------------------------------------------------------------------
// - Nothing has been compiled or run by the AI. Expect compile/runtime fixes on first build.
// - DPI/vendor buttons, extra thumb buttons beyond Button 5, and tilt-wheel as a button are not usable unless Windows reports them.
// - Hooks and SendInput generally cannot act on windows running as administrator (Windows UIPI) unless Motion also runs elevated.
// - A gesture button's click is replayed on release, so click-and-drag on that button does not work. Left is never a trigger.
// - Overlay placement on mixed-DPI multi-monitor setups is untested.
// - Store (UWP) apps can't be added by drag and drop; pick them from the running-apps list.
//
// ---------------------------------------------------------------------------------------------------------------------
// 8. NEXT STEPS (priority order from the owner's final brief)
// ---------------------------------------------------------------------------------------------------------------------
// 1) Get a clean build + owner test of v0.3 (icons, presets, wheels). Fix reported errors, edit only the files involved.
// 2) (after build check of v0.4) Settings overhaul: global defaults with per-profile overrides ("reset to global"), search, per-category reset, autosave.
// 3) Action Builder (sequences, delays, shortcut capture by pressing keys, launch, test/run).
// 4) Universal search / command palette; favorites/recents; action history view (debug logging off by default).
// 5) Cursor effects on the System wheel/tray (Magnifier, Spotlight, Halo, Highlight, Trail, Ripple, Emphasis).
// 6) Interactive mouse map; gesture presets; live gesture test; wheel(s) per profile; custom profile images.
// 7) Battery (if reported), RGB investigation UI (disabled with a note if unsupported).
//
// ---------------------------------------------------------------------------------------------------------------------
// 9. RGB / BATTERY NOTE
// ---------------------------------------------------------------------------------------------------------------------
// RGB lighting has no standard Windows API; it is vendor/model specific. Not implemented. If built, keep it a separate module and
// show it as disabled/greyed-out with a clear note when unsupported. Battery level only if the device reports it.
//
// ---------------------------------------------------------------------------------------------------------------------
// 10. CHANGELOG (newest first - add an entry for every change)
// ---------------------------------------------------------------------------------------------------------------------
// v0.8.0  (UNTESTED, not compiled) Redesign from the HTML prototype: new Theme.xaml tokens (dark/light), prototype sidebar (brand, connected-device card, nav),
//         top bar with engine status + light/dark switch (also Settings > Appearance, AppSettings.DarkMode), "Saved" toast, Overview page rebuilt
//         (Views/HomeView: My devices with the generic mouse illustration Views/MouseIllustration.cs, quick access, device status, RGB Control card
//         that is intentionally DISABLED, getting-started steps). Mouse name auto-detected via HidD_GetProductString (Services/RawDevices + DeviceNames),
//         editable (AppSettings.DeviceNameOverride); the illustration never depends on the device. Radial wheel palettes use lime. New app icon (lime).
//         Nav labels: Overview, Actions Ring (=Radial wheel page), Smart Actions (=Actions), App profiles (=Profiles); Buttons/Gestures/Detect unchanged.
// v0.7.1  Removed the "My mouse" page and the Havit/mouse-model code (it was slow). Radial: shapes (Ring/Pie/Slim/Bubbles/Orbit), hover effects, Spin animation, color themes; hovered slice no longer hides icon/text.
// v0.7.0  (UNTESTED, not compiled) Radial: even-gap geometry (parallel channels), Separated/Connected styles (WheelSettings.Style), Hide on mouse leave
//         (WheelSettings.HideOnLeave, distance-based timer), breadcrumb + "Back to parent" + sibling chips in the wheel editor. Nav accent line removed.
//         Test run starts immediately. App icon Assets/motion.ico (exe, window, tray). "My mouse" page (Views/DeviceView, Models/MouseModels): clickable
//         hotspots, shares ButtonAssign + engine InputDetected with Buttons/Detect; Havit GAMS64WB entry is an UNVERIFIED approximate layout (no spec found).
//         Middle-click autoscroll: engine already swallows mapped buttons (OnButton), nothing extra needed; Left is never assignable.
// v0.6.0  Detect mouse is its own page (Views/DetectView, sidebar item under "Set up your mouse"); removed from Buttons (which links to it). Stops when you leave the page.
// v0.5.2  Size unification: every button, input and dropdown is 42 px tall (Ui.ControlHeight); icon buttons 42x42; dropdowns/pickers 250 wide (Ui.ControlWidth); icon chips 40; same button padding; no per-control paddings/opacity overrides.
// v0.5.1  Wheel limit: max 8 slices per level (editor, load-time migration, draw-time guard).
// v0.5.0  UI/UX pass (UNTESTED): Theme.xaml redesign (42 px buttons with pressed/focus/disabled states, DangerButton, larger inputs/selects/toggle, nav accent bar);
//         sidebar grouped (Set up / Automate / App) with tooltips; header "Saved" pill (SettingsService.Saved event); Home = clickable "Get started"
//         steps; Ui.Intro "how it works" box on Buttons/Gestures/Wheel/Actions; clearer page descriptions. Ui.Btn got optional danger flag.
// v0.4.2  Fix: Icons.cs builds the Solid font from the theme resource (code-built pack URI showed empty boxes in pages).
// v0.4.1  Fix: InvariantGlobalization=false in Motion.csproj (window failed to open: "Cannot find non-neutral culture related to en-us").
// v0.4.0  (NOT COMPILED - first build may need fixes) Shortcuts: ShortcutParser now allows any number of keys + reverse naming; key capture
//         dialog (Views/ShortcutCaptureWindow.cs). Action Builder page + ActionSequence model + ActionRunner.RunSequence; ActionCatalog
//         lists are now dynamic (include saved actions). Gesture presets. ActionHistory (Services/ActionHistory.cs) + Troubleshooting card.
//         AppSettings schema 6 (Sequences, RecordHistory). Files: new ActionSequence.cs, ActionHistory.cs, ShortcutCaptureWindow.cs,
//         ActionsView.xaml(.cs); edited ShortcutParser, InputSender, ActionRunner, ActionCatalog, ActionDef, AppSettings, FeatureSettings,
//         RemapEngine, App.xaml.cs, ActionPicker, GesturesView, SettingsView, MainWindow(.xaml), Motion.csproj.
// v0.3.0  Icons: replaced FontAwesome.Sharp with bundled Font Awesome 6.5.1 fonts + generated catalog; searchable picker over the
//         whole set. System wheel (shared, first slice) + per-profile wheels; drag-and-drop reorder in the wheel editor; profile
//         icons; ready-made profiles (Illustrator, Word, Google Docs, Chrome, Gaming); AppSettings schema 5; this notes file.
// v0.2.0  Gestures, radial wheel, click-toggle horizontal scroll, Detect Mode, Raw Input device list, Font Awesome via NuGet,
//         UI polish (spacing, scrollbars), tray memory trimming.
// v0.1.x  Skeleton, tray, settings, startup; button remapping (hook); profiles with active-app switching, drag-and-drop apps.
