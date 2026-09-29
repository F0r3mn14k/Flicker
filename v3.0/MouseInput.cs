using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class MouseInput {
    public const string Left = "Mysz: lewy", Right = "Mysz: prawy", Middle = "Mysz: środkowy";
    [DllImport("user32.dll", SetLastError=true)] static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    public static void EnableDpiAwareness() {
        try { if(SetProcessDpiAwarenessContext(new IntPtr(-4))) return; } catch(EntryPointNotFoundException) { }
        SetProcessDPIAware();
    }
    public static bool IsMouse(string key) { return key==Left || key==Right || key==Middle; }
    public static Keyboard.Input Event(string key,bool down) {
        var input=new Keyboard.Input();
        if(key==Left) input.data.mouse.flags=down?2u:4u;
        else if(key==Right) input.data.mouse.flags=down?8u:16u;
        else if(key==Middle) input.data.mouse.flags=down?32u:64u;
        else throw new ArgumentException("Nieznany przycisk myszy.");
        return input;
    }
    public static Point Position() { Point point; if(!GetCursorPos(out point)) throw new InvalidOperationException("Nie można odczytać położenia kursora."); return point; }
    public static void ValidatePoint(Point point) {
        foreach(var screen in Screen.AllScreens) if(screen.Bounds.Contains(point)) return;
        throw new ArgumentException("Punkt znajduje się poza podłączonymi ekranami. Wybierz go ponownie.");
    }
    public static bool IsOwnWindow(Point point) {
        uint process; GetWindowThreadProcessId(WindowFromPoint(point),out process);
        return process==(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
    }
    public static void Move(Point point) {
        ValidatePoint(point);
        if(IsOwnWindow(point)) throw new InvalidOperationException("Punkt kliknięcia zasłania Flicker. Przesuń lub zminimalizuj jego okno i uruchom ponownie.");
        if(!SetCursorPos(point.X,point.Y)) throw new InvalidOperationException("Windows nie pozwolił przesunąć kursora.");
    }
}

class MousePointDialog : GradientForm {
    readonly Timer timer=new Timer();
    public Point SelectedPoint { get; private set; }
    public MousePointDialog(Point initial) {
        Text="Flicker — punkt kliknięcia"; ClientSize=new Size(450,285); Font=new Font("Segoe UI",10);
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
        var x=new NumericUpDown { Left=75,Top=28,Width=130,Minimum=-100000,Maximum=100000,Value=initial.X };
        var y=new NumericUpDown { Left=285,Top=28,Width=130,Minimum=-100000,Maximum=100000,Value=initial.Y };
        Controls.Add(new Label { Text="X",Left=30,Top=32,AutoSize=true }); Controls.Add(new Label { Text="Y",Left=240,Top=32,AutoSize=true }); Controls.Add(x); Controls.Add(y);
        var info=new Label { Text="Wybierz punkt i ustaw tam kursor w ciągu 3 sekund.\nNie musisz klikać. Możesz też wpisać współrzędne.\nPrzesuń to okno, jeśli zasłania miejsce docelowe.",Left=25,Top=123,Width=405,Height=75 }; Controls.Add(info);
        var pick=new ModernButton { Text="Wybierz punkt za 3 s",Left=25,Top=72,Width=390 }; Controls.Add(pick);
        var ok=new ModernButton { Text="Zapisz punkt",Left=190,Top=225,Width=130 }; var cancel=new ModernButton { Text="Anuluj",Left=330,Top=225,Width=95,DialogResult=DialogResult.Cancel };
        Controls.Add(ok); Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
        DateTime deadline=DateTime.MinValue;
        pick.Click+=delegate { deadline=DateTime.UtcNow.AddSeconds(3); pick.Enabled=ok.Enabled=x.Enabled=y.Enabled=false; timer.Start(); };
        timer.Interval=50;
        timer.Tick+=delegate {
            double remaining=(deadline-DateTime.UtcNow).TotalSeconds;
            if(remaining>0) { info.Text="Ustaw kursor w miejscu kliknięcia… " + Math.Ceiling(remaining) + " s"; return; }
            timer.Stop(); pick.Enabled=ok.Enabled=x.Enabled=y.Enabled=true;
            try { Point point=MouseInput.Position(); if(MouseInput.IsOwnWindow(point)) throw new ArgumentException("Wskaż punkt poza oknami Flickera."); x.Value=point.X; y.Value=point.Y; info.Text="Zapisano pozycję kursora. Kliknij Zapisz punkt."; }
            catch(Exception ex) { info.Text=ex.Message; }
        };
        ok.Click+=delegate {
            try { var point=new Point((int)x.Value,(int)y.Value); MouseInput.ValidatePoint(point); SelectedPoint=point; DialogResult=DialogResult.OK; }
            catch(Exception ex) { info.Text=ex.Message; }
        };
        Theme.Inputs(this);
    }
    protected override void Dispose(bool disposing) { if(disposing) timer.Dispose(); base.Dispose(disposing); }
}
