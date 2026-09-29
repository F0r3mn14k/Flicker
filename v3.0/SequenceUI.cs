using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

class SequenceForm : GradientForm {
    SequenceDocument document=new SequenceDocument();
    readonly HeldKeys held=new HeldKeys(Keyboard.SetKey);
    readonly Stopwatch watch=Stopwatch.StartNew();
    readonly Timer timer=new Timer();
    readonly DataGridView grid=new DataGridView();
    readonly Label progress=new Label();
    readonly NumericUpDown repeats=new NumericUpDown { Minimum=1,Maximum=10000,Value=1,Width=85 };
    readonly FlowLayoutPanel editing=new FlowLayoutPanel();
    readonly ModernButton play=new ModernButton { Text="Start · F8",Primary=true,Width=125,Height=38 };
    readonly ModernButton once=new ModernButton { Text="Jeden krok",Width=125,Height=38 };
    readonly SequenceRunner runner;
    readonly bool preview;
    bool loadFailed;
    Point dragStart; int dragIndex=-1;
    readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlickerV3","sequence.xml");
    public bool IsRunning { get { return runner.Running; } }
    public SequenceForm(bool preview=false) {
        this.preview=preview;
        runner=new SequenceRunner(delegate(SequenceStep step) {
            if(MouseInput.IsMouse(step.Key)) MouseInput.Move(new Point(step.X,step.Y));
            if(!held.Begin(step.Key,watch.Elapsed.TotalSeconds,step.HoldMs)) throw new InvalidOperationException("Poprzedni klawisz nie został zwolniony.");
        },delegate { return held.Count>0; });
        Text="Flicker 3.0 — Sekwencje"; Size=new Size(780,650); MinimumSize=new Size(720,560); Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterParent;
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=6 };
        foreach(float height in new float[] { 65,48,44 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,54)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,64)); Controls.Add(layout);
        layout.Controls.Add(new Label { Text="Sekwencje\nKolejne kroki · przeciągnij wiersz, aby zmienić kolejność",Dock=DockStyle.Fill,ForeColor=Theme.Ink,Font=new Font("Segoe UI",12) },0,0);
        editing.Dock=DockStyle.Fill; editing.WrapContents=false;
        editing.Controls.Add(ActionButton("+ Klawisz",delegate { Edit(-1,false,false); }));
        editing.Controls.Add(ActionButton("+ Mysz",delegate { Edit(-1,false,true); }));
        editing.Controls.Add(ActionButton("+ Czekaj",delegate { Edit(-1,true,false); }));
        editing.Controls.Add(new Label { Text="Powtórzenia",AutoSize=true,Margin=new Padding(10,9,4,0),ForeColor=Theme.Muted }); editing.Controls.Add(repeats);
        repeats.ValueChanged+=delegate { document.Repeats=(int)repeats.Value; Save(); }; layout.Controls.Add(editing,0,1);
        var order=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
        order.Controls.Add(ActionButton("Edytuj",delegate { if(grid.CurrentRow!=null) Edit(grid.CurrentRow.Index,false,false); }));
        order.Controls.Add(ActionButton("Duplikuj",delegate { if(!IsRunning && grid.CurrentRow!=null) { int i=grid.CurrentRow.Index; document.Steps.Insert(i+1,document.Steps[i].Copy()); RefreshSteps(i+1); Save(); } }));
        order.Controls.Add(ActionButton("Usuń",delegate { if(!IsRunning && grid.CurrentRow!=null) { int i=grid.CurrentRow.Index; document.Steps.RemoveAt(i); RefreshSteps(Math.Max(0,i-1)); Save(); } }));
        order.Controls.Add(ActionButton("W górę",delegate { MoveSelected(-1); })); order.Controls.Add(ActionButton("W dół",delegate { MoveSelected(1); })); layout.Controls.Add(order,0,2);
        grid.Dock=DockStyle.Fill; grid.AllowUserToAddRows=grid.AllowUserToDeleteRows=false; grid.ReadOnly=true; grid.RowHeadersVisible=false; grid.MultiSelect=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; grid.BackgroundColor=Theme.Surface; grid.BorderStyle=BorderStyle.None;
        grid.EnableHeadersVisualStyles=false; grid.ColumnHeadersDefaultCellStyle.BackColor=Theme.Input; grid.ColumnHeadersDefaultCellStyle.ForeColor=Theme.Ink; grid.ColumnHeadersHeight=36; grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None; grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal; repeats.BackColor=Theme.Input; repeats.ForeColor=Theme.Ink;
        grid.DefaultCellStyle.BackColor=Theme.Surface; grid.DefaultCellStyle.ForeColor=Theme.Ink; grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(32,77,77); grid.DefaultCellStyle.SelectionForeColor=Theme.Ink; grid.RowTemplate.Height=40; grid.GridColor=Theme.Border;
        foreach(string name in new string[] { "Krok","Akcja","Czas / punkt" }) grid.Columns.Add(name,name);
        grid.Columns[0].FillWeight=30; grid.Columns[1].FillWeight=120; grid.Columns[2].FillWeight=150;
        foreach(DataGridViewColumn col in grid.Columns) col.SortMode=DataGridViewColumnSortMode.NotSortable;
        grid.CellDoubleClick+=delegate(object sender,DataGridViewCellEventArgs e) { if(e.RowIndex>=0) Edit(e.RowIndex,false,false); };
        grid.AllowDrop=true;
        grid.MouseDown+=delegate(object sender,MouseEventArgs e) { dragStart=e.Location; dragIndex=e.Button==MouseButtons.Left?grid.HitTest(e.X,e.Y).RowIndex:-1; };
        grid.MouseMove+=delegate(object sender,MouseEventArgs e) { if(!IsRunning && dragIndex>=0 && e.Button==MouseButtons.Left && (Math.Abs(e.X-dragStart.X)>SystemInformation.DragSize.Width || Math.Abs(e.Y-dragStart.Y)>SystemInformation.DragSize.Height)) { int index=dragIndex; dragIndex=-1; grid.DoDragDrop(index,DragDropEffects.Move); } };
        grid.DragOver+=delegate(object sender,DragEventArgs e) { e.Effect=!IsRunning && e.Data.GetDataPresent(typeof(int))?DragDropEffects.Move:DragDropEffects.None; };
        grid.DragDrop+=delegate(object sender,DragEventArgs e) { if(IsRunning || !e.Data.GetDataPresent(typeof(int))) return; Point p=grid.PointToClient(new Point(e.X,e.Y)); MoveStep((int)e.Data.GetData(typeof(int)),grid.HitTest(p.X,p.Y).RowIndex); };
        layout.Controls.Add(grid,0,3);
        var transport=new FlowLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0) }; play.Click+=delegate { Toggle(); }; once.Click+=delegate { if(grid.CurrentRow!=null) Begin(grid.CurrentRow.Index); }; transport.Controls.Add(play); transport.Controls.Add(once); var stop=ActionButton("Stop · F9",delegate { Halt("Zatrzymano."); }); stop.Danger=true; transport.Controls.Add(stop); layout.Controls.Add(transport,0,4);
        progress.Dock=DockStyle.Fill; progress.ForeColor=Theme.Muted; progress.Text="Dodaj kroki. Start daje 3 sekundy na wybranie okna docelowego."; layout.Controls.Add(progress,0,5);
        if(!preview) LoadDocument(); else { document.Steps.Add(new SequenceStep { Key="D1" }); document.Steps.Add(new SequenceStep { Wait=true,Seconds=2 }); document.Steps.Add(new SequenceStep { Key="Enter" }); }
        repeats.Value=document.Repeats; RefreshSteps(0);
        timer.Interval=25; timer.Tick+=delegate { TickSequence(); }; if(!preview) timer.Start();
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { Halt("Zamykanie"); if(held.Count>0) { e.Cancel=true; return; } Save(); };
    }
    ModernButton ActionButton(string text,EventHandler handler) { var b=new ModernButton { Text=text,Width=110,Height=36,Margin=new Padding(0,0,6,0) }; b.Click+=handler; return b; }
    void Edit(int index,bool wait,bool mouse) {
        if(IsRunning) return;
        var step=index>=0?document.Steps[index].Copy():new SequenceStep { Wait=wait,Key=mouse?MouseInput.Left:"Space" };
        using(var dialog=new SequenceStepDialog(step,index>=0)) { if(dialog.ShowDialog(this)!=DialogResult.OK) return; }
        if(index>=0) document.Steps[index]=step; else { document.Steps.Add(step); index=document.Steps.Count-1; } RefreshSteps(index); Save();
    }
    void MoveSelected(int delta) { if(grid.CurrentRow!=null) MoveStep(grid.CurrentRow.Index,grid.CurrentRow.Index+delta); }
    void MoveStep(int from,int to) { if(IsRunning || from<0 || to<0 || from>=document.Steps.Count || to>=document.Steps.Count || from==to) return; var step=document.Steps[from]; document.Steps.RemoveAt(from); document.Steps.Insert(to,step); RefreshSteps(to); Save(); }
    void RefreshSteps(int select) { grid.Rows.Clear(); foreach(var step in document.Steps) grid.Rows.Add(grid.Rows.Count+1,step.Wait?"Czekaj":KeyPicker.DisplayName(step.Key),step.Wait?step.Seconds+" s":step.HoldMs+" ms"+(MouseInput.IsMouse(step.Key)?" · X: "+step.X+" Y: "+step.Y:"")); if(select>=0 && select<grid.Rows.Count) grid.CurrentCell=grid.Rows[select].Cells[0]; }
    public void Toggle() { if(IsRunning) Halt("Zatrzymano."); else Begin(-1); }
    void Begin(int index) {
        if(!Enabled || IsRunning) return;
        if(held.Count>0) { progress.Text="Trwa zwalnianie klawiszy. Poczekaj."; return; }
        try { runner.Start(document,watch.Elapsed.TotalSeconds,index); editing.Enabled=false; once.Enabled=false; play.Text="Stop · F8"; progress.Text="Start za 3 s — wybierz okno docelowe."; }
        catch(Exception ex) { progress.Text=ex.Message; }
    }
    public void Halt(string message) { runner.Stop(); try { held.Release(0,true); } catch(Exception ex) { message="Błąd zwalniania: "+ex.Message; } editing.Enabled=true; once.Enabled=true; play.Text="Start · F8"; progress.Text=message; }
    void TickSequence() {
        double now=watch.Elapsed.TotalSeconds;
        try {
            held.Release(now,!IsRunning);
            if(!IsRunning) return;
            bool own=Form.ActiveForm!=null;
            runner.Tick(now,!own);
            if(!IsRunning) { Halt("Sekwencja zakończona."); return; }
            if(grid.CurrentRow==null || grid.CurrentRow.Index!=runner.Index) grid.CurrentCell=grid.Rows[runner.Index].Cells[0];
            progress.Text="Powtórzenie "+runner.Cycle+" / "+document.Repeats+" · Krok "+(runner.Index+1)+" / "+document.Steps.Count+"\n"+runner.Phase+" · "+runner.Remaining(now).ToString("0.0")+" s"+(own?" · Wybierz inne okno, aby wysłać klawisz.":"");
        } catch(Exception ex) { Halt(ex.Message); }
    }
    void LoadDocument() { if(!File.Exists(path)) return; try { using(var stream=File.OpenRead(path)) { var loaded=(SequenceDocument)new XmlSerializer(typeof(SequenceDocument)).Deserialize(stream); loaded.Validate(); document=loaded; } } catch(Exception ex) { loadFailed=true; progress.Text="Błąd odczytu sekwencji: "+ex.Message; } }
    void Save() {
        if(preview || loadFailed) return;
        try { Directory.CreateDirectory(Path.GetDirectoryName(path)); string temp=path+".tmp"; using(var stream=File.Create(temp)) new XmlSerializer(typeof(SequenceDocument)).Serialize(stream,document); if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path); }
        catch(Exception ex) { progress.Text="Błąd zapisu: "+ex.Message; }
    }
    protected override void Dispose(bool disposing) { if(disposing) timer.Dispose(); base.Dispose(disposing); }
}

class SequenceStepDialog : GradientForm {
    public SequenceStepDialog(SequenceStep step,bool existing) {
        Text=step.Wait?"Krok: oczekiwanie":"Krok: naciśnięcie"; ClientSize=new Size(440,245); Font=new Font("Segoe UI",10); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
        var key=new KeyPicker { Left=150,Top=20,Width=260,SelectedKey=step.Key,Visible=!step.Wait }; Controls.Add(key);
        Controls.Add(new Label { Text=step.Wait?"Czas (sekundy)":"Klawisz / mysz",Left=20,Top=24,AutoSize=true });
        var duration=new NumericUpDown { Left=150,Top=step.Wait?20:75,Width=160,DecimalPlaces=step.Wait?2:0,Minimum=step.Wait?.01m:1,Maximum=step.Wait?86400:5000,Value=step.Wait?(decimal)step.Seconds:step.HoldMs }; Controls.Add(duration);
        if(!step.Wait) Controls.Add(new Label { Text="Przytrzymanie (ms)",Left=20,Top=79,AutoSize=true });
        Point point=new Point(step.X,step.Y); bool chosen=existing && MouseInput.IsMouse(step.Key);
        var pick=new ModernButton { Text=chosen?"Punkt: "+point.X+", "+point.Y:"Wybierz punkt…",Left=150,Top=120,Width=260,Height=36,Visible=!step.Wait,Enabled=MouseInput.IsMouse(step.Key) }; Controls.Add(pick);
        key.SelectedKeyChanged+=delegate { pick.Enabled=MouseInput.IsMouse(key.SelectedKey); };
        pick.Click+=delegate { using(var dialog=new MousePointDialog(point)) if(dialog.ShowDialog(this)==DialogResult.OK) { point=dialog.SelectedPoint; chosen=true; pick.Text="Punkt: "+point.X+", "+point.Y; } };
        var ok=new ModernButton { Text="Zapisz",Primary=true,Left=150,Top=190,Width=120,Height=36 }; Controls.Add(ok);
        ok.Click+=delegate { if(!step.Wait && MouseInput.IsMouse(key.SelectedKey) && !chosen) { MessageBox.Show(this,"Wskaż punkt kliknięcia."); return; } step.Key=key.SelectedKey; if(step.Wait) step.Seconds=(double)duration.Value; else step.HoldMs=(int)duration.Value; step.X=point.X; step.Y=point.Y; step.Validate(); DialogResult=DialogResult.OK; };
        var cancel=new ModernButton { Text="Anuluj",Left=285,Top=190,Width=120,Height=36,DialogResult=DialogResult.Cancel }; Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel; Theme.Inputs(this);
    }
}
