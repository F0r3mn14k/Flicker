using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

partial class ClickerForm {
    ProfileLibrary library;
    ModernButton profilesButton;
    readonly string profilesPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlickerV4","profiles.xml");
    void LoadProfiles() {
        if(previewOnly) return;
        try {
            if(File.Exists(profilesPath)) { var loaded=ProfileFiles.Read<ProfileLibrary>(profilesPath); loaded.Validate(); library=loaded; rules.Clear(); rules.AddRange(library.Active.Rules); loadFailed=false; }
            else {
                if(loadFailed) return;
                var p=new FlickerProfile(); foreach(var r in rules) p.Rules.Add(CopyRule(r));
                string old=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlickerV3","sequence.xml");
                if(File.Exists(old)) { p.Sequence=ProfileFiles.Read<SequenceDocument>(old); p.Sequence.Validate(); }
                p.Validate(); library=new ProfileLibrary { ActiveId=p.Id }; library.Profiles.Add(p); SaveProfiles();
            }
            UpdateProfileLabel();
        } catch(Exception ex) { loadFailed=true; status.Text="Nie udało się odczytać profili: "+ex.Message; }
    }
    bool SaveProfiles() {
        if(previewOnly || loadFailed || library==null) return false;
        try { library.Active.Rules.Clear(); foreach(var r in rules) library.Active.Rules.Add(CopyRule(r)); library.Validate(); ProfileFiles.Write(profilesPath,library); return true; }
        catch(Exception ex) { status.Text="Nie udało się zapisać profilu: "+ex.Message; return false; }
    }
    void UpdateProfileLabel() { if(profilesButton!=null) profilesButton.Text="Profile: "+(library==null?"Domyślny":library.Active.Name); }
    void ManageProfiles() {
        if(running || held.Count>0) { status.Text="Zatrzymaj działanie przed zmianą profilu."; return; }
        if(!Save()) return;
        using(var dialog=new ProfilesDialog(library)) {
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            try {
                var next=dialog.Result; next.Validate(); ProfileFiles.Write(profilesPath,next);
                library=next; rules.Clear(); foreach(var r in library.Active.Rules) rules.Add(CopyRule(r));
                RefreshRows(); RenderV2(); UpdateProfileLabel(); status.Text="Wybrano profil: "+library.Active.Name;
            } catch(Exception ex) { status.Text="Nie udało się zastosować profili: "+ex.Message; }
        }
    }
}

class ProfilesDialog : GradientForm {
    public ProfileLibrary Result { get; private set; }
    readonly ListBox list=new ListBox();
    readonly Label details=new Label();
    public ProfilesDialog(ProfileLibrary source) {
        Result=source.Copy(); Text="Flicker 4.0 — Profile"; Size=new Size(720,550); MinimumSize=new Size(660,480); Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterParent;
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); Controls.Add(layout);
        layout.Controls.Add(new Label { Text="Profile\nKażdy profil zawiera harmonogram i sekwencję.",Dock=DockStyle.Fill,ForeColor=Theme.Ink },0,0);
        list.Dock=DockStyle.Fill; list.BackColor=Theme.Surface; list.ForeColor=Theme.Ink; list.BorderStyle=BorderStyle.FixedSingle; list.IntegralHeight=false; list.SelectedIndexChanged+=delegate { Describe(); }; layout.Controls.Add(list,0,1);
        details.Dock=DockStyle.Fill; details.ForeColor=Theme.Muted; layout.Controls.Add(details,0,2);
        var actions=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
        actions.Controls.Add(Button("Nowy",delegate { NameEdit(false,false); })); actions.Controls.Add(Button("Kopia",delegate { NameEdit(true,false); })); actions.Controls.Add(Button("Nazwa",delegate { NameEdit(false,true); }));
        actions.Controls.Add(Button("Usuń",delegate { var p=Selected; if(p==null || Result.Profiles.Count==1) return; if(MessageBox.Show(this,"Usunąć profil „"+p.Name+"”?","Usuwanie profilu",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return; Result.Profiles.Remove(p); if(Result.ActiveId==p.Id) Result.ActiveId=Result.Profiles[0].Id; RefreshList(Result.ActiveId); }));
        actions.Controls.Add(Button("Importuj",delegate { Import(); })); actions.Controls.Add(Button("Eksportuj",delegate { Export(); })); layout.Controls.Add(actions,0,3);
        var footer=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
        var apply=Button("Zastosuj profil",delegate { if(Selected==null) return; Result.ActiveId=Selected.Id; Result.Validate(); DialogResult=DialogResult.OK; }); apply.Width=160; apply.Primary=true; footer.Controls.Add(apply);
        var cancel=Button("Anuluj",delegate { DialogResult=DialogResult.Cancel; }); footer.Controls.Add(cancel); CancelButton=cancel; layout.Controls.Add(footer,0,4);
        RefreshList(Result.ActiveId);
    }
    FlickerProfile Selected { get { return list.SelectedItem as FlickerProfile; } }
    ModernButton Button(string label,EventHandler action) { var b=new ModernButton { Text=label,Width=92,Height=36,Margin=new Padding(0,5,6,0) }; b.Click+=action; return b; }
    void RefreshList(string id) { list.Items.Clear(); foreach(var p in Result.Profiles) { list.Items.Add(p); if(p.Id==id) list.SelectedItem=p; } Describe(); }
    void Describe() { var p=Selected; details.Text=p==null?"":p.Rules.Count+" akcji · "+p.Sequence.Steps.Count+" kroków · "+p.Sequence.Repeats+" powtórzeń\nZastosuj profil zapisuje zmiany. Anuluj je odrzuca."; }
    void NameEdit(bool duplicate,bool rename) {
        var selected=Selected; if((duplicate || rename) && selected==null) return;
        using(var dialog=new ProfileNameDialog(rename?selected.Name:Result.UniqueName(duplicate?selected.Name+" — kopia":"Nowy profil"))) {
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            string name=dialog.ProfileName;
            if(Result.Profiles.Exists(delegate(FlickerProfile p) { return (!rename || p!=selected) && string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase); })) { MessageBox.Show(this,"Profil o tej nazwie już istnieje."); return; }
            if(rename) { selected.Name=name; RefreshList(selected.Id); }
            else { if(Result.Profiles.Count>=1000) { MessageBox.Show(this,"Maksymalnie 1000 profili."); return; } var p=duplicate?selected.Copy():new FlickerProfile(); p.Id=Guid.NewGuid().ToString("N"); p.Name=name; Result.Profiles.Add(p); RefreshList(p.Id); }
        }
    }
    void Import() {
        using(var dialog=new OpenFileDialog { Title="Importuj profil Flickera",Filter="Profil Flickera (*.flicker.xml)|*.flicker.xml|Plik XML (*.xml)|*.xml",CheckFileExists=true }) {
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            try { var imported=Result.Import(ProfileFiles.Read<FlickerProfile>(dialog.FileName)); RefreshList(imported.Id); details.Text="Zaimportowano: "+imported.Name+". Kliknij Zastosuj profil, aby zapisać."; }
            catch(Exception ex) { MessageBox.Show(this,"Nie udało się importować profilu.\n"+ex.Message,"Import",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }
    }
    void Export() {
        var p=Selected; if(p==null) return;
        using(var dialog=new SaveFileDialog { Title="Eksportuj profil Flickera",Filter="Profil Flickera (*.flicker.xml)|*.flicker.xml",FileName="profil.flicker.xml",DefaultExt="flicker.xml",AddExtension=true,OverwritePrompt=true }) {
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            try { p.Validate(); ProfileFiles.Write(dialog.FileName,p.Copy()); details.Text="Wyeksportowano profil „"+p.Name+"” do pliku."; }
            catch(Exception ex) { MessageBox.Show(this,"Nie udało się eksportować profilu.\n"+ex.Message); }
        }
    }
}
class ProfileNameDialog : GradientForm {
    readonly TextBox name=new TextBox();
    public string ProfileName { get { return name.Text.Trim(); } }
    public ProfileNameDialog(string initial) {
        Text="Nazwa profilu"; ClientSize=new Size(410,135); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=MinimizeBox=false; StartPosition=FormStartPosition.CenterParent; Font=new Font("Segoe UI",10);
        name.SetBounds(20,20,370,30); name.Text=initial; name.MaxLength=80; name.BackColor=Theme.Input; name.ForeColor=Theme.Ink; Controls.Add(name);
        var ok=new ModernButton { Text="Zapisz",Left=155,Top=75,Width=110,Height=36,Primary=true }; ok.Click+=delegate { if(ProfileName.Length==0) return; DialogResult=DialogResult.OK; }; Controls.Add(ok);
        var cancel=new ModernButton { Text="Anuluj",Left=280,Top=75,Width=110,Height=36,DialogResult=DialogResult.Cancel }; Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
    }
}
