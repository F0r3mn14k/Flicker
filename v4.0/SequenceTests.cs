using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

static class SequenceTests {
    static void Check(bool condition,string label) { if(!condition) throw new Exception("Sequence: "+label); }
    public static void Run() {
        var seen=new List<string>(); bool busy=false;
        var runner=new SequenceRunner(delegate(SequenceStep s) { seen.Add(s.Key); },delegate { return busy; });
        var doc=new SequenceDocument { Repeats=2 };
        doc.Steps.Add(new SequenceStep { Key="D1",HoldMs=100 });
        doc.Steps.Add(new SequenceStep { Wait=true,Seconds=2 });
        doc.Steps.Add(new SequenceStep { Key="Enter",HoldMs=100 });
        runner.Start(doc,0,-1); runner.Tick(2.99,true); Check(seen.Count==0,"start grace");
        runner.Tick(3,false); Check(seen.Count==0 && runner.Index==0,"own window does not skip step");
        runner.Tick(3,true); Check(seen.Count==1 && seen[0]=="D1","first key");
        busy=true; runner.Tick(4,true); Check(runner.Index==0,"wait for successful release"); busy=false;
        runner.Tick(4,true); Check(runner.Index==1,"advance only after release");
        runner.Tick(4,true); runner.Tick(5.99,true); Check(runner.Index==1 && seen.Count==1,"wait duration");
        runner.Tick(6,true); Check(runner.Index==2,"wait boundary"); runner.Tick(6,true); Check(seen[1]=="Enter","ordered input");
        runner.Tick(7,true); Check(runner.Cycle==2 && runner.Index==0,"repeat boundary");
        runner.Tick(7,true); runner.Tick(8,true); runner.Tick(8,true); runner.Tick(10,true); runner.Tick(10,true); runner.Tick(11,true);
        Check(!runner.Running && seen.Count==4,"finite repeat completion");
        runner.Start(doc,20,2); runner.Tick(23,true); runner.Tick(24,true); Check(!runner.Running && seen.Count==5,"one selected step only");
        runner.Start(doc,30,-1); runner.Stop(); runner.Tick(100,true); Check(seen.Count==5,"cancel countdown");
        runner.Start(doc,0,1); runner.Tick(3,true); runner.Stop(); runner.Tick(100,true); Check(seen.Count==5 && !runner.Running,"cancel wait");
        runner.Start(doc,0,-1); runner.Tick(1000,true); Check(seen.Count==6,"delayed tick does not burst"); runner.Stop();
        runner.Start(doc,0,-1); doc.Steps[0].Key="A"; runner.Tick(3,true); Check(seen[6]=="D1","running snapshot immutable"); runner.Stop();
        var mouse=new SequenceDocument(); mouse.Steps.Add(new SequenceStep { Key=MouseInput.Right,X=-50,Y=200 });
        using(var stream=new MemoryStream()) { var xml=new XmlSerializer(typeof(SequenceDocument)); xml.Serialize(stream,mouse); stream.Position=0; var loaded=(SequenceDocument)xml.Deserialize(stream); loaded.Validate(); Check(loaded.Steps[0].X==-50 && loaded.Steps[0].Key==MouseInput.Right,"mouse persistence"); }
        bool rejected=false; try { new SequenceStep { Seconds=double.NaN }.Validate(); } catch(ArgumentException) { rejected=true; } Check(rejected,"invalid timing rejected");
        var failing=new SequenceRunner(delegate { throw new InvalidOperationException("input failure"); },delegate { return false; }); failing.Start(doc,0,-1); try { failing.Tick(3,true); } catch(InvalidOperationException) { } Check(!failing.Running,"input failure halts sequence");
        Console.WriteLine("PASS: sequence ordering, waits, repeats, cancellation, single step, release gating, snapshots, XML and failures.");
    }
}
