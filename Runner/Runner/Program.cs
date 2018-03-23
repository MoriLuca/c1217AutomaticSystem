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
        static Classes.PLCWorker _plc = new Classes.PLCWorker();
        #warning rimuovere il plc e le chimate di aggiornamento da qua
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
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            //#region Console Title
            //Console.Title = $"Runner V[{Assembly.GetExecutingAssembly().GetName().Version}]";
            //Console.WriteLine("-- Inizio Programma Runner--\n");
            //#endregion

            //#region DatabaseCheck
            //try
            //{
            //    using (var dbContext = new Classes.ProduzioneEntities())
            //    {
            //        if (dbContext.Database.Exists()) Console.WriteLine("Connessione db OK");
            //        else Console.WriteLine("Connessione db Non presente.");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _log.WriteLog("Errore controllo esistenza Database : " + ex.Message);
            //}
            //#endregion

            //#region Aggiornamento Report Lavorazioni HMI
            //try
            //{
            //    _plc.UpdateRportGiorni1(Classes.PlcVariableName.ContatoreLavorazioneDestra);
            //    _plc.UpdateRportGiorni2(Classes.PlcVariableName.ContatoreLavorazioneSinistra);
            //    _plc.UpdateRportTotale(Classes.PlcVariableName.ContatoreLavorazioneDestra, Classes.PlcVariableName.ContatoreLavorazioneSinistra);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    _log.WriteLog("Errore aggiornameto lavorazioni : " + ex.Message);
            //}
            //#endregion

            //#region Threads 
            //_plc.AsyncHeartBeat();
            //_plc.AsyncScreebaLoop();
            //_plc.AsyncCheckEndOfTheGame();
            //_plc.AsyncCheckForWaste();
            //#endregion

            while (true)
            {
                Thread.Sleep(2000);
            }
        }

    }
}
