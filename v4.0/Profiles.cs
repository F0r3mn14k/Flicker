using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

public class FlickerProfile {
    public int FormatVersion { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Rule> Rules { get; set; }
    public SequenceDocument Sequence { get; set; }
    public FlickerProfile() { FormatVersion=1; Id=Guid.NewGuid().ToString("N"); Name="Domyślny"; Rules=new List<Rule>(); Sequence=new SequenceDocument(); }
    public override string ToString() { return Name; }
    public void Validate() {
        Guid id;
        if(FormatVersion!=1) throw new InvalidDataException("Nieobsługiwana wersja formatu profilu.");
        if(!Guid.TryParse(Id,out id) || string.IsNullOrWhiteSpace(Name) || Name.Length>80 || Name.IndexOfAny(new char[] { '\r','\n','\t' })>=0 || Rules==null || Rules.Count>10000 || Sequence==null) throw new InvalidDataException("Niepoprawne dane profilu.");
        foreach(var r in Rules) {
            if(r==null) throw new InvalidDataException("Pusty wpis profilu.");
            TimeSpan time,end;
            if(!TimeSpan.TryParse(r.Time,out time) || time<TimeSpan.Zero || time>=TimeSpan.FromDays(1)) throw new InvalidDataException("Niepoprawna godzina wpisu.");
            if(r.IsPause) {
                ScheduledPause.Validate(r);
                if(double.IsNaN(r.PauseMinutes) || r.PauseMinutes<.01 || r.PauseMinutes>1440 || !TimeSpan.TryParse(r.PauseEnd,out end) || end<TimeSpan.Zero || end>=TimeSpan.FromDays(1)) throw new InvalidDataException("Niepoprawne ustawienia wstrzymania.");
            } else if(!ClickerForm.ActionNames().Contains(r.Key) || (r.Mode!="Co ile sekund" && r.Mode!="O godzinie") || double.IsNaN(r.Seconds) || r.Seconds<.05 || r.Seconds>86400 || r.MaxDelayMs<0 || r.MaxDelayMs>60000 || r.HoldMinMs<1 || r.HoldMaxMs>5000 || r.HoldMinMs>r.HoldMaxMs || r.MouseX < -100000 || r.MouseX>100000 || r.MouseY < -100000 || r.MouseY>100000) throw new InvalidDataException("Niepoprawna akcja w profilu.");
        }
        Sequence.Validate();
    }
    public FlickerProfile Copy() {
        var copy=new FlickerProfile { Id=Id,Name=Name,FormatVersion=FormatVersion };
        foreach(var rule in Rules) copy.Rules.Add(ClickerForm.CopyRule(rule));
        copy.Sequence.Repeats=Sequence.Repeats;
        foreach(var step in Sequence.Steps) copy.Sequence.Steps.Add(step.Copy());
        return copy;
    }
}
public class ProfileLibrary {
    public int FormatVersion { get; set; }
    public string ActiveId { get; set; }
    public List<FlickerProfile> Profiles { get; set; }
    public ProfileLibrary() { FormatVersion=1; Profiles=new List<FlickerProfile>(); }
    [XmlIgnore] public FlickerProfile Active { get { return Profiles.Find(delegate(FlickerProfile p) { return p.Id==ActiveId; }); } }
    public void Validate() {
        if(FormatVersion!=1 || Profiles==null || Profiles.Count==0 || Profiles.Count>1000) throw new InvalidDataException("Niepoprawna biblioteka profili.");
        var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var p in Profiles) { if(p==null) throw new InvalidDataException("Pusty profil."); p.Validate(); if(!ids.Add(p.Id) || !names.Add(p.Name)) throw new InvalidDataException("Powtarzający się profil."); }
        if(Active==null) throw new InvalidDataException("Brak aktywnego profilu.");
    }
    public ProfileLibrary Copy() { var copy=new ProfileLibrary { ActiveId=ActiveId }; foreach(var p in Profiles) copy.Profiles.Add(p.Copy()); return copy; }
    public string UniqueName(string requested) {
        string root=requested.Trim(); if(root.Length>65) root=root.Substring(0,65);
        string name=root; int n=2;
        while(Profiles.Exists(delegate(FlickerProfile p) { return string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase); })) name=root+" ("+(n++)+")";
        return name;
    }
    public FlickerProfile Import(FlickerProfile source) {
        source.Validate(); if(Profiles.Count>=1000) throw new InvalidDataException("Osiągnięto limit 1000 profili.");
        var imported=source.Copy(); imported.Id=Guid.NewGuid().ToString("N"); imported.Name=UniqueName(imported.Name); Profiles.Add(imported); return imported;
    }
}
static class ProfileFiles {
    public static T Read<T>(string path) {
        using(var stream=File.OpenRead(path)) return Read<T>(stream);
    }
    public static T Read<T>(Stream stream) {
        var settings=new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=16*1024*1024 };
        using(var reader=XmlReader.Create(stream,settings)) return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
    }
    public static void Write<T>(string path,T value) {
        string directory=Path.GetDirectoryName(Path.GetFullPath(path)); Directory.CreateDirectory(directory);
        string temp=Path.Combine(directory,".flicker-"+Guid.NewGuid().ToString("N")+".tmp");
        try {
            using(var stream=File.Create(temp)) new XmlSerializer(typeof(T)).Serialize(stream,value);
            if(new FileInfo(temp).Length>16*1024*1024) throw new InvalidDataException("Plik profili jest zbyt duży (maks. 16 MB).");
            if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
        } finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}
