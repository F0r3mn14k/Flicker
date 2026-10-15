using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

partial class ClickerForm {
    TableLayoutPanel dashboard;
    Panel listArea;
    FlowLayoutPanel editActions;
    Label totals, empty;
    ModernButton compactButton, editButton, duplicateButton, deleteButton, reorderButton, moveUpButton, moveDownButton;
    readonly CheckBox autoCompact=new CheckBox(), pinWindow=new CheckBox();
    bool compact;
    bool reordering;
    Point reorderDragStart;
    int reorderDragIndex=-1;
    Size expandedSize=new Size(760,720);

    void BuildV2() {
        Text="Flicker 4.0"; Size=expandedSize; MinimumSize=new Size(680,620);
        StartPosition=FormStartPosition.CenterScreen; Font=new Font("Segoe UI",10); BackColor=Theme.Background;
        dashboard=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=8,BackColor=Theme.Background };
        foreach(float height in new float[] { 72,44,44,54 }) dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,56)); dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,36)); dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
        Controls.Add(dashboard);

        var header=new Panel { Dock=DockStyle.Fill,BackColor=Theme.Background };
        header.Controls.Add(new Label { Text="Flicker",Font=new Font("Segoe UI",25,FontStyle.Bold),ForeColor=Theme.Ink,AutoSize=true,Location=new Point(0,0) });
        header.Controls.Add(new Label { Text="4.0  /  TWOJE AUTOMATYZACJE",Font=new Font("Segoe UI",8,FontStyle.Bold),ForeColor=Theme.Muted,AutoSize=true,Location=new Point(3,43) });
        compactButton=new ModernButton { Text="Kompakt",Width=112,Height=36,Anchor=AnchorStyles.Top|AnchorStyles.Right };
        compactButton.Click+=delegate { SetCompact(!compact); };
        header.Controls.Add(compactButton);
        pinWindow.Text="Na wierzchu"; pinWindow.AutoSize=true; pinWindow.ForeColor=Theme.Muted;
        pinWindow.Anchor=AnchorStyles.Top|AnchorStyles.Right; pinWindow.CheckedChanged+=delegate { TopMost=pinWindow.Checked; };
        header.Controls.Add(pinWindow);
        header.Resize+=delegate { compactButton.Location=new Point(header.Width-116,0); pinWindow.Location=new Point(header.Width-116,42); };
        dashboard.Controls.Add(header,0,0);

        var navigation=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false,Margin=new Padding(0) };
        var sequences=MakeAction("Sekwencje →",delegate { OpenSequence(); },false); sequences.Width=130; navigation.Controls.Add(sequences);
        profilePicker=new ComboBox { Width=260,Height=40,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,0,8,0),AccessibleName="Szybki wybór profilu" };
        profilePicker.SelectedIndexChanged+=delegate { if(!updatingProfilePicker) SelectProfile(profilePicker.SelectedItem as FlickerProfile); };
        navigation.Controls.Add(profilePicker);
        profilesButton=MakeAction("Profile…",delegate { ManageProfiles(); },false); profilesButton.Width=155; navigation.Controls.Add(profilesButton); Theme.Inputs(navigation); dashboard.Controls.Add(navigation,0,1);
        totals=new Label { Dock=DockStyle.Fill,ForeColor=Theme.Muted,TextAlign=ContentAlignment.MiddleLeft };
        dashboard.Controls.Add(totals,0,2);

        editor.Dock=DockStyle.Fill; editor.BackColor=Theme.Background; editor.WrapContents=false; editor.Margin=new Padding(0);
        var addKey=MakeAction("+ Klawisz",delegate { AddAction(false); },true); addKey.Width=100; editor.Controls.Add(addKey);
        var addMouse=MakeAction("+ Mysz",delegate { AddAction(true); },false); addMouse.Width=90; editor.Controls.Add(addMouse);
        pauseButton.Text="+ Wstrzymaj"; pauseButton.Width=115; pauseButton.Height=40;
        pauseButton.Click+=delegate { if(running) return; using(var dialog=new ScheduledPauseDialog(null)) if(dialog.ShowDialog(this)==DialogResult.OK) { rules.Add(dialog.Result); RefreshRows(); Save(); RenderV2(); } };
        editor.Controls.Add(pauseButton);
        reorderButton=MakeAction("Kolejność",delegate { SetReordering(!reordering); },false); reorderButton.Width=130;
        moveUpButton=MakeAction("↑ Góra",delegate { MoveSelectedRule(-1); },false); moveUpButton.Width=60;
        moveDownButton=MakeAction("↓ Dół",delegate { MoveSelectedRule(1); },false); moveDownButton.Width=60;
        editor.Controls.Add(reorderButton); editor.Controls.Add(moveUpButton); editor.Controls.Add(moveDownButton); dashboard.Controls.Add(editor,0,3);

        listArea=new Panel { Dock=DockStyle.Fill,BackColor=Theme.Surface,Margin=new Padding(0) };
        grid.Dock=DockStyle.Fill; grid.BackgroundColor=Theme.Surface; grid.BorderStyle=BorderStyle.None;
        grid.AllowUserToAddRows=grid.AllowUserToDeleteRows=grid.AllowUserToResizeRows=false; grid.RowHeadersVisible=false;
        grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect=false; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText="Wł.",FillWeight=35 });
        foreach(string name in new string[] { "Akcja", "Harmonogram", "Następne", "Wykonano" }) grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText=name,ReadOnly=true });
        grid.Columns[1].FillWeight=100; grid.Columns[2].FillWeight=215; grid.Columns[3].FillWeight=100; grid.Columns[4].FillWeight=65; StyleGrid();
        grid.CurrentCellDirtyStateChanged+=delegate { if(grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellValueChanged+=delegate(object sender,DataGridViewCellEventArgs e) { if(!refreshingRows && !reordering && e.RowIndex>=0 && e.ColumnIndex==0 && e.RowIndex<rules.Count) { rules[e.RowIndex].Enabled=Convert.ToBoolean(grid.Rows[e.RowIndex].Cells[0].Value); Save(); RenderV2(); } };
        grid.CellDoubleClick+=delegate(object sender,DataGridViewCellEventArgs e) { if(e.RowIndex>=0) EditSelected(); };
        grid.SelectionChanged+=delegate { RenderV2(); };
        grid.MouseDown+=delegate(object sender,MouseEventArgs e) {
            if(!reordering || e.Button!=MouseButtons.Left) return;
            var hit=grid.HitTest(e.X,e.Y);
            reorderDragIndex=hit.Type==DataGridViewHitTestType.Cell && hit.ColumnIndex>0 ? hit.RowIndex : -1;
            reorderDragStart=e.Location;
        };
        grid.MouseMove+=delegate(object sender,MouseEventArgs e) {
            if(!reordering || reorderDragIndex<0 || e.Button!=MouseButtons.Left) return;
            if(Math.Abs(e.X-reorderDragStart.X)<SystemInformation.DragSize.Width && Math.Abs(e.Y-reorderDragStart.Y)<SystemInformation.DragSize.Height) return;
            int index=reorderDragIndex; reorderDragIndex=-1; grid.DoDragDrop(index,DragDropEffects.Move);
        };
        grid.DragOver+=delegate(object sender,DragEventArgs e) { e.Effect=reordering && e.Data.GetDataPresent(typeof(int)) ? DragDropEffects.Move : DragDropEffects.None; };
        grid.DragDrop+=delegate(object sender,DragEventArgs e) {
            if(!reordering || !e.Data.GetDataPresent(typeof(int))) return;
            Point point=grid.PointToClient(new Point(e.X,e.Y)); int target=grid.HitTest(point.X,point.Y).RowIndex;
            if(target<0 && rules.Count>0) target=rules.Count-1;
            if(MoveRule(rules,(int)e.Data.GetData(typeof(int)),target)) { RefreshRows(); if(target<grid.Rows.Count) grid.CurrentCell=grid.Rows[target].Cells[1]; Save(); RenderV2(); }
        };
        listArea.Controls.Add(grid);
        empty=new Label { Text="Twój pierwszy harmonogram\n\nDodaj klawisz, kliknięcie myszy lub wstrzymanie.\nKażda akcja otrzyma własny czas wykonania.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Theme.Muted,BackColor=Theme.Surface };
        listArea.Controls.Add(empty); dashboard.Controls.Add(listArea,0,4);

        var bottom=new FlowLayoutPanel { Dock=DockStyle.Fill,BackColor=Theme.Background,WrapContents=false,Margin=new Padding(0),Padding=new Padding(0,10,0,0) };
        start.Text="Start  ·  F8"; start.Primary=true; start.Width=125; start.Height=40; start.Margin=new Padding(0,0,8,0); start.Click+=delegate { Toggle(); };
        bottom.Controls.Add(start);
        manualPauseButton.Text="Pauza"; manualPauseButton.Width=105; manualPauseButton.Height=40; manualPauseButton.Margin=new Padding(0,0,8,0); manualPauseButton.Click+=delegate { ToggleManualPause(); }; bottom.Controls.Add(manualPauseButton);
        var stop=MakeAction("Stop  ·  F9",delegate { Stop("Zatrzymano. F8 rozpoczyna nową sesję."); },false); stop.Danger=true; stop.Width=120; bottom.Controls.Add(stop);
        editActions=new FlowLayoutPanel { Width=245,Height=42,WrapContents=false,Margin=new Padding(0),BackColor=Theme.Background };
        editButton=MakeAction("Edytuj",delegate { EditSelected(); },false); editButton.Width=62;
        duplicateButton=MakeAction("Kopia",delegate { DuplicateSelected(); },false); duplicateButton.Width=70;
        deleteButton=MakeAction("Usuń",delegate { if(running || reordering || grid.CurrentRow==null) return; rules.RemoveAt(grid.CurrentRow.Index); RefreshRows(); Save(); RenderV2(); },false); deleteButton.Width=60;
        editActions.Controls.AddRange(new Control[] { editButton,duplicateButton,deleteButton }); bottom.Controls.Add(editActions);
        dashboard.Controls.Add(bottom,0,5);
        var footer=new Panel { Dock=DockStyle.Fill,BackColor=Theme.Background };
        clock.Dock=DockStyle.Fill; clock.ForeColor=Theme.Muted; clock.Font=new Font("Segoe UI",9); clock.TextAlign=ContentAlignment.MiddleLeft;
        autoCompact.Text="Kompakt po starcie"; autoCompact.Checked=true; autoCompact.AutoSize=false; autoCompact.Width=180; autoCompact.Dock=DockStyle.Right; autoCompact.ForeColor=Theme.Muted;
        footer.Controls.Add(clock); footer.Controls.Add(autoCompact); dashboard.Controls.Add(footer,0,6);
        status.Dock=DockStyle.Fill; status.ForeColor=Theme.Muted; status.AutoEllipsis=true; status.Text=""; status.Font=new Font("Segoe UI",9); dashboard.Controls.Add(status,0,7);
        timer.Interval=250; timer.Tick+=delegate { Tick(); }; if(!previewOnly) timer.Start();
        if(!previewOnly) { if(!File.Exists(profilesPath)) { LoadRules(); LoadPause(); } LoadProfiles(); }
        else { rules.Add(new Rule { Key="D1",Seconds=5,MaxDelayMs=250 }); rules.Add(new Rule { Key=MouseInput.Left,MouseX=840,MouseY=420,Seconds=30 }); rules.Add(new Rule { IsPause=true,Time="09:59:59",PauseMinutes=10 }); var previewProfile=new FlickerProfile { Name="Domyślny",Pinned=true }; foreach(var rule in rules) previewProfile.Rules.Add(CopyRule(rule)); library=new ProfileLibrary { ActiveId=previewProfile.Id }; library.Profiles.Add(previewProfile); UpdateProfileLabel(); }
        RefreshRows(); RenderV2(); clock.Text="Zegar lokalny  ·  "+DateTime.Now.ToString("HH:mm:ss");
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { Stop("Zamykanie"); Save(); if(held.Count>0) e.Cancel=true; };
    }
    ModernButton MakeAction(string text,EventHandler action,bool primary) {
        var button=new ModernButton { Text=text,Width=135,Height=40,Primary=primary,Margin=new Padding(0,0,8,0) }; button.Click+=action; return button;
    }
    void AddAction(bool mouse) {
        if(running || reordering) return;
        var rule=new Rule { Key=mouse?MouseInput.Left:"Space" };
        using(var dialog=new EditRule(rule,ActionNames(),true)) {
            dialog.Text=mouse?"Nowe kliknięcie myszy":"Nowy klawisz";
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            rules.Add(rule); RefreshRows(); Save(); RenderV2();
        }
    }
    void EditSelected() {
        if(running || reordering || grid.CurrentRow==null) return;
        int index=grid.CurrentRow.Index; Rule rule=rules[index];
        if(rule.IsPause) { using(var dialog=new ScheduledPauseDialog(rule)) { if(dialog.ShowDialog(this)!=DialogResult.OK) return; rules[index]=dialog.Result; } }
        else using(var dialog=new EditRule(rule,ActionNames())) if(dialog.ShowDialog(this)!=DialogResult.OK) return;
        RefreshRows(); if(index<grid.Rows.Count) grid.CurrentCell=grid.Rows[index].Cells[1]; Save(); RenderV2();
    }
    public static Rule CopyRule(Rule r) {
        return new Rule { IsPause=r.IsPause,PauseByDuration=r.PauseByDuration,PauseMinutes=r.PauseMinutes,PauseEnd=r.PauseEnd,Enabled=r.Enabled,Key=r.Key,MouseX=r.MouseX,MouseY=r.MouseY,Mode=r.Mode,Seconds=r.Seconds,Time=r.Time,MaxDelayMs=r.MaxDelayMs,HoldMinMs=r.HoldMinMs,HoldMaxMs=r.HoldMaxMs,AllowDuringPause=r.AllowDuringPause };
    }
    void DuplicateSelected() {
        if(running || reordering || grid.CurrentRow==null) return;
        int index=grid.CurrentRow.Index+1; rules.Insert(index,CopyRule(rules[index-1])); RefreshRows(); grid.CurrentCell=grid.Rows[index].Cells[1]; Save(); RenderV2();
    }
    void SetReordering(bool value) {
        if(running || reordering==value) return;
        reordering=value; reorderDragIndex=-1; grid.AllowDrop=value; grid.ReadOnly=value; grid.Cursor=value?Cursors.SizeAll:Cursors.Default;
        status.Text=value?"Tryb zmiany kolejności: przeciągnij wiersz albo użyj W górę / W dół. Kliknij „Gotowe”, aby wrócić do edycji.":"Kolejność zapisana.";
        RenderV2();
    }
    public static bool MoveRule(List<Rule> source,int from,int to) {
        if(source==null || from<0 || to<0 || from>=source.Count || to>=source.Count || from==to) return false;
        Rule rule=source[from]; source.RemoveAt(from); source.Insert(to,rule); return true;
    }
    void MoveSelectedRule(int delta) {
        if(!reordering || grid.CurrentRow==null) return;
        int from=grid.CurrentRow.Index, to=from+delta;
        if(!MoveRule(rules,from,to)) return;
        RefreshRows(); grid.CurrentCell=grid.Rows[to].Cells[1]; Save(); RenderV2();
    }
    public void SetCompact(bool value) {
        if(compact==value) return;
        SuspendLayout();
        if(value) expandedSize=Size;
        compact=value; MinimumSize=value?new Size(480,250):new Size(680,620);
        totals.Visible=editor.Visible=listArea.Visible=editActions.Visible=autoCompact.Visible=!value;
        dashboard.GetControlFromPosition(0,1).Visible=!value; dashboard.RowStyles[1].Height=value?0:44;
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
        editButton.Enabled=duplicateButton.Enabled=deleteButton.Enabled=!running && !reordering && selected;
        foreach(Control control in editor.Controls) if(control!=reorderButton && control!=moveUpButton && control!=moveDownButton) control.Enabled=!running && !reordering;
        if(profilesButton!=null) profilesButton.Enabled=!running && !reordering;
        reorderButton.Enabled=!running; reorderButton.Text=reordering?"Gotowe":"Kolejność"; reorderButton.Primary=reordering;
        moveUpButton.Enabled=moveDownButton.Enabled=!running && reordering && selected;
        manualPauseButton.Enabled=running; manualPauseButton.Primary=manualPaused;
        manualPauseButton.Text=manualPaused?(manualHotkeyReady?"Wznów · F7":"Wznów"):manualHotkeyReady?"Pauza · F7":"Pauza";
    }
}
