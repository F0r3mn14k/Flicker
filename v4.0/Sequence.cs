using System;
using System.Collections.Generic;

public class SequenceStep {
    public bool Wait { get; set; }
    public string Key { get; set; }
    public double Seconds { get; set; }
    public int HoldMs { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public SequenceStep() { Key="Space"; Seconds=1; HoldMs=80; }
    public SequenceStep Copy() { return (SequenceStep)MemberwiseClone(); }
    public void Validate() {
        if(double.IsNaN(Seconds) || double.IsInfinity(Seconds) || Seconds<.01 || Seconds>86400 || HoldMs<1 || HoldMs>5000 || X < -100000 || X>100000 || Y < -100000 || Y>100000 || (!Wait && !ClickerForm.ActionNames().Contains(Key))) throw new ArgumentException("Niepoprawny krok sekwencji.");
    }
}
public class SequenceDocument {
    public List<SequenceStep> Steps { get; set; }
    public int Repeats { get; set; }
    public SequenceDocument() { Steps=new List<SequenceStep>(); Repeats=1; }
    public void Validate() {
        if(Steps==null || Steps.Count>10000 || Repeats<1 || Repeats>10000) throw new ArgumentException("Niepoprawna sekwencja.");
        foreach(var step in Steps) { if(step==null) throw new ArgumentException("Pusty krok."); step.Validate(); }
    }
}

// One transition per tick: no catch-up bursts after a delayed timer.
class SequenceRunner {
    SequenceDocument document;
    readonly Action<SequenceStep> execute;
    readonly Func<bool> busy;
    bool started, single;
    double deadline;
    public bool Running { get; private set; }
    public int Index { get; private set; }
    public int Cycle { get; private set; }
    public double Remaining(double now) { return Math.Max(0,deadline-now); }
    public SequenceRunner(Action<SequenceStep> execute,Func<bool> busy) { this.execute=execute; this.busy=busy; }
    public void Start(SequenceDocument source,double now,int onlyIndex) {
        source.Validate();
        if(source.Steps.Count==0 || onlyIndex < -1 || onlyIndex>=source.Steps.Count) throw new ArgumentException("Dodaj lub wybierz krok.");
        document=new SequenceDocument { Repeats=source.Repeats };
        foreach(var step in source.Steps) document.Steps.Add(step.Copy());
        Index=onlyIndex<0?0:onlyIndex; Cycle=1; single=onlyIndex>=0; started=false; deadline=now+3; Running=true;
    }
    public void Stop() { Running=false; }
    public string Phase { get { return !Running?"Zakończono":!started?"Gotowy na krok":document.Steps[Index].Wait?"Oczekiwanie":"Przytrzymanie"; } }
    public void Tick(double now,bool canSend) {
        if(!Running || now<deadline) return;
        if(started) {
            if(busy()) return;
            started=false;
            if(single) { Running=false; return; }
            Index++;
            if(Index==document.Steps.Count) { Index=0; Cycle++; if(Cycle>document.Repeats) { Cycle=document.Repeats; Running=false; return; } }
            return;
        }
        var step=document.Steps[Index];
        if(!step.Wait && (!canSend || busy())) return;
        try { if(!step.Wait) execute(step); }
        catch { Running=false; throw; }
        started=true; deadline=now+(step.Wait?step.Seconds:step.HoldMs/1000.0);
    }
}
