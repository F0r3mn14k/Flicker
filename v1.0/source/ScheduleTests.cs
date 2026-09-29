using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

static class ScheduleTests {
    static void Check(bool ok,string label) { if(!ok) throw new Exception(label); }
    public static void Run() {
        var day=new DateTime(2026,9,28);
        var rule=new Rule { IsPause=true,Time="09:59:59",PauseByDuration=true,PauseMinutes=10 };
        ScheduledPause.Validate(rule);
        Check(!ScheduledPause.EndAt(rule,day.AddHours(9)).HasValue,"Before pause");
        var start=day.AddHours(10).AddSeconds(-1); var end=start.AddMinutes(10);
        Check(ScheduledPause.EndAt(rule,start)==end,"Duration exact start");
        Check(ScheduledPause.EndAt(rule,end.AddMilliseconds(-1))==end,"Before duration end");
        Check(!ScheduledPause.EndAt(rule,end).HasValue,"Duration exclusive end");
        rule.PauseByDuration=false; rule.PauseEnd="10:10:00";
        Check(ScheduledPause.EndAt(rule,start)==day.AddHours(10).AddMinutes(10),"Until time");
        rule.Time="23:59:00"; rule.PauseMinutes=10; rule.PauseByDuration=true;
        Check(ScheduledPause.EndAt(rule,day.AddDays(1).AddMinutes(2))==day.AddDays(1).AddMinutes(9),"Duration across midnight");
        rule.PauseByDuration=false; rule.PauseEnd="00:10:00";
        Check(ScheduledPause.EndAt(rule,day.AddDays(1))==day.AddDays(1).AddMinutes(10),"Until across midnight");
        var first=new Rule { IsPause=true,Time="10:00:00",PauseMinutes=10 };
        var second=new Rule { IsPause=true,Time="10:05:00",PauseMinutes=20 };
        var rules=new List<Rule>{first,second,new Rule()};
        Check(ScheduledPause.CombinedEnd(rules,day.AddHours(10).AddMinutes(6))==day.AddHours(10).AddMinutes(25),"Overlapping pauses");
        second.Enabled=false;
        Check(ScheduledPause.CombinedEnd(rules,day.AddHours(10).AddMinutes(6))==day.AddHours(10).AddMinutes(10),"Disabled pause ignored");
        Check(!ScheduledPause.CombinedEnd(rules,day.AddHours(11)).HasValue,"Resumed after union");
        using(var memory=new MemoryStream()) {
            var xml=new XmlSerializer(typeof(List<Rule>)); xml.Serialize(memory,rules); memory.Position=0;
            var loaded=(List<Rule>)xml.Deserialize(memory);
            Check(loaded[0].IsPause && loaded[0].PauseMinutes==10 && !loaded[2].IsPause,"Mixed list persistence");
        }
        using(var old=new StringReader("<Rule><Key>A</Key></Rule>")) {
            var loaded=(Rule)new XmlSerializer(typeof(Rule)).Deserialize(old);
            Check(!loaded.IsPause && loaded.PauseMinutes==10,"Legacy rule remains keyboard");
        }
        using(var picker=new KeyPicker()) {
            picker.Accept(Keys.Enter); Check(picker.SelectedKey=="Enter","Enter canonical alias");
            picker.Accept(Keys.D1); Check(picker.SelectedKey=="D1","Digit capture");
            picker.Accept(Keys.Tab); Check(picker.SelectedKey=="Tab","Tab capture");
            picker.Accept(Keys.F9); Check(picker.SelectedKey=="Tab","Stop hotkey reserved");
            picker.SelectedKey=MouseInput.Right; Check(MouseInput.IsMouse(picker.SelectedKey),"Mouse selection retained");
            var button=picker.Controls[0];
            foreach(var code in new Keys[] { Keys.A, Keys.Enter, Keys.Tab, Keys.Escape, Keys.Space, Keys.Left, Keys.Oemtilde }) {
                picker.BeginListening();
                var message=Message.Create(button.Handle,0x100,new IntPtr((int)code),IntPtr.Zero);
                Check(button.PreProcessMessage(ref message),"Capture handles key before button/dialog: "+code);
                Check((Keys)Enum.Parse(typeof(Keys),picker.SelectedKey)==code,"Captured through WinForms pipeline: "+code);
                Check(KeyPicker.Listening==null,"Capture ends after selection");
            }
            Check(picker.SelectedKey=="Oemtilde","Backtick canonical key name");
            Check(KeyPicker.DisplayName(picker.SelectedKey)=="` / ~","Backtick readable label");
            var down=Keyboard.Events(picker.SelectedKey)[0];
            var up=Keyboard.Events(picker.SelectedKey)[1];
            Check(down.data.key.scan!=0 && down.data.key.scan==up.data.key.scan && (down.data.key.flags&2)==0 && (up.data.key.flags&2)!=0,"Backtick down/up scan code");
            using(var memory=new MemoryStream()) {
                var xml=new XmlSerializer(typeof(Rule)); xml.Serialize(memory,new Rule { Key=picker.SelectedKey }); memory.Position=0;
                var loaded=(Rule)xml.Deserialize(memory);
                Check(loaded.Key=="Oemtilde" && ClickerForm.ActionNames().Contains(loaded.Key),"Backtick saved rule accepted on reload");
            }
        }
        Console.WriteLine("PASS: keyboard capture, pause duration/end time, midnight, overlap, disabled rows, persistence and legacy defaults.");
    }
}
