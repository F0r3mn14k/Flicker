using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

static class ProfileTests {
    static void Check(bool ok,string label) { if(!ok) throw new Exception("Profiles: "+label); }
    static bool Reject(Action action) { try { action(); return false; } catch(Exception) { return true; } }
    public static void Run() {
        var profile=new FlickerProfile { Name="Zażółć gęślą jaźń" };
        profile.Rules.Add(new Rule { Key="OemMinus",Count=42,Next=9,Pending=true });
        profile.Rules.Add(new Rule { Key=MouseInput.Right,MouseX=-80,MouseY=900 });
        profile.Rules.Add(new Rule { IsPause=true,Time="09:59:59",PauseMinutes=10 });
        profile.Sequence.Repeats=20; profile.Sequence.Steps.Add(new SequenceStep { Key="Oemplus" }); profile.Sequence.Steps.Add(new SequenceStep { Wait=true,Seconds=2 });
        profile.Validate();
        using(var stream=new MemoryStream()) {
            new XmlSerializer(typeof(FlickerProfile)).Serialize(stream,profile); stream.Position=0;
            var imported=ProfileFiles.Read<FlickerProfile>(stream); imported.Validate();
            Check(imported.Name==profile.Name && imported.Rules.Count==3 && imported.Sequence.Repeats==20 && imported.Sequence.Steps[1].Wait,"full export/import round trip");
            Check(imported.Rules[0].Count==0 && !imported.Rules[0].Pending && imported.Rules[0].Next==0,"runtime not exported");
            Check(imported.Rules[1].MouseX==-80 && imported.Rules[2].IsPause,"mouse and pause preserved");
        }
        var library=new ProfileLibrary { ActiveId=profile.Id }; library.Profiles.Add(profile);
        var copy=library.Import(profile); Check(copy.Id!=profile.Id && copy.Name!=profile.Name && library.ActiveId==profile.Id,"collision creates copy without activation");
        copy.Rules[0].Key="A"; copy.Sequence.Steps[0].Key="B"; Check(profile.Rules[0].Key=="OemMinus" && profile.Sequence.Steps[0].Key=="Oemplus","profiles independent");
        var edit=library.Copy(); edit.Profiles[0].Name="Edited"; Check(profile.Name!="Edited","cancel leaves original library intact");
        library.ActiveId=copy.Id;
        using(var stream=new MemoryStream()) { new XmlSerializer(typeof(ProfileLibrary)).Serialize(stream,library); stream.Position=0; var loaded=ProfileFiles.Read<ProfileLibrary>(stream); loaded.Validate(); Check(loaded.Active.Id==copy.Id,"active profile persists"); }
        var invalid=profile.Copy(); invalid.FormatVersion=99; int count=library.Profiles.Count;
        Check(Reject(delegate { library.Import(invalid); }) && library.Profiles.Count==count,"unsupported import leaves library unchanged");
        invalid=profile.Copy(); invalid.Rules[0].Seconds=double.NaN; Check(Reject(delegate { invalid.Validate(); }),"invalid timing rejected");
        invalid=profile.Copy(); invalid.Rules[0].Key="F9"; Check(Reject(delegate { invalid.Validate(); }),"reserved key rejected");
        invalid=profile.Copy(); invalid.Sequence.Steps[0].HoldMs=0; Check(Reject(delegate { invalid.Validate(); }),"invalid sequence rejected");
        using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("<!DOCTYPE FlickerProfile [<!ENTITY x SYSTEM 'file:///missing'>]><FlickerProfile><Name>&x;</Name></FlickerProfile>"))) Check(Reject(delegate { ProfileFiles.Read<FlickerProfile>(stream); }),"DTD prohibited");
        using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("<FlickerProfile>"))) Check(Reject(delegate { ProfileFiles.Read<FlickerProfile>(stream); }),"truncated import rejected");
        string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"profile-test-"+Guid.NewGuid().ToString("N")); string path=Path.Combine(folder,"profile.xml");
        try { ProfileFiles.Write(path,profile); var changed=profile.Copy(); changed.Name="Nowa nazwa"; ProfileFiles.Write(path,changed); Check(ProfileFiles.Read<FlickerProfile>(path).Name==changed.Name && ProfileFiles.Read<FlickerProfile>(path+".bak").Name==profile.Name,"atomic replacement and backup"); }
        finally { if(File.Exists(path)) File.Delete(path); if(File.Exists(path+".bak")) File.Delete(path+".bak"); if(Directory.Exists(folder)) Directory.Delete(folder); }
        Console.WriteLine("PASS: profiles export/import, independence, collisions, active selection, validation, XML and atomic backup.");
    }
}
