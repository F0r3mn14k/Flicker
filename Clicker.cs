using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

public class Rule {
    public bool IsPause { get; set; }
    public bool PauseByDuration { get; set; }
    public double PauseMinutes { get; set; }
    public string PauseEnd { get; set; }
    public bool Enabled { get; set; }
    public string Key { get; set; }
    public int MouseX { get; set; }
    public int MouseY { get; set; }
    public string Mode { get; set; }
    public double Seconds { get; set; }
    public string Time { get; set; }
    public int MaxDelayMs { get; set; }
    public int HoldMinMs { get; set; }
    public int HoldMaxMs { get; set; }
    [XmlIgnore] public bool Pending;
    [XmlIgnore] public double Next;
    [XmlIgnore] public DateTime LastDay;
    [XmlIgnore] public int Count;
    public Rule() { PauseByDuration=true; PauseMinutes=10; PauseEnd="10:10:00"; Enabled = true; Key = "Space"; Mode = "Co ile sekund"; Seconds = 1; Time = "12:00:00"; HoldMinMs = 50; HoldMaxMs = 100; }
}

static class Keyboard {
    [StructLayout(LayoutKind.Sequential)] public struct KI { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] public struct MI { public int x, y; public uint data, flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Explicit)] public struct Union { [FieldOffset(0)] public KI key; [FieldOffset(0)] public MI mouse; }
    [StructLayout(LayoutKind.Sequential)] public struct Input { public uint type; public Union data; }
    [DllImport("user32.dll", SetLastError=true)] static extern uint SendInput(uint n, Input[] inputs, int size);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code, uint mode);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    public static Input[] Events(string name) {
        Keys key = (Keys)Enum.Parse(typeof(Keys), name);
        uint code = MapVirtualKey((uint)key, 4);
        if (code == 0) throw new InvalidOperationException("Brak kodu klawisza: " + name);
        Input down = new Input(); down.type = 1;
        down.data.key.scan = (ushort)(code & 255);
        bool extended = key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || key == Keys.Insert || key == Keys.Delete || key == Keys.Home || key == Keys.End || key == Keys.PageUp || key == Keys.PageDown;
        down.data.key.flags = 8u | (extended || (code & 0xff00) == 0xe000 ? 1u : 0u);
        Input up = down; up.data.key.flags |= 2;
        return new Input[] { down, up };
    }
    public static void SetKey(string name, bool down) {
        Input input = MouseInput.IsMouse(name) ? MouseInput.Event(name,down) : Events(name)[down ? 0 : 1];
        uint sent = SendInput(1, new Input[] { input }, Marshal.SizeOf(typeof(Input)));
        if (sent != 1) {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException("Windows nie przyjął naciśnięcia (kod " + error + "). Sprawdź uprawnienia aplikacji docelowej.");
        }
    }
}

// Deadlines are monotonic; failures retain ownership so releases can be retried.
class HeldKeys {
    readonly Dictionary<string, double> held = new Dictionary<string, double>();
    readonly List<string> releases = new List<string>();
    readonly Action<string, bool> send;
    public HeldKeys(Action<string, bool> sender) { send = sender; }
    public int Count { get { return held.Count; } }
    public bool MouseHeld { get { return held.ContainsKey(MouseInput.Left) || held.ContainsKey(MouseInput.Right) || held.ContainsKey(MouseInput.Middle); } }
    public bool Begin(string key, double now, int milliseconds) {
        if(held.ContainsKey(key)) return false;
        send(key, true); held.Add(key, now + milliseconds / 1000.0); return true;
    }
    public void Release(double now, bool all) {
        if(held.Count == 0) return;
        Exception failure = null;
        releases.Clear();
        foreach(var item in held) if(all || now >= item.Value) releases.Add(item.Key);
        foreach(string key in releases) {
            try { send(key, false); held.Remove(key); } catch(Exception ex) { failure = ex; }
        }
        if(failure != null) throw failure;
    }
}

class ClickerForm : GradientForm {
    const string Interval = "Co ile sekund", Daily = "O godzinie";
    readonly List<Rule> rules = new List<Rule>();
    readonly DataGridView grid = new DataGridView();
    readonly KeyPicker key = new KeyPicker();
    readonly ComboBox mode = new ComboBox();
    readonly NumericUpDown seconds = new NumericUpDown();
    readonly NumericUpDown delay = new NumericUpDown();
    readonly NumericUpDown holdMin = new NumericUpDown(), holdMax = new NumericUpDown();
    readonly HeldKeys held = new HeldKeys(Keyboard.SetKey);
    readonly Random random = new Random();
    readonly DateTimePicker time = new DateTimePicker();
    readonly Label clock = new Label(), status = new Label();
    readonly ModernButton start = new ModernButton { Primary = true };
    readonly FlowLayoutPanel editor = new FlowLayoutPanel();
    readonly Timer timer = new Timer();
    bool listPaused;
    DateTime listPauseUntil;

    readonly ModernButton pauseButton = new ModernButton();
    readonly Stopwatch monotonic = Stopwatch.StartNew();
    readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeyboardClicker", "rules.xml");
    bool running, hotkeyReady, refreshingRows;
    double nextUiUpdate;
    readonly bool previewOnly;
    double armedAt;
    Point mousePoint; bool mousePointChosen;
    public ClickerForm(bool preview = false) {
        previewOnly = preview;
        Text = "Flicker"; Size = new Size(630, 920); MinimumSize = new Size(630, 880);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(243,248,237);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(20), ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 370)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        Controls.Add(layout);
        var heading = new Panel { Dock=DockStyle.Fill, BackColor=Color.Transparent };
        heading.Controls.Add(new Label { Text="Flicker",Font=new Font("Segoe UI",27,FontStyle.Bold),ForeColor=Theme.Ink,BackColor=Color.Transparent,AutoSize=true });
        pauseButton.Text="+ Wstrzymaj…"; pauseButton.Width=200; pauseButton.Dock=DockStyle.Right;
        pauseButton.Click += delegate { if(running) return; using(var dialog=new ScheduledPauseDialog(null)) { if(dialog.ShowDialog(this)==DialogResult.OK) { rules.Add(dialog.Result); RefreshRows(); Save(); } } };
        heading.Controls.Add(pauseButton); layout.Controls.Add(heading,0,0);
        layout.Controls.Add(new Label { Text = "Twój rytm. Twoje skróty. Naciśnięcia trafiają do aktywnego okna.\nF8 — start / stop   •   F9 — stop   •   3 sekundy na zmianę okna.", ForeColor = Theme.Muted, BackColor = Color.Transparent, AutoSize = true },0,1);
        editor.Dock = DockStyle.Fill; editor.WrapContents = true; editor.BackColor = Color.White; var setupCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0,0,0,16) }; var setupTitle = new Label { Text = "01   USTAW AKCJĘ", Dock = DockStyle.Top, Height = 30, ForeColor = Theme.Accent, Font = new Font("Segoe UI",9,FontStyle.Bold) }; var pointButton=Button("Punkt myszy…",delegate { if(running) return; using(var dialog=new MousePointDialog(mousePoint)) { if(dialog.ShowDialog(this)==DialogResult.OK) { mousePoint=dialog.SelectedPoint; mousePointChosen=true; status.Text="Punkt myszy: " + mousePoint.X + ", " + mousePoint.Y; } } }); pointButton.Dock=DockStyle.Right; pointButton.Width=165; var setupHeading=new Panel { Dock=DockStyle.Top,Height=34 }; setupTitle.Dock=DockStyle.Left; setupTitle.Width=230; setupHeading.Controls.Add(setupTitle); setupHeading.Controls.Add(pointButton); setupCard.Controls.Add(editor); setupCard.Controls.Add(setupHeading); layout.Controls.Add(setupCard,0,2);
        mode.DropDownStyle = ComboBoxStyle.DropDownList; key.Width = 230; mode.Width = 230;
        key.SelectedKey = "Space";
        mode.Items.AddRange(new object[] { Interval, Daily }); mode.SelectedIndex = 0;
        seconds.DecimalPlaces = 2; seconds.Minimum = .05m; seconds.Maximum = 86400; seconds.Value = 1; seconds.Increment = .1m; seconds.Width = 230;
        delay.Maximum = 60000; delay.Width = 230; delay.Increment = 50;
        time.Format = DateTimePickerFormat.Custom; time.CustomFormat = "HH:mm:ss"; time.ShowUpDown = true; time.Width = 230; time.Enabled = false;
        mode.SelectedIndexChanged += delegate { seconds.Enabled = mode.SelectedIndex == 0; time.Enabled = mode.SelectedIndex == 1; };
        editor.Controls.Add(Field("Kliknij i naciśnij klawisz", key)); editor.Controls.Add(Field("Tryb", mode)); editor.Controls.Add(Field("Sekundy", seconds)); editor.Controls.Add(Field("Godzina lokalna", time));
        editor.Controls.Add(Field("Losowe opóźnienie do (ms)", delay));
        holdMin.Minimum = holdMax.Minimum = 1; holdMin.Maximum = holdMax.Maximum = 5000; holdMin.Value = 50; holdMax.Value = 100; holdMin.Width = holdMax.Width = 230;
        holdMin.ValueChanged += delegate { if(holdMax.Value < holdMin.Value) holdMax.Value = holdMin.Value; }; holdMax.ValueChanged += delegate { if(holdMin.Value > holdMax.Value) holdMin.Value = holdMax.Value; };
        editor.Controls.Add(Field("Przytrzymanie od (ms)", holdMin)); editor.Controls.Add(Field("Przytrzymanie do (ms)", holdMax));
        var add = Button("+  Dodaj do listy", delegate { if(MouseInput.IsMouse(key.SelectedKey) && !mousePointChosen) { MessageBox.Show(this,"Najpierw wybierz Punkt myszy…"); return; } rules.Add(new Rule { Key = key.SelectedKey, MouseX=mousePoint.X, MouseY=mousePoint.Y, Mode = mode.Text, Seconds = (double)seconds.Value, Time = time.Value.ToString("HH:mm:ss"), MaxDelayMs = (int)delay.Value, HoldMinMs = (int)holdMin.Value, HoldMaxMs = (int)holdMax.Value }); RefreshRows(); Save(); });
        ((ModernButton)add).Primary = true; add.Width = 230; add.Margin = new Padding(6,22,0,0); editor.Controls.Add(add);
        grid.Dock = DockStyle.Fill; grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Aktywny", FillWeight = 55 });
        foreach (string h in new string[] { "Akcja", "Harmonogram", "Następne", "Licznik" }) grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = h, ReadOnly = true });
        grid.Columns[0].FillWeight = 60; grid.Columns[1].FillWeight = 65; grid.Columns[2].FillWeight = 235; grid.Columns[3].FillWeight = 100; grid.Columns[4].FillWeight = 55; StyleGrid();
        grid.CurrentCellDirtyStateChanged += delegate { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e) { if (!refreshingRows && e.RowIndex >= 0 && e.ColumnIndex == 0 && e.RowIndex < rules.Count) { rules[e.RowIndex].Enabled = Convert.ToBoolean(grid.Rows[e.RowIndex].Cells[0].Value); Save(); } };
        var listCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(16) }; listCard.Controls.Add(grid); listCard.Controls.Add(new Label { Text = "02   TWOJE HARMONOGRAMY", Dock = DockStyle.Top, Height = 32, ForeColor = Theme.Accent, Font = new Font("Segoe UI",9,FontStyle.Bold) }); layout.Controls.Add(listCard,0,3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0,12,0,0) };
        start.Text = "Start   ·   F8"; start.Width = 120; start.Margin = new Padding(0,0,8,0); start.Height = 42; start.Click += delegate { Toggle(); }; actions.Controls.Add(start);
        actions.Controls.Add(Button("Stop   ·   F9", delegate { Stop("Zatrzymano."); }));
        var edit = Button("Edytuj wybrany", delegate { if (running || grid.CurrentRow == null) return; var r = rules[grid.CurrentRow.Index]; if(r.IsPause) { using(var pauseDialog=new ScheduledPauseDialog(r)) { if(pauseDialog.ShowDialog(this)==DialogResult.OK) { rules[grid.CurrentRow.Index]=pauseDialog.Result; RefreshRows(); Save(); } } return; } using (var dialog = new EditRule(r, ActionNames())) { if (dialog.ShowDialog(this) == DialogResult.OK) { RefreshRows(); Save(); } } });
        var remove = Button("Usuń wybrany", delegate { if (!running && grid.CurrentRow != null) { rules.RemoveAt(grid.CurrentRow.Index); RefreshRows(); Save(); } });
        actions.Controls.Add(edit); actions.Controls.Add(remove); layout.Controls.Add(actions,0,4);
        status.Text = "Gotowy. Dodaj pierwszy klawisz."; status.Dock = DockStyle.Fill; status.BackColor = Color.FromArgb(218,237,225); status.ForeColor = Theme.Ink; status.Padding = new Padding(12,0,0,0); status.TextAlign = ContentAlignment.MiddleLeft; layout.Controls.Add(status,0,5);
        clock.Dock = DockStyle.Fill; clock.ForeColor = Theme.Muted; clock.BackColor = Color.Transparent; clock.TextAlign = ContentAlignment.MiddleLeft; clock.Font = new Font("Segoe UI",9); layout.Controls.Add(clock,0,6);
        timer.Interval = 250; timer.Tick += delegate { Tick(); edit.Enabled = remove.Enabled = pauseButton.Enabled = !running; }; if(!preview) timer.Start();
        Theme.Inputs(editor); if(!preview) { LoadRules(); LoadPause(); } else { rules.Add(new Rule { Key = "Space", MaxDelayMs = 250 }); rules.Add(new Rule { Key = "E", Mode = Daily, Time = "20:30:00" }); } RefreshRows();
        FormClosing += delegate(object sender, FormClosingEventArgs e) { Stop("Zamykanie"); Save(); if(held.Count > 0) e.Cancel = true; };
    }
    protected override void Dispose(bool disposing) { if(disposing) timer.Dispose(); base.Dispose(disposing); }
    static Control Field(string title, Control control) { var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Width = control.Width + 10, Height = 66, BackColor = Color.Transparent }; p.Controls.Add(new Label { Text = title, AutoSize = true, Margin = new Padding(3,0,0,8) }); p.Controls.Add(control); return p; }
    void StyleGrid() {
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(234,243,236);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9,FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(234,243,236);
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.Ink;
        grid.ColumnHeadersHeight = 40; grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.DefaultCellStyle.ForeColor = Theme.Ink;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(201,234,218);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(13,83,67);
        grid.DefaultCellStyle.Padding = new Padding(3,5,3,5);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246,249,245);
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.GridColor = Color.FromArgb(225,235,225);
        grid.RowTemplate.Height = 48;
        grid.Columns[1].DefaultCellStyle.WrapMode=DataGridViewTriState.True;
        grid.Columns[2].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        foreach(DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
    }
    static Button Button(string text, EventHandler action) { var b = new ModernButton { Text = text, Width = 130, Height = 42, Margin = new Padding(0,0,8,0), Danger = text.StartsWith("Stop") || text.StartsWith("Usuń") }; b.Click += action; return b; }
    public static List<string> ActionNames() { var names=KeyNames(); names.AddRange(new string[] { MouseInput.Left, MouseInput.Right, MouseInput.Middle }); return names; }
    public static List<string> KeyNames() { var names = new List<string>(); for (char c = 'A'; c <= 'Z'; c++) names.Add(c.ToString()); for (int i=0;i<10;i++) names.Add("D"+i); names.AddRange(new string[] { "Space", "Enter", "Tab", "Escape", "Back", "Delete", "Insert", "Home", "End", "PageUp", "PageDown", "Up", "Down", "Left", "Right", "Oemtilde" }); for(int i=1;i<=12;i++) if(i!=8 && i!=9) names.Add("F"+i); return names; }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if(previewOnly) return; bool a = Keyboard.RegisterHotKey(Handle, 1, 0x4000, (uint)Keys.F8); bool b = Keyboard.RegisterHotKey(Handle, 2, 0x4000, (uint)Keys.F9); hotkeyReady = b; if(!a || !b) status.Text = "Nie udało się zarejestrować skrótów. Zamknij inne aplikacje używające F8/F9 i uruchom ponownie."; }
    protected override void OnHandleDestroyed(EventArgs e) { Keyboard.UnregisterHotKey(Handle,1); Keyboard.UnregisterHotKey(Handle,2); base.OnHandleDestroyed(e); }
    protected override void WndProc(ref Message m) { if(m.Msg == 0x0312) { if(KeyPicker.Listening!=null && !running) { KeyPicker.Listening.Accept(m.WParam.ToInt32()==1 ? Keys.F8 : Keys.F9); return; } if(m.WParam.ToInt32()==1) Toggle(); if(m.WParam.ToInt32()==2) Stop("Zatrzymano przez F9."); } base.WndProc(ref m); }
    void Toggle() {
        if(running) { Stop("Zatrzymano."); return; }
        if(!Enabled || (Form.ActiveForm != null && Form.ActiveForm != this)) return;
        if(held.Count != 0) { status.Text = "Trwa zwalnianie klawiszy — start zablokowany."; return; }
        if(!hotkeyReady) { status.Text = "Start zablokowany: awaryjny skrót F9 jest zajęty."; return; }
        if(!rules.Exists(delegate(Rule r) { return r.Enabled; })) { status.Text = "Dodaj i włącz przynajmniej jeden wpis."; return; }
        armedAt = monotonic.Elapsed.TotalSeconds + 3;
        foreach(var r in rules) { r.Next = armedAt + r.Seconds + RandomDelay(r, random); r.Pending = false; r.Count = 0; }
        listPaused=false; running = true; timer.Interval = 10; editor.Enabled = false; grid.ReadOnly = true; start.Text = "Stop   ·   F8";
    }
    void Stop(string message) { running = false; listPaused=false; try { held.Release(0, true); } catch(Exception ex) { message = "Nie udało się zwolnić klawisza: " + ex.Message; } timer.Interval = held.Count == 0 ? 250 : 10; foreach(var r in rules) r.Pending = false; editor.Enabled = true; grid.ReadOnly = false; start.Text = "Start   ·   F8"; status.Text = message; UpdateRows(DateTime.Now); }
    public static int RandomHold(Rule r, Random source) { return source.Next(r.HoldMinMs, r.HoldMaxMs + 1); }
    public static double RandomDelay(Rule r, Random source) { return source.Next(r.MaxDelayMs + 1) / 1000.0; }
    public static bool DueDaily(Rule r, DateTime now) { DateTime due = now.Date + TimeSpan.Parse(r.Time); return r.LastDay != now.Date && now >= due && (now-due).TotalSeconds < 2; }
    void LoadPause() {
        string oldPath=Path.Combine(Path.GetDirectoryName(path),"pause.xml");
        if(!File.Exists(oldPath) || File.Exists(oldPath+".migrated")) return;
        try {
            using(var stream=File.OpenRead(oldPath)) {
                var old=(PauseSettings)new XmlSerializer(typeof(PauseSettings)).Deserialize(stream); old.Validate();
                if(old.Enabled && !rules.Exists(delegate(Rule r) { return r.IsPause && r.Time==old.Start && r.PauseEnd==old.End; }))
                    rules.Add(new Rule { IsPause=true,Mode=Daily,Time=old.Start,PauseByDuration=false,PauseEnd=old.End });
            }
            if(Save()) File.WriteAllText(oldPath+".migrated","Przeniesiono do rules.xml; oryginał pozostawiono jako kopię.");
        } catch(Exception ex) { status.Text="Nie udało się przenieść starej przerwy: "+ex.Message; }
    }
    bool HandlePause(DateTime now, double elapsed, bool ownWindow) {
        try {
            DateTime? until=ScheduledPause.CombinedEnd(rules,now);
            if(until.HasValue) {
                if(!listPaused) { held.Release(elapsed,true); foreach(var r in rules) r.Pending=false; }
                listPaused=true; listPauseUntil=until.Value;
                status.Text="Wstrzymano do " + listPauseUntil.ToString("dd.MM HH:mm:ss") + ". F9 zatrzymuje.";
                return true;
            }
            if(listPaused) {
                listPaused=false;
                foreach(var r in rules) {
                    if(r.IsPause) continue;
                    r.Pending=false; r.Next=elapsed+r.Seconds+RandomDelay(r,random);
                    if(r.Mode==Daily && now.Date+TimeSpan.Parse(r.Time)<=now) r.LastDay=now.Date;
                }
                status.Text="Wznowiono — interwały liczone od nowa.";
                return true;
            }
            return false;
        } catch(Exception ex) { Stop("Wstrzymanie: "+ex.Message); return true; }
    }
    void Tick() {
        DateTime now = DateTime.Now; double elapsed = monotonic.Elapsed.TotalSeconds;
        try { held.Release(elapsed, !running); } catch(Exception ex) { Stop(ex.Message); }

        if(running && elapsed < armedAt) status.Text = "Start za " + Math.Ceiling(armedAt-elapsed) + " s — wybierz okno docelowe.";
        else if(running) {
            bool ownWindow = Form.ActiveForm != null || Keyboard.GetForegroundWindow() == Handle;
            if(!HandlePause(now, elapsed, ownWindow)) {
            status.Text = ownWindow ? "Wstrzymano wysyłanie: wybierz inne okno. F9 zatrzymuje." : "Działa — F9 zatrzymuje. Godziny są wykonywane codziennie.";
            foreach(var r in rules) {
                if(!r.Enabled || r.IsPause) continue;
                if(r.Mode == Daily && !r.Pending && DueDaily(r, now)) {
                    r.LastDay = now.Date;
                    r.Pending = true;
                    r.Next = elapsed + RandomDelay(r, random);
                }
                bool due = (r.Mode == Interval || r.Pending) && elapsed >= r.Next;
                if(!due) continue;
                if(r.Mode == Interval) r.Next = elapsed + r.Seconds + RandomDelay(r, random);
                else r.Pending = false;
                if(ownWindow) continue;
                try { if(MouseInput.IsMouse(r.Key)) { if(held.MouseHeld) continue; MouseInput.Move(new Point(r.MouseX,r.MouseY)); } if(held.Begin(r.Key, monotonic.Elapsed.TotalSeconds, RandomHold(r, random))) r.Count++; }
                catch(Exception ex) { Stop(ex.Message); break; }
            }
        }
        }
        if(elapsed >= nextUiUpdate) { nextUiUpdate = elapsed + .1; clock.Text = "Zegar komputera: " + now.ToString("yyyy-MM-dd  HH:mm:ss") + "   •   " + TimeZoneInfo.Local.StandardName; UpdateRows(now); }
    }
    void RefreshRows() {
        refreshingRows=true;
        try {
            grid.Rows.Clear();
            foreach(var r in rules) {
                string action=r.IsPause?"Wstrzymaj":KeyPicker.DisplayName(r.Key)+(MouseInput.IsMouse(r.Key)?" ("+r.MouseX+", "+r.MouseY+")":"");
                string schedule=r.IsPause?"Od "+r.Time+(r.PauseByDuration?" na "+r.PauseMinutes+" min":" do "+r.PauseEnd):(r.Mode==Interval?"Co "+r.Seconds+" s":"Codziennie "+r.Time)+" + 0–"+r.MaxDelayMs+" ms; trzymaj "+r.HoldMinMs+"–"+r.HoldMaxMs+" ms";
                grid.Rows.Add(r.Enabled,action,schedule,"—",r.IsPause?(object)"—":r.Count);
            }
        } finally { refreshingRows=false; }
    }
    void UpdateRows(DateTime now) {
        for(int i=0;i<rules.Count;i++) {
            Rule r=rules[i]; string next="—";
            if(running && r.Enabled) {
                var pauseEnd=r.IsPause?ScheduledPause.EndAt(r,now):null;
                if(pauseEnd.HasValue) next="Do "+pauseEnd.Value.ToString("HH:mm:ss");
                else if(listPaused && !r.IsPause) next="Wstrzymano";
                else if(!r.IsPause && (r.Mode==Interval || r.Pending)) next="Za "+Math.Max(0,r.Next-monotonic.Elapsed.TotalSeconds).ToString("0.0")+" s";
                else { DateTime d=now.Date+TimeSpan.Parse(r.Time); if(d<now || (!r.IsPause && r.LastDay==now.Date)) d=d.AddDays(1); next=d.ToString("dd.MM HH:mm:ss"); }
            }
            if(!Equals(grid.Rows[i].Cells[3].Value,next)) grid.Rows[i].Cells[3].Value=next;
            object count=r.IsPause?(object)"—":r.Count;
            if(!Equals(grid.Rows[i].Cells[4].Value,count)) grid.Rows[i].Cells[4].Value=count;
        }
    }
    bool Save() {
        if(previewOnly) return false;
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); string temp=path+".tmp";
            using(var stream=File.Create(temp)) new XmlSerializer(typeof(List<Rule>)).Serialize(stream,rules);
            if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
            return true;
        } catch(Exception ex) { status.Text="Nie udało się zapisać ustawień: "+ex.Message; return false; }
    }
    void LoadRules() { if(!File.Exists(path)) return; try { using(var stream=File.OpenRead(path)) { var loaded=(List<Rule>)new XmlSerializer(typeof(List<Rule>)).Deserialize(stream); foreach(var r in loaded) { if(r.IsPause) { ScheduledPause.Validate(r); continue; } TimeSpan t; if(!ActionNames().Contains(r.Key) || r.MouseX < -100000 || r.MouseX > 100000 || r.MouseY < -100000 || r.MouseY > 100000 || (r.Mode!=Interval && r.Mode!=Daily) || double.IsNaN(r.Seconds) || r.Seconds<.05 || r.Seconds>86400 || r.MaxDelayMs<0 || r.MaxDelayMs>60000 || r.HoldMinMs<1 || r.HoldMaxMs>5000 || r.HoldMinMs>r.HoldMaxMs || !TimeSpan.TryParse(r.Time,out t) || t<TimeSpan.Zero || t>=TimeSpan.FromDays(1)) throw new InvalidDataException("Niepoprawny wpis."); } rules.AddRange(loaded); } } catch(Exception ex) { status.Text="Nie udało się odczytać ustawień: "+ex.Message; } }
}

class EditRule : GradientForm {
    public EditRule(Rule rule, List<string> names) {
        Text="Edytuj wpis"; ClientSize=new Size(440,450); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent; Font=new Font("Segoe UI",10);
        var key=new KeyPicker { Left=170,Top=16,Width=240,SelectedKey=rule.Key };
        var mode=new ComboBox { Left=170,Top=60,Width=170,DropDownStyle=ComboBoxStyle.DropDownList }; mode.Items.AddRange(new object[]{"Co ile sekund","O godzinie"}); mode.SelectedItem=rule.Mode;
        var seconds=new NumericUpDown { Left=170,Top=100,Width=170,DecimalPlaces=2,Minimum=.05m,Maximum=86400,Value=(decimal)rule.Seconds };
        var time=new DateTimePicker { Left=170,Top=140,Width=170,Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm:ss",ShowUpDown=true,Value=DateTime.Today+TimeSpan.Parse(rule.Time) };
        EventHandler update=delegate { seconds.Enabled=mode.SelectedIndex==0; time.Enabled=mode.SelectedIndex==1; }; mode.SelectedIndexChanged+=update; update(null,EventArgs.Empty);
        var delay=new NumericUpDown { Left=240,Top=180,Width=170,Maximum=60000,Value=rule.MaxDelayMs,Increment=50 }; Controls.Add(delay); Controls.Add(new Label { Text="Losowe opóźnienie do (ms)",Left=20,Top=184,AutoSize=true });
        var holdMin=new NumericUpDown { Left=240,Top=220,Width=170,Minimum=1,Maximum=5000,Value=rule.HoldMinMs }; var holdMax=new NumericUpDown { Left=240,Top=260,Width=170,Minimum=1,Maximum=5000,Value=rule.HoldMaxMs };
        holdMin.ValueChanged += delegate { if(holdMax.Value < holdMin.Value) holdMax.Value=holdMin.Value; }; holdMax.ValueChanged += delegate { if(holdMin.Value > holdMax.Value) holdMin.Value=holdMax.Value; }; Controls.AddRange(new Control[]{holdMin,holdMax}); Controls.Add(new Label { Text="Przytrzymanie od (ms)",Left=20,Top=224,AutoSize=true }); Controls.Add(new Label { Text="Przytrzymanie do (ms)",Left=20,Top=264,AutoSize=true });
        string[] labels={"Klawisz","Tryb","Sekundy","Godzina lokalna"}; for(int i=0;i<4;i++) Controls.Add(new Label { Text=labels[i],Left=20,Top=24+40*i,AutoSize=true });
        Controls.AddRange(new Control[]{key,mode,seconds,time});
        Point point=new Point(rule.MouseX,rule.MouseY); bool pointConfigured=MouseInput.IsMouse(rule.Key);
        var pointButton=new ModernButton { Text="Punkt myszy…",Left=240,Top=305,Width=170,Enabled=MouseInput.IsMouse(key.SelectedKey) }; pointButton.Click+=delegate { using(var dialog=new MousePointDialog(point)) { if(dialog.ShowDialog(this)==DialogResult.OK) { point=dialog.SelectedPoint; pointConfigured=true; pointButton.Text="X: "+point.X+"  Y: "+point.Y; } } }; Controls.Add(pointButton); key.SelectedKeyChanged+=delegate { pointButton.Enabled=MouseInput.IsMouse(key.SelectedKey); };
        var ok=new ModernButton { Primary=true, Text="Zapisz",Left=150,Top=395,Width=90 }; ok.Click+=delegate { if(MouseInput.IsMouse(key.SelectedKey) && !pointConfigured) { MessageBox.Show(this,"Najpierw wybierz Punkt myszy…"); return; } rule.MouseX=point.X; rule.MouseY=point.Y; rule.Key=key.SelectedKey; rule.Mode=mode.Text; rule.Seconds=(double)seconds.Value; rule.Time=time.Value.ToString("HH:mm:ss"); rule.MaxDelayMs=(int)delay.Value; rule.HoldMinMs=(int)holdMin.Value; rule.HoldMaxMs=(int)holdMax.Value; DialogResult=DialogResult.OK; }; Controls.Add(ok);
        var cancel=new ModernButton { Text="Anuluj",Left=250,Top=395,Width=90,DialogResult=DialogResult.Cancel }; Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel; Theme.Inputs(this);
    }
}

static class Program {
    [STAThread] static int Main(string[] args) {
        MouseInput.EnableDpiAwareness();
        if(args.Length>0 && args[0]=="--self-test") {
            try {
                PauseTests.Run();
                ScheduleTests.Run();
                if(Marshal.SizeOf(typeof(Keyboard.Input)) != (IntPtr.Size==8?40:28)) throw new Exception("INPUT size");
                foreach(string key in ClickerForm.KeyNames()) { var e=Keyboard.Events(key); if(e[0].data.key.scan==0 || (e[1].data.key.flags&2)==0) throw new Exception("Key "+key); }
                if((Keyboard.Events("Left")[0].data.key.flags&1)==0) throw new Exception("Extended key");
                string[] mouseButtons={MouseInput.Left,MouseInput.Right,MouseInput.Middle};
                uint[] mouseFlags={2,8,32};
                for(int i=0;i<mouseButtons.Length;i++) {
                    var down=MouseInput.Event(mouseButtons[i],true); var up=MouseInput.Event(mouseButtons[i],false);
                    if(down.type!=0 || up.type!=0 || down.data.mouse.flags!=mouseFlags[i] || up.data.mouse.flags!=mouseFlags[i]*2) throw new Exception("Mouse flags");
                }
                var mouseEvents=new List<bool>();
                var mouseHeld=new HeldKeys(delegate(string button,bool down) { mouseEvents.Add(down); });
                mouseHeld.Begin(MouseInput.Left,0,100);
                if(!mouseHeld.MouseHeld || mouseHeld.Begin(MouseInput.Left,0,100)) throw new Exception("Mouse ownership");
                mouseHeld.Release(0,true);
                if(mouseHeld.MouseHeld || mouseEvents.Count!=2 || mouseEvents[1]) throw new Exception("Mouse stop release");
                var mouseRule=new Rule { Key=MouseInput.Right,MouseX=-500,MouseY=250 };
                using(var memory=new MemoryStream()) { var xml=new XmlSerializer(typeof(Rule)); xml.Serialize(memory,mouseRule); memory.Position=0; var copy=(Rule)xml.Deserialize(memory); if(copy.Key!=MouseInput.Right || copy.MouseX!=-500 || copy.MouseY!=250) throw new Exception("Mouse persistence"); }
                Console.WriteLine("PASS: mouse buttons, held-button ownership, emergency release and point persistence.");
                Rule r=new Rule { Time="12:30:00" }; DateTime d=new DateTime(2026,9,27,12,30,0);
                if(ClickerForm.DueDaily(r,d.AddSeconds(-1)) || !ClickerForm.DueDaily(r,d) || ClickerForm.DueDaily(r,d.AddSeconds(3))) throw new Exception("Schedule window");
                r.LastDay=d.Date; if(ClickerForm.DueDaily(r,d) || !ClickerForm.DueDaily(r,d.AddDays(1))) throw new Exception("Daily deduplication");
                var random = new Random(42);
                if(ClickerForm.RandomDelay(r,random)!=0) throw new Exception("Zero delay");
                r.MaxDelayMs=500; var values=new HashSet<double>();
                for(int i=0;i<10000;i++) { double value=ClickerForm.RandomDelay(r,random); if(value<0 || value>.5) throw new Exception("Delay bounds"); values.Add(value); }
                if(values.Count<2 || !values.Contains(0) || !values.Contains(.5)) throw new Exception("Delay variability and inclusive endpoints");
                var holds = new HashSet<int>();
                for(int i=0;i<10000;i++) { int value=ClickerForm.RandomHold(r, random); if(value<50 || value>100) throw new Exception("Hold bounds"); holds.Add(value); }
                if(!holds.Contains(50) || !holds.Contains(100)) throw new Exception("Hold endpoints");
                r.HoldMinMs = r.HoldMaxMs = 75;
                if(ClickerForm.RandomHold(r,random)!=75) throw new Exception("Fixed hold");
                var events = new List<string>(); bool failRelease = false;
                var held = new HeldKeys(delegate(string name, bool down) { if(!down && failRelease) throw new Exception("Simulated release failure"); events.Add(name + (down ? ":down" : ":up")); });
                if(!held.Begin("A",0,100) || held.Begin("A",0,50)) throw new Exception("Overlapping key");
                held.Release(.099,false); if(events.Count!=1) throw new Exception("Released early");
                held.Release(.1,false); if(events.Count!=2 || events[1]!="A:up" || held.Count!=0) throw new Exception("Deadline release");
                held.Begin("A",1,5000); held.Begin("B",1,5000); held.Release(1,true);
                if(held.Count!=0 || events.Count!=6) throw new Exception("Stop releases all");
                held.Begin("C",2,50); failRelease=true;
                try { held.Release(3,true); } catch(Exception) { }
                if(held.Count!=1) throw new Exception("Failed release lost");
                failRelease=false; held.Release(3,true); if(held.Count!=0) throw new Exception("Release retry");
                using(var old=new StringReader("<Rule><Key>A</Key></Rule>")) { var legacy=(Rule)new XmlSerializer(typeof(Rule)).Deserialize(old); if(legacy.HoldMinMs!=50 || legacy.HoldMaxMs!=100) throw new Exception("Legacy defaults"); }
                using(var memory=new MemoryStream()) { var xml=new XmlSerializer(typeof(List<Rule>)); xml.Serialize(memory,new List<Rule>{r}); memory.Position=0; var restored=(List<Rule>)xml.Deserialize(memory); if(restored[0].Time!=r.Time || restored[0].LastDay!=DateTime.MinValue || restored[0].MaxDelayMs!=500 || restored[0].HoldMinMs!=75 || restored[0].HoldMaxMs!=75 || restored[0].Pending) throw new Exception("Persistence"); }
                Console.WriteLine("PASS: random hold bounds, fixed duration, release deadlines, overlap suppression, stop, release retry, legacy settings.");
                Console.WriteLine("PASS: random delay bounds, zero, variability, endpoints and persistence.");
                Console.WriteLine("PASS: native layout, all key mappings, extended keys, daily schedule, duplicate suppression, XML persistence."); return 0;
            } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new ClickerForm()); return 0;
    }
}
