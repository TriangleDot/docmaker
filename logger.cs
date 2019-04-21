using System;
using System.IO;

namespace Log
{
    enum LogFlags {StdoutOnly, FileOnly, FileAndStdout};
    class Logger { // Basic logging class
        public String filename = "log.txt"; // Filename for the log

        
        private LogFlags flags = LogFlags.StdoutOnly;
        private String name = "Logger";

        public Logger(String name, LogFlags flag = LogFlags.StdoutOnly, String filename="log.txt") 
        {
            this.flags = flag;
            this.filename = filename;
            this.name = name;
        }
        public void info(String output) 
        {
            this.write("["+name+": "+DateTime.Now.ToShortTimeString()+" - Info]: "+output, ConsoleColor.Blue);
        }

        public void warning(String output) 
        {
            this.write("["+name+": "+DateTime.Now.ToShortTimeString()+" - Warning]: "+output, ConsoleColor.Yellow);
        }
        public void error(String output) 
        {
            this.write("["+name+": "+DateTime.Now.ToShortTimeString()+" - Error]: "+output, ConsoleColor.Red);
        }
        private void write(String output, Nullable<ConsoleColor> color = null) 
        {
            if (this.flags == LogFlags.FileAndStdout || this.flags == LogFlags.StdoutOnly)
            {
                if (color != null) {
                    Console.ForegroundColor = (ConsoleColor)color;
                }
                Console.WriteLine(output);
                Console.ResetColor();
            }
            if (this.flags == LogFlags.FileAndStdout || this.flags == LogFlags.FileOnly)
            {
                using (StreamWriter sw = File.AppendText(this.filename)) 
                {
                    sw.WriteLine(output);
                    
                }	

            }
        }
    }
}