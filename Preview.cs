using System;
using System.Drawing;
using System.Windows.Forms;

// Offline rendering only: no message loop, key simulation or saved settings.
static class Preview {
    static void Prepare(Control control) { typeof(Control).GetMethod("SetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(control, new object[] { 2, true }); var handle = control.Handle; foreach(Control child in control.Controls) Prepare(child); control.PerformLayout(); }
    [STAThread] static void Main() {
        MouseInput.EnableDpiAwareness();
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using(var form = new ClickerForm(true)) {
            Prepare(form); form.PerformLayout();
            using(var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save("preview.png");
            }
            form.Size = form.MinimumSize; form.PerformLayout();
            using(var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save("preview-min.png");
            }
        }
        using(var form = new EditRule(new Rule(), ClickerForm.KeyNames())) {
            Prepare(form); form.PerformLayout();
            using(var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save("preview-edit.png");
            }
        }
        using(var form = new ScheduledPauseDialog(null)) {
            Prepare(form); form.PerformLayout();
            using(var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save("preview-pause.png");
            }
        }
        using(var form = new MousePointDialog(new Point(500,300))) {
            Prepare(form); form.PerformLayout();
            using(var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save("preview-mouse.png");
            }
        }
    }
}
