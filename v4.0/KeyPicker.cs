using System;
using System.Drawing;
using System.Windows.Forms;

class KeyPicker : UserControl {
    readonly ModernButton capture = new ModernButton();
    readonly ModernButton mouse = new ModernButton();
    string selectedKey = "Space";
    public static KeyPicker Listening { get; private set; }
    public event EventHandler SelectedKeyChanged;
    public string SelectedKey {
        get { return selectedKey; }
        set { selectedKey=value; capture.Text=DisplayName(value); if(SelectedKeyChanged!=null) SelectedKeyChanged(this,EventArgs.Empty); }
    }
    public KeyPicker() {
        Height=32; Width=230;
        capture.Dock=DockStyle.Fill; capture.Text=selectedKey; capture.Font=new Font("Segoe UI",9);
        mouse.Dock=DockStyle.Right; mouse.Width=65; mouse.Text="Mysz ▾"; mouse.Font=new Font("Segoe UI",8);
        var menu=new ContextMenuStrip();
        foreach(string name in new string[] { MouseInput.Left,MouseInput.Right,MouseInput.Middle }) {
            string choice=name; menu.Items.Add(name,null,delegate { Cancel(); SelectedKey=choice; });
        }
        mouse.Click+=delegate { Cancel(); menu.Show(mouse,new Point(0,mouse.Height)); };
        capture.Click+=delegate { BeginListening(); };
        capture.Leave+=delegate { Cancel(); };
        Controls.Add(capture); Controls.Add(mouse);
        Disposed+=delegate { Cancel(); menu.Dispose(); };
    }
    public void BeginListening() { if(Listening!=null) Listening.Cancel(); Listening=this; capture.Text="Naciśnij klawisz…"; }
    public static string DisplayName(string name) {
        switch(name) {
            case "Oemtilde": return "` / ~";
            case "OemMinus": return "- / _";
            case "Oemplus": return "= / +";
            case "Oemcomma": return ", / <";
            case "OemPeriod": return ". / >";
            case "Oem1": return "; / :";
            case "Oem2": return "/ / ?";
            case "Oem4": return "[ / {";
            case "Oem5": return "\\ / |";
            case "Oem6": return "] / }";
            case "Oem7": return "' / \"";
            case "Oem102": return "Dodatkowy klawisz ISO (OEM 102)";
            default: return name;
        }
    }
    public void Cancel() { if(Listening==this) Listening=null; capture.Text=DisplayName(selectedKey); }
    public void Accept(Keys key) {
        foreach(string name in ClickerForm.KeyNames()) {
            if((Keys)Enum.Parse(typeof(Keys),name)!=key) continue;
            Cancel(); SelectedKey=name; return;
        }
        capture.Text="Niedostępny — wybierz inny";
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {
        if(Listening==this) { Accept(keyData & Keys.KeyCode); return true; }
        return base.ProcessCmdKey(ref msg,keyData);
    }
}
