using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner.Classes
{
    public static class Database
    {
        public static object locker = new object();
        static Luca.Logger _log = new Luca.Logger(@"\GiDi_Runner\Database\");
        public enum SavingNewLogResult
        {
            Errore,
            SalvatoSoloLog,
            SalvatoEAggiorantaTabellaOrdini
        }

        public enum SavingNewWaste
        {
            Errore,
            LogNonTrovato,
            SoloLogAggiornato,
            SalvatoEAggiorantaTabellaOrdini
        }

        public struct ResocontoOrdine
        {
            public string CodiceArticolo;
            public int Produzione;
        }

        public static List<Classes.production2plc> ReadRecepies()
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.production2plc.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                return null;
            }
        }

        public static SavingNewLogResult WriteLog(Classes.productionLog dataToLog)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    contex.productionLogs.Add(dataToLog);
                    contex.SaveChanges();

                    //se non esiste nessuna ricetta con questo lotto registrata, la lavorazione è stata 
                    //effettuata con una delle due lavorazioni jolly del HMI.
                    //Non viene quindi aggiornata la tabella ordini
                    if (!contex.production2plc.Any(l => l.Lotto == dataToLog.Lotto))
                    {
                        PLCWorker.ConsoleWriteOnEventWarning("Can not update recepy global production, recepy doesent exists.");
                        return SavingNewLogResult.SalvatoSoloLog;
                    }

                }
                if (AddRecepyNumber(dataToLog.Lotto)) return SavingNewLogResult.SalvatoEAggiorantaTabellaOrdini;
                else return SavingNewLogResult.SalvatoSoloLog;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _log.WriteLog("Error Writing log for new Production. Insert row on Production error : " + ex);
                return SavingNewLogResult.Errore;
            }
        }

        // Legge la produzione dal database, in base ai parametri passati
        public static int ReadProduction(DateTime giorno)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.productionLogs.Where(c => c.OraLog.Day == giorno.Day).ToList().Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return -1;
            }
        }
        public static int ReadProduction(DateTime giorno, int turno)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.productionLogs.Where(c => c.OraLog.Day == giorno.Day
                        && c.Turno == turno).ToList().Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return -1;
            }
        }
        public static int ReadProduction(string codiceArticolo)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.productionLogs.Where(c => c.CodiceArticolo.Trim() == codiceArticolo.Trim()
                        && c.OraLog.Day == DateTime.Today.Day).ToList().Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return -1;
            }
        }
        public static int ReadProduction(string codiceArticolo, DateTime giorno)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.productionLogs.Where(c => c.CodiceArticolo.Trim() == codiceArticolo.Trim()
                        && giorno.Day == giorno.Day).ToList().Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return -1;
            }
        }
        public static int ReadProduction(string codiceArticolo, int turno)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.productionLogs.Where(c => c.CodiceArticolo.Trim() == codiceArticolo.Trim()
                        && c.Turno == turno).ToList().Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return -1;
            }
        }
        public static int ReadProduction(string codiceArticolo, int turno, DateTime giorno)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    return contex.productionLogs.Where(c => c.CodiceArticolo.Trim() == codiceArticolo.Trim()
                        && c.Turno == turno && c.OraLog.Day == giorno.Day).ToList().Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return -1;
            }
        }
        //ritorna la produzione giornaliera degli ultimi due codici lavorati
        public static ResocontoOrdine[] ReadDailyProductionLastTwoWorkedCode()
        {
            try
            {
                ResocontoOrdine uno, due;

                using (var contex = new Classes.ProduzioneEntities())
                {
                    uno.CodiceArticolo = contex.productionLogs.OrderByDescending(i => i.id).FirstOrDefault().CodiceArticolo;
                    due.CodiceArticolo = contex.productionLogs.Where(l => l.CodiceArticolo != uno.CodiceArticolo).OrderByDescending(i => i.id).FirstOrDefault().CodiceArticolo;
                    uno.Produzione = ReadProduction(uno.CodiceArticolo, DateTime.Today);
                    due.Produzione = ReadProduction(due.CodiceArticolo, DateTime.Today);
                    return new ResocontoOrdine[] { uno, due };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read production error : " + ex.Message);
                return null;
            }
        }
        private static bool AddRecepyNumber(string nomeLotto)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    Classes.production2plc ricetta = contex.production2plc.Where(l => l.Lotto == nomeLotto).FirstOrDefault();
                    ricetta.NumeroParziale++;
                    contex.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                PLCWorker.ConsoleWriteOnEventError("Error on adding 1 to a recepy - " + ex);
                _log.WriteLog("Error on adding 1 to a recepy - " + ex);
                return false;
            }
        }
        public static SavingNewWaste SubRecepyNumber(PlcVariableName.StazioneSaldatrice stazioneSaldatrice, int idLavorazione)
        {
            productionLog log = new productionLog();

            using (var contex = new Classes.ProduzioneEntities())
            {

                #region Get Log from db
                try
                {
                    // Assegno ad un nuovo log, il valore dell'ultimo record inserito, non marcato come scarto, che è 
                    // stato lavorato nella stazione passata come parametro
                    log = contex.productionLogs.OrderByDescending(i => i.id)
                        .Where(l => l.Stazione == (int)stazioneSaldatrice && l.Waste == false &&
                        l.IdLavorazione == idLavorazione).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    string mex = $"SubRecepyNumber, error on finding requested log with Id {idLavorazione} : " + ex.Message;
                    PLCWorker.ConsoleWriteOnEventError(mex);
                    _log.WriteLog(mex);
                    return SavingNewWaste.Errore;
                }
                #endregion

                // assegno il valore di scarto
                if (log == null) return SavingNewWaste.LogNonTrovato;
                log.Waste = true;
                #region Salvataggio Log con flag di waste
                try
                {
                    contex.SaveChanges();
                }
                catch (Exception ex)
                {
                    string mex = $"SubRecepyNumber, Errore salvataggio log con flag di waste " + ex.Message;
                    PLCWorker.ConsoleWriteOnEventError(mex);
                    _log.WriteLog(mex);
                    return SavingNewWaste.Errore;
                }
                #endregion


                production2plc ricetta = contex.production2plc.Where(l => l.Lotto == log.Lotto).FirstOrDefault();
                if (ricetta == null)
                {
                    string mex = $"Impossibile Aggiornare la ricetta con lotto {log.Lotto}. Lotto non trovata." +
                        "Solo il log è stato modificato";
                    PLCWorker.ConsoleWriteOnEventWarning(mex);
                    _log.WriteLog(mex);
                    return SavingNewWaste.SoloLogAggiornato;
                }
                else
                {
                    if (ricetta.NumeroParziale > 0)
                    {
                        ricetta.NumeroParziale--;
                    }
                }
                #region Salvataggio Ricetta con numero diminuito di uno
                try
                {
                    contex.SaveChanges();
                }
                catch (Exception ex)
                {
                    string mex = $"SubRecepyNumber, Errore salvataggio ricetta con numero diminutio di 1 " + ex.Message;
                    PLCWorker.ConsoleWriteOnEventError(mex);
                    _log.WriteLog(mex);
                    return SavingNewWaste.SoloLogAggiornato;
                }
                #endregion

                string stazione;
                if ((int)log.Stazione == 1) stazione = "Sinistra";
                else stazione = "Destra";

                string message = $"Scartata la lavorazione con Lotto {log.Lotto}, Codice Articolo {log.CodiceArticolo}, lavorata dalla stazione {stazione},\n";
                message += $"Alle ore {log.OraLog}, Turno {log.Turno}";
                PLCWorker.ConsoleWriteOnEventSuccess(message);
                _log.WriteLog(message);
                return SavingNewWaste.SalvatoEAggiorantaTabellaOrdini;
            }
        }

    }
}

