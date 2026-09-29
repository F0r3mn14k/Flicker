using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

static class Theme {
    public static readonly Color Background = Color.FromArgb(17, 23, 30);
    public static readonly Color Surface = Color.FromArgb(26, 35, 45);
    public static readonly Color Input = Color.FromArgb(34, 45, 57);
    public static readonly Color Ink = Color.FromArgb(234, 243, 247);
    public static readonly Color Muted = Color.FromArgb(164, 182, 194);
    public static readonly Color Accent = Color.FromArgb(69, 224, 199);
    public static readonly Color Border = Color.FromArgb(58, 75, 88);
    public static GraphicsPath Round(Rectangle rect, int radius) {
        var path = new GraphicsPath(); int d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right-d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right-d, rect.Bottom-d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom-d, d, d, 90, 90); path.CloseFigure(); return path;
    }
    public static void Inputs(Control root) {
        foreach(Control c in root.Controls) {
            if(c is NumericUpDown) { ((NumericUpDown)c).BorderStyle = BorderStyle.FixedSingle; c.BackColor = Input; c.ForeColor = Ink; }
            if(c is ComboBox) {
                var combo=(ComboBox)c; combo.FlatStyle=FlatStyle.Flat; c.BackColor=Input; c.ForeColor=Ink;
                if(combo.DrawMode!=DrawMode.OwnerDrawFixed) {
                    combo.DrawMode=DrawMode.OwnerDrawFixed;
                    combo.DrawItem+=delegate(object sender,DrawItemEventArgs e) {
                        var box=(ComboBox)sender; bool selected=(e.State & DrawItemState.Selected)!=0;
                        Color background=selected?Color.FromArgb(32,77,77):Input;
                        using(var brush=new SolidBrush(background)) e.Graphics.FillRectangle(brush,e.Bounds);
                        string text=e.Index>=0?box.GetItemText(box.Items[e.Index]):box.Text;
                        TextRenderer.DrawText(e.Graphics,text,box.Font,e.Bounds,box.Enabled?Ink:Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);
                        e.DrawFocusRectangle();
                    };
                }
            }
            if(c is Label) { c.ForeColor = Muted; c.BackColor = Color.Transparent; }
            if(c is CheckBox) { c.ForeColor=Ink; c.BackColor=Color.Transparent; }
            Inputs(c);
        }
    }
}

class GradientForm : Form {
    public GradientForm() {
        DoubleBuffered = true; ResizeRedraw = true; ForeColor = Theme.Ink; BackColor=Theme.Background;
        using(var stream = typeof(GradientForm).Assembly.GetManifestResourceStream("Flicker.Icon")) {
            if(stream != null) using(var icon = new Icon(stream)) Icon = (Icon)icon.Clone();
        }
    }
    protected override void OnPaintBackground(PaintEventArgs e) {
        if(ClientSize.Width == 0 || ClientSize.Height == 0) return;
        e.Graphics.Clear(Theme.Background);
    }
}

class ModernButton : Button {
    // Use the standard Windows Forms renderer: opaque white, straight edges.
    bool primary,danger;
    public bool Primary { get { return primary; } set { primary=value; ApplyColors(); } }
    public bool Danger { get { return danger; } set { danger=value; ApplyColors(); } }
    void ApplyColors() {
        BackColor=primary?Theme.Accent:Theme.Input;
        ForeColor=primary?Theme.Background:danger?Color.FromArgb(255,165,160):Theme.Ink;
        FlatAppearance.BorderColor=primary?Theme.Accent:Theme.Border;
        FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(105,239,216):Color.FromArgb(47,63,76);
        FlatAppearance.MouseDownBackColor=primary?Color.FromArgb(41,185,166):Theme.Surface;
    }
    public ModernButton() {
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        BackColor = Theme.Input;
        ForeColor = Theme.Ink;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = Color.FromArgb(155, 169, 162);
        FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 247, 246);
        FlatAppearance.MouseDownBackColor = Color.FromArgb(231, 236, 233);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10, FontStyle.Bold);
        Height = 42;
        ApplyColors();
    }
    protected override void OnPaint(PaintEventArgs e) {
        if(Enabled) { base.OnPaint(e); return; }
        e.Graphics.Clear(Theme.Surface);
        using(var pen=new Pen(Theme.Border)) e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }
}
class Card : Panel {
    public Card() { DoubleBuffered=true; BackColor=Theme.Surface; Padding=new Padding(16); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using(var pen=new Pen(Theme.Border)) e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1); }
}
