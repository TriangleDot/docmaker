using System;
using Gtk;
using NonJankExporter;
using System.Collections.Generic;
using Mono.Options;

namespace docmaker
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // these variables will be set when the command line is parsed
            String exportpath = null;
            String filepath = null;
            String template = "default";
            String title = "";
            var shouldShowHelp = false;

            // thses are the available options, not that they set the variables
            var options = new OptionSet { 
                { "e|export=", "Path to export HTML to", e => exportpath=e },
                { "t|template=", "Template that will be used for the export", t => template=t} ,
                { "i|title=", "Title that will be used for the export", t => title=t} ,
                { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
            };

            List<string> extra;
            try {
                // parse the command line
                extra = options.Parse (args);
            } catch (OptionException e) {
                // output some error message
                Console.Write ("Error: ");
                Console.WriteLine (e.Message);
                Console.WriteLine ("Try 'docmaker --help' for more information.");
                return;
            }

            if (shouldShowHelp) {
                // show some app description message
                Console.WriteLine("Docmaker by Triangledot");
                Console.WriteLine ();

                // output the options
                Console.WriteLine ("Options:");
                options.WriteOptionDescriptions (Console.Out);
                return;
            }

            if (extra.Count > 0) {
                Console.WriteLine($"Loading file {extra[0]}");
                filepath = extra[0];
            }

            if (exportpath == null) {
                Application.Init();

                var app = new Application("org.triangledot.docmaker", GLib.ApplicationFlags.None);
                app.Register(GLib.Cancellable.Current);

                using (var win = new MainWindow())
                {
                    app.AddWindow(win);
                    if (filepath != null) {
                        win.openDir(filepath);
                    }

                    win.Show();
                }
                Application.Run();
            }
            else {
                new NoJankExport(filepath,exportpath,template, title).export();
            }
        }
    }
}
