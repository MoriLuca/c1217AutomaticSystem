using Runner.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runner
{
    class Program
    {

        #region Object Declaration
        static Luca.Logger _log = new Luca.Logger(@"\GiDi_Runner\Main\");
        #endregion

        #region Exit Handler
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                    _log.WriteLog("The User Logged Off;");
                    return false;
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    _log.WriteLog("PC Shutdown.;");
                    return false;
                case CtrlType.CTRL_CLOSE_EVENT:
                    _log.WriteLog("Application was closed;");
                    return false;
                default:
                    return false;
            }
        }
        #endregion

        static void Main(string[] args)
        {
            #region Application Exit Handler
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            #endregion

            #region Console Title
            Console.Title = $"Runner V[{Assembly.GetExecutingAssembly().GetName().Version}]";
            Console.WriteLine("-- Inizio Programma Runner--\n");
            #endregion

            #region DatabaseCheck
            try
            {
                using (var dbContext = new Classes.ProduzioneEntities())
                {
                    if (dbContext.Database.Exists())
                    {
                        Console.WriteLine("Connessione db OK");
                    }
                    else
                    {
                        string message = "Connessione db Non presente.\n";
                        message += "Please build the database for the application from the Microsoft SQL Server Managment\n" +
                           "or check where the SQL Server Service is ON.";
                        Console.WriteLine(message);
                        _log.WriteLog(message);
                        Console.ReadLine();
                        Environment.Exit(0);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = "Error while checking for DB Presence : " + ex.Message;
                Console.WriteLine(message);
                _log.WriteLog(message);
            }
            #endregion

            #region PLC Instance
            //The constructor will launch all the separate threads
            PLCWorker plc = new PLCWorker();
            #endregion

            while (true)
            {
                Thread.Sleep(2000);
            }
        }

    }
}
