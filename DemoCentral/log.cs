
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;


    public class Log
    {
        private static object LogLock = new object();
        private static string logPath = (@"\log.txt");

        public static void WriteLine(string msg)
        {
            // Locking the body of this method and thus preventing multiple threads accessing it simultaneously
            // see https://stackoverflow.com/questions/13901048/how-to-lock-an-asp-net-mvc-action
            lock (LogLock)
            {
                using (StreamWriter w = new StreamWriter(logPath, true))
                {
                    w.WriteLine();
                    WriteToLog(msg, w);
                }
            }
        }

        public static void LogException(Exception e)
        {
            WriteLine(e.ToString());
        }

        private static void WriteToLog(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }
    }
