using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

partial class ClickerForm {
    TableLayoutPanel dashboard;
    Panel listArea;
    FlowLayoutPanel editActions;
    Label totals, empty;
    ModernButton compactButton, editButton, duplicateButton, deleteButton;
    readonly CheckBox autoCompact=new CheckBox(), pinWindow=new CheckBox();
    bool compact;
    Size expandedSize=new Size(760,720);

    void BuildV2() {
        Text="Flicker 2.5"; Size=expandedSize; MinimumSize=new Size(680,620);
        StartPosition=FormStartPosition.CenterScreen; Font=new Font("Segoe UI",10); BackColor=Theme.Background;
        dashboard=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=8,BackColor=Theme.Background };
        foreach(float height in new float[] { 72,0,44,54 }) dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,56)); dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,36)); dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
        Controls.Add(dashboard);

        var header=new Panel { Dock=DockStyle.Fill,BackColor=Theme.Background };
        header.Controls.Add(new Label { Text="Flicker",Font=new Font("Segoe UI",25,FontStyle.Bold),ForeColor=Theme.Ink,AutoSize=true,Location=new Point(0,0) });
        header.Controls.Add(new Label { Text="2.5  /  TWOJE AUTOMATYZACJE",Font=new Font("Segoe UI",8,FontStyle.Bold),ForeColor=Theme.Muted,AutoSize=true,Location=new Point(3,43) });
        compactButton=new ModernButton { Text="Kompakt",Width=112,Height=36,Anchor=AnchorStyles.Top|AnchorStyles.Right };
        compactButton.Click+=delegate { SetCompact(!compact); };
        header.Controls.Add(compactButton);
        pinWindow.Text="Na wierzchu"; pinWindow.AutoSize=true; pinWindow.ForeColor=Theme.Muted;
        pinWindow.Anchor=AnchorStyles.Top|AnchorStyles.Right; pinWindow.CheckedChanged+=delegate { TopMost=pinWindow.Checked; };
        header.Controls.Add(pinWindow);
        header.Resize+=delegate { compactButton.Location=new Point(header.Width-116,0); pinWindow.Location=new Point(header.Width-116,42); };
        dashboard.Controls.Add(header,0,0);

        totals=new Label { Dock=DockStyle.Fill,ForeColor=Theme.Muted,TextAlign=ContentAlignment.MiddleLeft };
        dashboard.Controls.Add(totals,0,2);

        editor.Dock=DockStyle.Fill; editor.BackColor=Theme.Background; editor.WrapContents=false; editor.Margin=new Padding(0);
        editor.Controls.Add(MakeAction("+ Klawisz",delegate { AddAction(false); },true));
        editor.Controls.Add(MakeAction("+ Mysz",delegate { AddAction(true); },false));
        pauseButton.Text="+ Wstrzymaj"; pauseButton.Width=150; pauseButton.Height=40;
        pauseButton.Click+=delegate { if(running) return; using(var dialog=new ScheduledPauseDialog(null)) if(dialog.ShowDialog(this)==DialogResult.OK) { rules.Add(dialog.Result); RefreshRows(); Save(); RenderV2(); } };
        editor.Controls.Add(pauseButton); dashboard.Controls.Add(editor,0,3);

        listArea=new Panel { Dock=DockStyle.Fill,BackColor=Theme.Surface,Margin=new Padding(0) };
        grid.Dock=DockStyle.Fill; grid.BackgroundColor=Theme.Surface; grid.BorderStyle=BorderStyle.None;
        grid.AllowUserToAddRows=grid.AllowUserToDeleteRows=grid.AllowUserToResizeRows=false; grid.RowHeadersVisible=false;
        grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect=false; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText="Wł.",FillWeight=35 });
        foreach(string name in new string[] { "Akcja", "Harmonogram", "Następne", "Wykonano" }) grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText=name,ReadOnly=true });
        grid.Columns[1].FillWeight=100; grid.Columns[2].FillWeight=215; grid.Columns[3].FillWeight=100; grid.Columns[4].FillWeight=65; StyleGrid();
        grid.CurrentCellDirtyStateChanged+=delegate { if(grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellValueChanged+=delegate(object sender,DataGridViewCellEventArgs e) { if(!refreshingRows && e.RowIndex>=0 && e.ColumnIndex==0 && e.RowIndex<rules.Count) { rules[e.RowIndex].Enabled=Convert.ToBoolean(grid.Rows[e.RowIndex].Cells[0].Value); Save(); RenderV2(); } };
        grid.CellDoubleClick+=delegate(object sender,DataGridViewCellEventArgs e) { if(e.RowIndex>=0) EditSelected(); };
        grid.SelectionChanged+=delegate { RenderV2(); };
        listArea.Controls.Add(grid);
        empty=new Label { Text="Twój pierwszy harmonogram\n\nDodaj klawisz, kliknięcie myszy lub wstrzymanie.\nKażda akcja otrzyma własny czas wykonania.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Theme.Muted,BackColor=Theme.Surface };
        listArea.Controls.Add(empty); dashboard.Controls.Add(listArea,0,4);

        var bottom=new FlowLayoutPanel { Dock=DockStyle.Fill,BackColor=Theme.Background,WrapContents=false,Margin=new Padding(0),Padding=new Padding(0,10,0,0) };
        start.Text="Start  ·  F8"; start.Primary=true; start.Width=125; start.Height=40; start.Margin=new Padding(0,0,8,0); start.Click+=delegate { Toggle(); };
        bottom.Controls.Add(start);
        var stop=MakeAction("Stop  ·  F9",delegate { Stop("Zatrzymano. F8 rozpoczyna nową sesję."); },false); stop.Danger=true; stop.Width=120; bottom.Controls.Add(stop);
        editActions=new FlowLayoutPanel { Width=330,Height=42,WrapContents=false,Margin=new Padding(0),BackColor=Theme.Background };
        editButton=MakeAction("Edytuj",delegate { EditSelected(); },false); editButton.Width=88;
        duplicateButton=MakeAction("Duplikuj",delegate { DuplicateSelected(); },false); duplicateButton.Width=100;
        deleteButton=MakeAction("Usuń",delegate { if(running || grid.CurrentRow==null) return; rules.RemoveAt(grid.CurrentRow.Index); RefreshRows(); Save(); RenderV2(); },false); deleteButton.Width=88;
        editActions.Controls.AddRange(new Control[] { editButton,duplicateButton,deleteButton }); bottom.Controls.Add(editActions);
        dashboard.Controls.Add(bottom,0,5);
        var footer=new Panel { Dock=DockStyle.Fill,BackColor=Theme.Background };
        clock.Dock=DockStyle.Fill; clock.ForeColor=Theme.Muted; clock.Font=new Font("Segoe UI",9); clock.TextAlign=ContentAlignment.MiddleLeft;
        autoCompact.Text="Kompakt po starcie"; autoCompact.Checked=true; autoCompact.AutoSize=false; autoCompact.Width=180; autoCompact.Dock=DockStyle.Right; autoCompact.ForeColor=Theme.Muted;
        footer.Controls.Add(clock); footer.Controls.Add(autoCompact); dashboard.Controls.Add(footer,0,6);
        status.Dock=DockStyle.Fill; status.ForeColor=Theme.Muted; status.AutoEllipsis=true; status.Text=""; status.Font=new Font("Segoe UI",9); dashboard.Controls.Add(status,0,7);
        timer.Interval=250; timer.Tick+=delegate { Tick(); }; if(!previewOnly) timer.Start();
        if(!previewOnly) { LoadRules(); LoadPause(); }
        else { rules.Add(new Rule { Key="D1",Seconds=5,MaxDelayMs=250 }); rules.Add(new Rule { Key=MouseInput.Left,MouseX=840,MouseY=420,Seconds=30 }); rules.Add(new Rule { IsPause=true,Time="09:59:59",PauseMinutes=10 }); }
        RefreshRows(); RenderV2(); clock.Text="Zegar lokalny  ·  "+DateTime.Now.ToString("HH:mm:ss");
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { Stop("Zamykanie"); Save(); if(held.Count>0) e.Cancel=true; };
    }
    ModernButton MakeAction(string text,EventHandler action,bool primary) {
        var button=new ModernButton { Text=text,Width=135,Height=40,Primary=primary,Margin=new Padding(0,0,8,0) }; button.Click+=action; return button;
    }
    void AddAction(bool mouse) {
        if(running) return;
        var rule=new Rule { Key=mouse?MouseInput.Left:"Space" };
        using(var dialog=new EditRule(rule,ActionNames(),true)) {
            dialog.Text=mouse?"Nowe kliknięcie myszy":"Nowy klawisz";
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            rules.Add(rule); RefreshRows(); Save(); RenderV2();
        }
    }
    void EditSelected() {
        if(running || grid.CurrentRow==null) return;
        int index=grid.CurrentRow.Index; Rule rule=rules[index];
        if(rule.IsPause) { using(var dialog=new ScheduledPauseDialog(rule)) { if(dialog.ShowDialog(this)!=DialogResult.OK) return; rules[index]=dialog.Result; } }
        else using(var dialog=new EditRule(rule,ActionNames())) if(dialog.ShowDialog(this)!=DialogResult.OK) return;
        RefreshRows(); if(index<grid.Rows.Count) grid.CurrentCell=grid.Rows[index].Cells[1]; Save(); RenderV2();
    }
    public static Rule CopyRule(Rule r) {
        return new Rule { IsPause=r.IsPause,PauseByDuration=r.PauseByDuration,PauseMinutes=r.PauseMinutes,PauseEnd=r.PauseEnd,Enabled=r.Enabled,Key=r.Key,MouseX=r.MouseX,MouseY=r.MouseY,Mode=r.Mode,Seconds=r.Seconds,Time=r.Time,MaxDelayMs=r.MaxDelayMs,HoldMinMs=r.HoldMinMs,HoldMaxMs=r.HoldMaxMs };
    }
    void DuplicateSelected() {
        if(running || grid.CurrentRow==null) return;
        int index=grid.CurrentRow.Index+1; rules.Insert(index,CopyRule(rules[index-1])); RefreshRows(); grid.CurrentCell=grid.Rows[index].Cells[1]; Save(); RenderV2();
    }
    public void SetCompact(bool value) {
        if(compact==value) return;
        SuspendLayout();
        if(value) expandedSize=Size;
        compact=value; MinimumSize=value?new Size(480,250):new Size(680,620);
        totals.Visible=editor.Visible=listArea.Visible=editActions.Visible=autoCompact.Visible=!value;
        dashboard.RowStyles[2].Height=value?0:44; dashboard.RowStyles[3].Height=value?0:54;
        dashboard.RowStyles[4].SizeType=value?SizeType.Absolute:SizeType.Percent; dashboard.RowStyles[4].Height=value?0:100;
        compactButton.Text=value?"Pełny widok":"Kompakt";
        Size=value?new Size(480,280):expandedSize;
        ResumeLayout(true);
    }
    void RenderV2() {
        if(empty==null || editButton==null) return;
        int active=0; long count=0; foreach(var rule in rules) { if(rule.Enabled) active++; count+=rule.Count; }
        totals.Text=rules.Count+" akcji   /   "+active+" włączonych                                  "+count+" wykonań";
        empty.Visible=rules.Count==0; if(empty.Visible) empty.BringToFront();
        bool selected=grid.CurrentRow!=null;
        editButton.Enabled=duplicateButton.Enabled=deleteButton.Enabled=!running && selected;
        pauseButton.Enabled=!running;
    }
}
