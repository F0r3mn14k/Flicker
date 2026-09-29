using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// The numeric field remains authoritative: synchronizing the thumb never rounds typed values.
class NumericSlider : TrackBar {
    readonly NumericUpDown number;
    readonly ToolTip hint=new ToolTip();
    bool syncing;
    public NumericSlider(NumericUpDown number) {
        this.number=number;
        Minimum=0; Maximum=1000; SmallChange=1; LargeChange=50;
        AutoSize=false; Height=30; TickStyle=TickStyle.None; BackColor=Theme.Background;
        AccessibleName=string.IsNullOrEmpty(number.AccessibleName)?"Suwak wartości":number.AccessibleName;
        number.ValueChanged+=NumberChanged; number.EnabledChanged+=NumberChanged;
        ValueChanged+=delegate {
            if(syncing) return;
            number.Value=FromPosition(Value,number.Minimum,number.Maximum,number.DecimalPlaces);
            Sync();
        };
        Sync();
    }
    public static decimal FromPosition(int position,decimal min,decimal max,int decimals) {
        if(position<=0 || max<=min) return min;
        if(position>=1000) return max;
        double range=(double)(max-min), fraction=position/1000.0;
        double offset=range>1000?Math.Exp(fraction*Math.Log(1+range))-1:fraction*range;
        return Math.Max(min,Math.Min(max,Math.Round(min+(decimal)offset,decimals,MidpointRounding.AwayFromZero)));
    }
    public static int ToPosition(decimal value,decimal min,decimal max) {
        if(max<=min) return 0;
        double range=(double)(max-min), offset=(double)(Math.Max(min,Math.Min(max,value))-min);
        double fraction=range>1000?Math.Log(1+offset)/Math.Log(1+range):offset/range;
        return Math.Max(0,Math.Min(1000,(int)Math.Round(fraction*1000)));
    }
    void NumberChanged(object sender,EventArgs e) { Sync(); }
    protected override void OnKeyDown(KeyEventArgs e) {
        decimal value=number.Value;
        switch(e.KeyCode) {
            case Keys.Left: case Keys.Down: value-=number.Increment; break;
            case Keys.Right: case Keys.Up: value+=number.Increment; break;
            case Keys.PageDown: value-=number.Increment*10; break;
            case Keys.PageUp: value+=number.Increment*10; break;
            case Keys.Home: value=number.Minimum; break;
            case Keys.End: value=number.Maximum; break;
            default: base.OnKeyDown(e); return;
        }
        number.Value=Math.Max(number.Minimum,Math.Min(number.Maximum,value));
        e.Handled=true; e.SuppressKeyPress=true;
    }
    void Sync() {
        syncing=true;
        try { Value=ToPosition(number.Value,number.Minimum,number.Maximum); Enabled=number.Enabled; }
        finally { syncing=false; }
        hint.SetToolTip(this,number.Value+"  ("+number.Minimum+" – "+number.Maximum+"). "+(number.Maximum-number.Minimum>1000?"Skala nieliniowa — dokładniejsza dla małych wartości. ":"")+"Dokładną wartość możesz wpisać w polu.");
    }
    public static void AddToDialog(Form form) {
        var fields=new List<NumericUpDown>();
        foreach(Control control in form.Controls) if(control is NumericUpDown) fields.Add((NumericUpDown)control);
        fields.Sort(delegate(NumericUpDown a,NumericUpDown b) { return b.Top.CompareTo(a.Top); });
        foreach(var field in fields) {
            int threshold=field.Bottom;
            foreach(Control control in form.Controls) if(control.Top>=threshold) control.Top+=34;
            foreach(Control control in form.Controls) if(control is Label && Math.Abs(control.Top-field.Top)<10) { field.AccessibleName=control.Text; break; }
            form.Controls.Add(new NumericSlider(field) { Left=field.Left,Top=threshold+1,Width=field.Width });
            form.ClientSize=new Size(form.ClientSize.Width,form.ClientSize.Height+34);
        }
    }
    protected override void Dispose(bool disposing) { if(disposing) { number.ValueChanged-=NumberChanged; number.EnabledChanged-=NumberChanged; hint.Dispose(); } base.Dispose(disposing); }
}
