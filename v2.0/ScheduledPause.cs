using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

static class ScheduledPause {
    public static DateTime? EndAt(Rule rule,DateTime now) {
        if(!rule.IsPause || !rule.Enabled) return null;
        DateTime start=now.Date+TimeSpan.Parse(rule.Time);
        if(start>now) start=start.AddDays(-1);
        DateTime end;
        if(rule.PauseByDuration) end=start.AddMinutes(rule.PauseMinutes);
        else { end=start.Date+TimeSpan.Parse(rule.PauseEnd); if(end<=start) end=end.AddDays(1); }
        return now>=start && now<end ? (DateTime?)end : null;
    }
    public static DateTime? CombinedEnd(IEnumerable<Rule> rules,DateTime now) {
        DateTime? end=null;
        foreach(var rule in rules) { var candidate=EndAt(rule,now); if(candidate.HasValue && (!end.HasValue || candidate.Value>end.Value)) end=candidate; }
        return end;
    }
    public static void Validate(Rule rule) {
        TimeSpan start,end;
        if(!TimeSpan.TryParse(rule.Time,out start) || start<TimeSpan.Zero || start>=TimeSpan.FromDays(1)) throw new ArgumentException("Nieprawidłowa godzina początku.");
        if(rule.PauseByDuration) { if(double.IsNaN(rule.PauseMinutes) || rule.PauseMinutes<.01 || rule.PauseMinutes>1440) throw new ArgumentException("Czas wstrzymania: od 0,01 do 1440 minut."); }
        else if(!TimeSpan.TryParse(rule.PauseEnd,out end) || end<TimeSpan.Zero || end>=TimeSpan.FromDays(1) || end==start) throw new ArgumentException("Godzina końca musi być różna od początku.");
    }
}

class ScheduledPauseDialog : GradientForm {
    public Rule Result { get; private set; }
    public ScheduledPauseDialog(Rule existing) {
        var rule=existing ?? new Rule { IsPause=true,Time="09:59:59",PauseMinutes=10,PauseEnd="10:10:00",PauseByDuration=true };
        Text="Flicker — wstrzymaj wykonywanie"; ClientSize=new Size(450,320); Font=new Font("Segoe UI",10);
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
        var start=new DateTimePicker { Left=235,Top=25,Width=180,Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm:ss",ShowUpDown=true,Value=DateTime.Today+TimeSpan.Parse(rule.Time) };
        var mode=new ComboBox { Left=235,Top=70,Width=180,DropDownStyle=ComboBoxStyle.DropDownList };
        mode.Items.AddRange(new object[] { "Na X minut", "Do godziny" }); mode.SelectedIndex=rule.PauseByDuration?0:1;
        var minutes=new NumericUpDown { Left=235,Top=115,Width=180,Minimum=.01m,Maximum=1440,DecimalPlaces=2,Value=(decimal)rule.PauseMinutes };
        var end=new DateTimePicker { Left=235,Top=160,Width=180,Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm:ss",ShowUpDown=true,Value=DateTime.Today+TimeSpan.Parse(rule.PauseEnd) };
        EventHandler update=delegate { minutes.Enabled=mode.SelectedIndex==0; end.Enabled=mode.SelectedIndex==1; }; mode.SelectedIndexChanged+=update; update(null,EventArgs.Empty);
        string[] labels={"Codziennie od","Wstrzymaj","Liczba minut","Godzina zakończenia"};
        for(int i=0;i<labels.Length;i++) Controls.Add(new Label { Text=labels[i],Left=25,Top=29+i*45,AutoSize=true });
        Controls.AddRange(new Control[]{start,mode,minutes,end});
        Controls.Add(new Label { Text="Wstrzymuje klawiaturę i mysz. Po końcu interwały\nruszają od nowa, bez nadrabiania kliknięć.",Left=25,Top=209,Width=395,Height=45 });
        var save=new ModernButton { Text="Zapisz",Left=205,Top=266,Width=100 };
        save.Click+=delegate {
            var result=new Rule { IsPause=true,Enabled=rule.Enabled,Mode="O godzinie",Time=start.Value.ToString("HH:mm:ss"),PauseByDuration=mode.SelectedIndex==0,PauseMinutes=(double)minutes.Value,PauseEnd=end.Value.ToString("HH:mm:ss") };
            try { ScheduledPause.Validate(result); Result=result; DialogResult=DialogResult.OK; } catch(Exception ex) { MessageBox.Show(this,ex.Message); }
        };
        var cancel=new ModernButton { Text="Anuluj",Left=315,Top=266,Width=100,DialogResult=DialogResult.Cancel };
        Controls.Add(save); Controls.Add(cancel); AcceptButton=save; CancelButton=cancel; Theme.Inputs(this);
    }
}
