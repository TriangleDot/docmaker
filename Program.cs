using System;
using Gtk;

namespace docmaker
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            var app = new Application("org.triangledot.docmaker", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            using (var win = new MainWindow())
            {
                app.AddWindow(win);

                win.Show();
            }
            Application.Run();
        }
    }
}
