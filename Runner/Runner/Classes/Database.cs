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

        public static void WriteLog(Classes.productionLog dataToLog)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    contex.productionLogs.Add(dataToLog);
                    contex.SaveChanges();
                }
                AddRecepyNumber(dataToLog.Lotto);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
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
        private static void AddRecepyNumber(string nomeLotto)
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    Classes.production2plc ricetta = contex.production2plc.Where(l => l.Lotto == nomeLotto).FirstOrDefault();
                    ricetta.NumeroParziale++;
                    contex.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stazioneSaldatrice"></param>
        /// <returns>Stringa per console</returns>
        public static string SubRecepyNumber(PlcVariableName.StazioneSaldatrice stazioneSaldatrice, int idLavorazione)
        {
            string message = "";

            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {

                    // Assegno ad un nuovo log, il valore dell'ultimo record inserito, non marcato come scarto, che è 
                    // stato lavorato nella stazione passata come parametro
                    Classes.productionLog log = contex.productionLogs.OrderByDescending(i => i.id)
                        .Where(l => l.Stazione == (int)stazioneSaldatrice && l.Waste == false && l.IdLavorazione == idLavorazione).FirstOrDefault();
                    // assegno il valore di scarto
                    log.Waste = true;

                    Classes.production2plc ricetta = contex.production2plc.Where(l => l.Lotto == log.Lotto).FirstOrDefault();
                    if(ricetta != null)
                    {
                        if (ricetta.NumeroParziale > 0)
                        {
                            ricetta.NumeroParziale--;
                        }
                    }
                    else
                    {
                        message = "Error - SubRecepyNumber [Ricerca lotto da sottrarre]";
                    }
                    
                    contex.SaveChanges();
                    string stazione;
                    if ((int)log.Stazione == 1) stazione = "Sinistra";
                    else stazione = "Destra";
                    message += $"Scartata la lavorazione con Lotto {log.Lotto}, Codice Articolo {log.CodiceArticolo}, lavorata dalla stazione {stazione},\n";
                    message += $"Alle ore {log.OraLog}, Turno {log.Turno}";
                    return message;
                }
            }
            catch (Exception ex)
            {
                return ("Error - SubRecepyNumber " + ex.Message);
            }

        }
    }
}
