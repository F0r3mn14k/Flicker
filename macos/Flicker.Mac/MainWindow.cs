using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Flicker.Mac;

public sealed class MainWindow : Window
{
    private readonly ObservableCollection<Rule> _rules = [];
    private readonly ListBox _list = new();
    private readonly ComboBox _profiles = new();
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _start = new() { Content = "Start" }, _pause = new() { Content = "Pauza", IsEnabled = false }, _stop = new() { Content = "Stop", IsEnabled = false };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private ProfileLibrary _library = ProfileStorage.CreateDefault();
    private bool _running, _manualPause, _busy, _updatingProfiles;
    private double _manualPauseAt;

    private static readonly string LibraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Flicker", "profiles.xml");

    public MainWindow()
    {
        Title = "Flicker dla macOS";
        Width = 780; Height = 650; MinWidth = 640; MinHeight = 520;
        Background = new SolidColorBrush(Color.Parse("#11171E"));

        var root = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto") };
        root.Children.Add(Header());
        root.Children.Add(ProfileBar().At(1));
        root.Children.Add(ActionBar().At(2));

        _list.ItemsSource = _rules;
        _list.Background = new SolidColorBrush(Color.Parse("#1A232D"));
        _list.Foreground = Brushes.White;
        root.Children.Add(_list.At(3));
        root.Children.Add(Transport().At(4));
        _status.Foreground = new SolidColorBrush(Color.Parse("#A4B6C2"));
        _status.Text = "Dodaj wpis i uruchom harmonogram. Przed pierwszym użyciem nadaj Flickerowi dostęp Dostępność w macOS.";
        root.Children.Add(_status.At(5));
        Content = root;

        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        LoadLibrary();
        Closed += (_, _) => { SaveLibrary(); _timer.Stop(); };
    }

    private Control Header() => new StackPanel
    {
        Spacing = 2,
        Children =
        {
            new TextBlock { Text = "Flicker", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
            new TextBlock { Text = "macOS · UNIVERSAL BETA", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#45E0C7")) }
        }
    };

    private Control ProfileBar()
    {
        _profiles.Width = 300; _profiles.SelectionChanged += (_, _) => SelectProfile(_profiles.SelectedItem as FlickerProfile);
        return new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Children = { _profiles, Button("Nowy profil", (_, _) => NewProfile()), Button("Eksport", async (_, _) => await ExportProfile()), Button("Import", async (_, _) => await ImportProfile()) }
        };
    }

    private Control ActionBar() => new StackPanel
    {
        Orientation = Orientation.Horizontal, Spacing = 8,
        Children =
        {
            Button("+ Klawisz", async (_, _) => await AddRule(false, false)),
            Button("+ Mysz", async (_, _) => await AddRule(false, true)),
            Button("+ Wstrzymaj", async (_, _) => await AddRule(true, false)),
            Button("Edytuj", async (_, _) => await EditSelected()),
            Button("↑", (_, _) => Move(-1)), Button("↓", (_, _) => Move(1)),
            Button("Usuń", (_, _) => DeleteSelected())
        }
    };

    private Control Transport()
    {
        _start.Classes.Add("primary");
        _start.Click += (_, _) => Start(); _pause.Click += (_, _) => TogglePause(); _stop.Click += (_, _) => Stop("Zatrzymano.");
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { _start, _pause, _stop } };
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> handler) { var button = new Button { Content = text, Padding = new Thickness(14, 8) }; button.Click += handler; return button; }

    private void LoadLibrary()
    {
        try { if (File.Exists(LibraryPath)) _library = ProfileStorage.ReadLibrary(LibraryPath); }
        catch (Exception ex) { _status.Text = "Nie udało się odczytać profili: " + ex.Message; _library = ProfileStorage.CreateDefault(); }
        if (_library.Active is null) _library = ProfileStorage.CreateDefault();
        RefreshProfiles();
    }

    private void SaveLibrary()
    {
        try { SyncRulesToActive(); ProfileStorage.WriteLibrary(LibraryPath, _library); }
        catch (Exception ex) { _status.Text = "Nie udało się zapisać profili: " + ex.Message; }
    }

    private void RefreshProfiles()
    {
        _updatingProfiles = true;
        _profiles.ItemsSource = _library.Ordered().ToList();
        _profiles.SelectedItem = _library.Active;
        _updatingProfiles = false;
        LoadRulesFromActive();
    }

    private void SyncRulesToActive()
    {
        if (_library.Active is null) return;
        _library.Active.Rules = _rules.Select(Clone).ToList();
    }

    private void LoadRulesFromActive()
    {
        _rules.Clear();
        foreach (var rule in _library.Active?.Rules ?? []) _rules.Add(Clone(rule));
    }

    private static Rule Clone(Rule rule) => new()
    {
        IsPause = rule.IsPause, Enabled = rule.Enabled, AllowDuringPause = rule.AllowDuringPause, Key = rule.Key, Mode = rule.Mode,
        Seconds = rule.Seconds, Time = rule.Time, PauseMinutes = rule.PauseMinutes, HoldMs = rule.HoldMs, MouseX = rule.MouseX, MouseY = rule.MouseY
    };

    private void SelectProfile(FlickerProfile? next)
    {
        if (_updatingProfiles || next is null || next.Id == _library.ActiveId) return;
        if (_running) { _status.Text = "Zatrzymaj harmonogram przed zmianą profilu."; RefreshProfiles(); return; }
        SyncRulesToActive(); _library.ActiveId = next.Id; SaveLibrary(); LoadRulesFromActive(); _status.Text = "Wybrano profil: " + next.Name;
    }

    private void NewProfile()
    {
        if (_running) return;
        var profile = new FlickerProfile { Name = "Nowy profil" };
        var suffix = 2; while (_library.Profiles.Any(p => p.Name.Equals(profile.Name, StringComparison.CurrentCultureIgnoreCase))) profile.Name = "Nowy profil " + suffix++;
        SyncRulesToActive(); _library.Profiles.Add(profile); _library.ActiveId = profile.Id; SaveLibrary(); RefreshProfiles();
    }

    private async Task AddRule(bool pause, bool mouse)
    {
        if (_running) return;
        var rule = new Rule { IsPause = pause, Key = mouse ? "Mysz: lewy" : "Space", Time = "09:00:00" };
        if (await RuleEditor.Edit(this, rule)) { _rules.Add(rule); SaveLibrary(); }
    }

    private async Task EditSelected()
    {
        if (_running || _list.SelectedItem is not Rule rule) return;
        if (await RuleEditor.Edit(this, rule)) SaveLibrary();
        _list.ItemsSource = null; _list.ItemsSource = _rules;
    }

    private void DeleteSelected() { if (!_running && _list.SelectedItem is Rule rule) { _rules.Remove(rule); SaveLibrary(); } }
    private void Move(int direction)
    {
        if (_running || _list.SelectedItem is not Rule rule) return;
        var from = _rules.IndexOf(rule); var to = from + direction;
        if (to < 0 || to >= _rules.Count) return;
        _rules.Move(from, to); _list.SelectedItem = rule; SaveLibrary();
    }

    private void Start()
    {
        if (_rules.Count == 0) { _status.Text = "Dodaj co najmniej jeden wpis."; return; }
        if (!MacInput.HasAccessibilityPermission()) { _status.Text = "Nadaj Flickerowi uprawnienie Dostępność w Ustawienia systemowe → Prywatność i ochrona → Dostępność."; return; }
        var now = _clock.Elapsed.TotalSeconds;
        foreach (var rule in _rules) { rule.Next = now + Math.Max(.05, rule.Seconds); rule.Count = 0; rule.LastDay = default; }
        _running = true; _manualPause = false; _start.IsEnabled = false; _pause.IsEnabled = _stop.IsEnabled = true;
        _status.Text = "Działa. Pauza zatrzymuje także wpisy master.";
    }

    private void TogglePause()
    {
        if (!_running) return;
        var now = _clock.Elapsed.TotalSeconds;
        if (!_manualPause) { _manualPause = true; _manualPauseAt = now; _pause.Content = "Wznów"; _status.Text = "Pauza ręczna."; }
        else { var duration = now - _manualPauseAt; foreach (var rule in _rules.Where(r => !r.IsPause && r.Mode == "Co ile sekund")) rule.Next += duration; _manualPause = false; _pause.Content = "Pauza"; _status.Text = "Wznowiono odliczanie."; }
    }

    private void Stop(string message) { _running = _manualPause = false; _start.IsEnabled = true; _pause.IsEnabled = _stop.IsEnabled = false; _pause.Content = "Pauza"; _status.Text = message; }

    private async void Tick()
    {
        if (!_running || _manualPause || _busy) return;
        var now = _clock.Elapsed.TotalSeconds; var date = DateTime.Now;
        var paused = IsScheduledPause(date);
        foreach (var rule in _rules)
        {
            if (!rule.Enabled || rule.IsPause || (paused && !rule.AllowDuringPause)) continue;
            var due = rule.Mode == "O godzinie" ? IsDailyDue(rule, date) : now >= rule.Next;
            if (!due) continue;
            if (rule.Mode == "Co ile sekund") rule.Next = now + Math.Max(.05, rule.Seconds);
            try { _busy = true; await Task.Run(() => MacInput.Press(rule)); rule.Count++; _status.Text = $"Wykonano: {rule}"; }
            catch (Exception ex) { Stop(ex.Message); }
            finally { _busy = false; }
        }
    }

    private bool IsScheduledPause(DateTime now)
    {
        foreach (var rule in _rules.Where(r => r.Enabled && r.IsPause))
        {
            if (!TimeSpan.TryParse(rule.Time, out var startTime)) continue;
            var start = now.Date + startTime; if (start > now) start = start.AddDays(-1);
            if (now >= start && now < start.AddMinutes(rule.PauseMinutes)) return true;
        }
        return false;
    }

    private static bool IsDailyDue(Rule rule, DateTime now)
    {
        if (!TimeSpan.TryParse(rule.Time, out var time) || rule.LastDay == DateOnly.FromDateTime(now)) return false;
        var due = now.Date + time;
        if (now < due || (now - due).TotalSeconds > 2) return false;
        rule.LastDay = DateOnly.FromDateTime(now); return true;
    }

    private async Task ExportProfile()
    {
        if (_library.Active is null) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Eksportuj profil Flickera", SuggestedFileName = _library.Active.Name + ".flicker.xml", DefaultExtension = "flicker.xml" });
        if (file is null) return;
        SyncRulesToActive(); ProfileStorage.WriteProfile(file.Path.LocalPath, _library.Active); _status.Text = "Wyeksportowano profil.";
    }

    private async Task ImportProfile()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Importuj profil Flickera", AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("Profil Flickera") { Patterns = ["*.flicker.xml", "*.xml"] }] });
        if (files.Count == 0) return;
        try { var profile = ProfileStorage.ReadProfile(files[0].Path.LocalPath); profile.Id = Guid.NewGuid().ToString("N"); _library.Profiles.Add(profile); _library.ActiveId = profile.Id; SaveLibrary(); RefreshProfiles(); _status.Text = "Zaimportowano profil."; }
        catch (Exception ex) { _status.Text = "Nie udało się importować profilu: " + ex.Message; }
    }
}

internal static class ControlExtensions
{
    public static T At<T>(this T control, int row) where T : Control { Grid.SetRow(control, row); return control; }
}

internal sealed class RuleEditor : Window
{
    private readonly Rule _rule;
    private readonly ComboBox _key = new(), _mode = new();
    private readonly NumericUpDown _seconds = new() { Minimum = .05m, Maximum = 86400, Increment = .05m }, _hold = new() { Minimum = 1, Maximum = 5000 }, _pauseMinutes = new() { Minimum = .01m, Maximum = 1440, Increment = .25m };
    private readonly NumericUpDown _mouseX = new() { Minimum = -100000, Maximum = 100000 }, _mouseY = new() { Minimum = -100000, Maximum = 100000 };
    private readonly CheckBox _master = new() { Content = "Master: działa podczas wstrzymania" };

    private RuleEditor(Rule rule)
    {
        _rule = rule; Title = rule.IsPause ? "Wstrzymanie" : "Edytuj wpis"; Width = 460; Height = rule.IsPause ? 330 : 600; CanResize = false;
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
        _key.ItemsSource = new[] { "Space", "Enter", "Tab", "Escape", "A", "B", "C", "D", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0", "-", "=", ",", ".", "/", "[", "]", ";", "'", "\\", "`", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "Mysz: lewy", "Mysz: prawy", "Mysz: środkowy" };
        _key.SelectedItem = rule.Key; _mode.ItemsSource = new[] { "Co ile sekund", "O godzinie" }; _mode.SelectedItem = rule.Mode;
        _seconds.Value = (decimal)rule.Seconds; _hold.Value = rule.HoldMs; _pauseMinutes.Value = (decimal)rule.PauseMinutes; _mouseX.Value = rule.MouseX; _mouseY.Value = rule.MouseY; _master.IsChecked = rule.AllowDuringPause;
        if (rule.IsPause) { panel.Children.Add(Label("Codziennie od (HH:mm:ss)")); var time = new TextBox { Text = rule.Time }; panel.Children.Add(time); panel.Children.Add(Label("Na ile minut")); panel.Children.Add(_pauseMinutes); panel.Children.Add(Buttons(() => { _rule.Time = time.Text ?? ""; _rule.PauseMinutes = (double)(_pauseMinutes.Value ?? 10); Close(true); })); }
        else
        {
            var time = new TextBox { Text = rule.Time };
            panel.Children.Add(Label("Klawisz lub mysz")); panel.Children.Add(_key);
            panel.Children.Add(Label("Tryb")); panel.Children.Add(_mode);
            panel.Children.Add(Label("Interwał (sekundy)")); panel.Children.Add(_seconds);
            panel.Children.Add(Label("Godzina lokalna (HH:mm:ss)")); panel.Children.Add(time);
            panel.Children.Add(Label("Przytrzymanie (ms)")); panel.Children.Add(_hold);
            panel.Children.Add(Label("Punkt myszy X / Y (tylko dla myszy)"));
            panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { _mouseX, _mouseY } });
            panel.Children.Add(_master);
            panel.Children.Add(Buttons(() =>
            {
                _rule.Key = _key.SelectedItem?.ToString() ?? "Space"; _rule.Mode = _mode.SelectedItem?.ToString() ?? "Co ile sekund";
                _rule.Seconds = (double)(_seconds.Value ?? 1); _rule.Time = time.Text ?? ""; _rule.HoldMs = (int)(_hold.Value ?? 80);
                _rule.MouseX = (int)(_mouseX.Value ?? 0); _rule.MouseY = (int)(_mouseY.Value ?? 0);
                _rule.AllowDuringPause = _master.IsChecked == true; Close(true);
            }));
        }
        Content = panel;
    }

    private static TextBlock Label(string text) => new() { Text = text, Foreground = Brushes.White };
    private Control Buttons(Action save) { var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; var ok = new Button { Content = "Zapisz" }; ok.Click += (_, _) => save(); var cancel = new Button { Content = "Anuluj" }; cancel.Click += (_, _) => Close(false); row.Children.Add(ok); row.Children.Add(cancel); return row; }
    public static async Task<bool> Edit(Window parent, Rule rule) => await new RuleEditor(rule).ShowDialog<bool>(parent);
}
