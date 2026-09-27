using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

static class Theme {
    public static readonly Color Ink = Color.FromArgb(24, 49, 48);
    public static readonly Color Muted = Color.FromArgb(78, 101, 97);
    public static readonly Color Accent = Color.FromArgb(0, 116, 103);
    public static GraphicsPath Round(Rectangle rect, int radius) {
        var path = new GraphicsPath(); int d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right-d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right-d, rect.Bottom-d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom-d, d, d, 90, 90); path.CloseFigure(); return path;
    }
    public static void Inputs(Control root) {
        foreach(Control c in root.Controls) {
            if(c is NumericUpDown) { ((NumericUpDown)c).BorderStyle = BorderStyle.FixedSingle; c.BackColor = Color.FromArgb(247,250,247); c.ForeColor = Ink; }
            if(c is ComboBox) { ((ComboBox)c).FlatStyle = FlatStyle.Flat; c.BackColor = Color.FromArgb(236,245,240); c.ForeColor = Ink; }
            if(c is Label) { c.ForeColor = Muted; c.BackColor = Color.Transparent; }
            Inputs(c);
        }
    }
}

class GradientForm : Form {
    public GradientForm() {
        DoubleBuffered = true; ResizeRedraw = true; ForeColor = Theme.Ink;
        using(var stream = typeof(GradientForm).Assembly.GetManifestResourceStream("Flicker.Icon")) {
            if(stream != null) using(var icon = new Icon(stream)) Icon = (Icon)icon.Clone();
        }
    }
    protected override void OnPaintBackground(PaintEventArgs e) {
        if(ClientSize.Width == 0 || ClientSize.Height == 0) return;
        using(var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(255,245,225), Color.FromArgb(220,245,233), 65f)) e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}

class ModernButton : Button {
    // Use the standard Windows Forms renderer: opaque white, straight edges.
    public bool Primary { get; set; }
    public bool Danger { get; set; }
    public ModernButton() {
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        BackColor = Color.White;
        ForeColor = Theme.Ink;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = Color.FromArgb(155, 169, 162);
        FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 247, 246);
        FlatAppearance.MouseDownBackColor = Color.FromArgb(231, 236, 233);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10, FontStyle.Bold);
        Height = 42;
    }
}
class Card : Panel {
    public Card() { DoubleBuffered=true; BackColor=Color.White; Padding=new Padding(16); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using(var pen=new Pen(Color.FromArgb(199,216,202))) e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1); }
}
