using System;
using System.Drawing;
using System.Windows.Forms;

public class PauseSettings {
    public bool Enabled { get; set; }
    public string Start { get; set; }
    public string End { get; set; }
    public string ResumeKey { get; set; }
    public PauseSettings() { Start = "09:59:59"; End = "10:10:00"; ResumeKey = "Enter"; }
    public void Validate() {
        TimeSpan start, end;
        if(!TimeSpan.TryParse(Start,out start) || !TimeSpan.TryParse(End,out end) ||
           start < TimeSpan.Zero || end < TimeSpan.Zero || start >= TimeSpan.FromDays(1) || end >= TimeSpan.FromDays(1) || start == end)
            throw new ArgumentException("Ustaw różne godziny początku i końca przerwy.");
        if(!ClickerForm.KeyNames().Contains(ResumeKey)) throw new ArgumentException("Wybierz klawisz wznowienia.");
    }
    public DateTime? EndOfCurrentPause(DateTime now) {
        if(!Enabled) return null;
        TimeSpan start = TimeSpan.Parse(Start), end = TimeSpan.Parse(End);
        if(start < end) return now.TimeOfDay >= start && now.TimeOfDay < end ? (DateTime?)(now.Date + end) : null;
        if(now.TimeOfDay >= start) return now.Date.AddDays(1) + end;
        if(now.TimeOfDay < end) return now.Date + end;
        return null;
    }
}

enum PauseAction { Continue, EnterPause, Wait, SendResumeKey, Complete }

// Pure state machine: the caller owns key delivery and acknowledges it only on success.
class PauseController {
    public bool Active { get; private set; }
    public bool KeySent { get; private set; }
    public DateTime Until { get; private set; }
    DateTime completedUntil;
    public PauseAction Advance(PauseSettings settings, DateTime now, bool anyHeld) {
        if(Active) {
            if(KeySent) {
                if(anyHeld) return PauseAction.Wait;
                Active = false; completedUntil = Until; return PauseAction.Complete;
            }
            return now >= Until ? PauseAction.SendResumeKey : PauseAction.Wait;
        }
        DateTime? until = settings.EndOfCurrentPause(now);
        if(until.HasValue && until.Value > completedUntil) {
            Until = until.Value; Active = true; KeySent = false; return PauseAction.EnterPause;
        }
        return PauseAction.Continue;
    }
    public void AcknowledgeKey() { KeySent = true; }
    public void Cancel() { Active = false; KeySent = false; }
}

class PauseDialog : GradientForm {
    public PauseSettings Result { get; private set; }
    public PauseDialog(PauseSettings value) {
        Text = "Flicker — przerwa w harmonogramie"; ClientSize = new Size(470,340);
        Font = new Font("Segoe UI",10); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        var enabled = new CheckBox { Text="Włącz codzienną przerwę", Checked=value.Enabled, Left=22, Top=20, Width=400, BackColor=Color.Transparent };
        var start = new DateTimePicker { Left=255,Top=67,Width=175,Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm:ss",ShowUpDown=true,Value=DateTime.Today+TimeSpan.Parse(value.Start) };
        var end = new DateTimePicker { Left=255,Top=109,Width=175,Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm:ss",ShowUpDown=true,Value=DateTime.Today+TimeSpan.Parse(value.End) };
        var key = new ComboBox { Left=255,Top=151,Width=175,DropDownStyle=ComboBoxStyle.DropDownList };
        key.Items.AddRange(ClickerForm.KeyNames().ToArray()); key.SelectedItem=value.ResumeKey;
        Controls.AddRange(new Control[] { enabled,start,end,key });
        string[] titles = { "Pauza od", "Wznowienie o", "Klawisz na zakończenie" };
        for(int i=0;i<titles.Length;i++) Controls.Add(new Label { Text=titles[i],Left=22,Top=71+i*42,AutoSize=true });
        Controls.Add(new Label { Text="Podczas przerwy zwykłe wpisy są wstrzymane.\nNa końcu aplikacja naciska wybrany klawisz, puszcza go\ni rozpoczyna interwały od nowa. Godziny: zegar komputera.",Left=22,Top=200,Width=426,Height=65 });
        var save = new ModernButton { Text="Zapisz",Left=225,Top=284,Width=100 };
        save.Click += delegate {
            var result = new PauseSettings { Enabled=enabled.Checked,Start=start.Value.ToString("HH:mm:ss"),End=end.Value.ToString("HH:mm:ss"),ResumeKey=key.Text };
            try { result.Validate(); Result=result; DialogResult=DialogResult.OK; }
            catch(Exception ex) { MessageBox.Show(this,ex.Message,"Sprawdź godziny",MessageBoxButtons.OK,MessageBoxIcon.Information); }
        };
        var cancel = new ModernButton { Text="Anuluj",Left=335,Top=284,Width=100,DialogResult=DialogResult.Cancel };
        Controls.Add(save); Controls.Add(cancel); AcceptButton=save; CancelButton=cancel; Theme.Inputs(this);
    }
}
