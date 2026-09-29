using System;
using System.IO;
using System.Xml.Serialization;

static class PauseTests {
    static void Check(bool condition,string message) { if(!condition) throw new Exception("Pause: "+message); }
    public static void Run() {
        var settings=new PauseSettings { Enabled=true };
        settings.Validate();
        var state=new PauseController();
        var day=new DateTime(2026,9,27);
        Check(state.Advance(settings,day.AddHours(9),false)==PauseAction.Continue,"before start");
        Check(state.Advance(settings,day.AddHours(10).AddSeconds(-1),true)==PauseAction.EnterPause,"exact start");
        Check(state.Advance(settings,day.AddHours(10),false)==PauseAction.Wait,"blocks ordinary rules");
        var end=day.AddHours(10).AddMinutes(10);
        Check(state.Advance(settings,end,false)==PauseAction.SendResumeKey,"exact end");
        state.AcknowledgeKey();
        Check(state.Advance(settings,end,true)==PauseAction.Wait,"wait for key up without repeated key down");
        Check(state.Advance(settings,end,false)==PauseAction.Complete,"complete after key up");
        Check(state.Advance(settings,end,false)==PauseAction.Continue,"no repeated resume");
        Check(state.Advance(settings,day.AddHours(10),false)==PauseAction.Continue,"clock rollback does not repeat completed pause");
        Check(state.Advance(settings,day.AddDays(1).AddHours(10),false)==PauseAction.EnterPause,"next day");
        state.Cancel();
        Check(!state.Active && !state.KeySent,"stop cancels pending resume");
        Check(state.Advance(settings,end.AddDays(1),false)==PauseAction.Continue,"no resume after stop");
        var fresh=new PauseController();
        Check(fresh.Advance(settings,day.AddHours(10),false)==PauseAction.EnterPause,"start inside pause");
        Check(fresh.Advance(settings,end.AddMinutes(5),false)==PauseAction.SendResumeKey,"active pause survives clock jump or sleep");
        settings.Start="23:50:00"; settings.End="00:10:00"; settings.Validate();
        Check(settings.EndOfCurrentPause(day.AddHours(23).AddMinutes(55))==day.AddDays(1).AddMinutes(10),"midnight end date");
        Check(settings.EndOfCurrentPause(day.AddMinutes(5))==day.AddMinutes(10),"start after midnight");
        Check(settings.EndOfCurrentPause(day.AddMinutes(10))==null,"exclusive end");
        settings.Enabled=false;
        Check(settings.EndOfCurrentPause(day.AddMinutes(5))==null,"disabled");
        using(var memory=new MemoryStream()) {
            var xml=new XmlSerializer(typeof(PauseSettings)); xml.Serialize(memory,settings); memory.Position=0;
            var saved=(PauseSettings)xml.Deserialize(memory); saved.Validate();
            Check(saved.End==settings.End && saved.Start==settings.Start && saved.ResumeKey=="Enter" && !saved.Enabled,"settings round trip");
        }
        settings.End=settings.Start; bool rejected=false;
        try { settings.Validate(); } catch(ArgumentException) { rejected=true; }
        Check(rejected,"equal endpoints rejected");
        Console.WriteLine("PASS: pause boundaries, resume key ordering, cancellation, midnight, clock changes and persistence.");
    }
}
