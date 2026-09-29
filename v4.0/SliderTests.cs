using System;
using System.Windows.Forms;

static class SliderTests {
    static void Check(bool ok,string label) { if(!ok) throw new Exception("Slider: "+label); }
    public static void Run() {
        foreach(decimal max in new decimal[] { 1440,5000,60000,86400 }) {
            Check(NumericSlider.FromPosition(0,.05m,max,2)==.05m && NumericSlider.FromPosition(1000,.05m,max,2)==max,"exact endpoints");
            decimal previous=.05m;
            for(int i=0;i<=1000;i++) { decimal value=NumericSlider.FromPosition(i,.05m,max,2); Check(value>=previous && value<=max,"monotonic bounded mapping"); previous=value; }
        }
        using(var number=new NumericUpDown { Minimum=.05m,Maximum=86400,DecimalPlaces=2,Value=123.45m })
        using(var slider=new NumericSlider(number)) {
            Check(number.Value==123.45m,"creation preserves typed value");
            number.Value=987.65m; Check(number.Value==987.65m && slider.Value==NumericSlider.ToPosition(987.65m,.05m,86400),"typed precision preserved");
            slider.Value=0; Check(number.Value==.05m,"slider updates numeric minimum"); slider.Value=1000; Check(number.Value==86400,"slider updates numeric maximum");
            number.Enabled=false; Check(!slider.Enabled,"disabled mode propagated");
        }
        using(var low=new NumericUpDown { Minimum=1,Maximum=5000,Value=50 })
        using(var high=new NumericUpDown { Minimum=1,Maximum=5000,Value=100 }) {
            low.ValueChanged+=delegate { if(high.Value<low.Value) high.Value=low.Value; };
            high.ValueChanged+=delegate { if(low.Value>high.Value) low.Value=high.Value; };
            using(var a=new NumericSlider(low)) using(var b=new NumericSlider(high)) {
                a.Value=1000; Check(high.Value==5000 && b.Value==1000,"range lower adjusts upper");
                b.Value=0; Check(low.Value==1 && a.Value==0,"range upper adjusts lower");
            }
        }
        Console.WriteLine("PASS: slider precision, bounds, nonlinear mapping, disabled states and linked ranges.");
    }
}
