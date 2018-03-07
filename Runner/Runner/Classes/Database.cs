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
        public static string SubRecepyNumber(PlcVariableName.StazioneSaldatrice stazioneSaldatrice)
        {
            string message = "";

            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {

                    // Assegno ad un nuovo log, il valore dell'ultimo record inserito, non marcato come scarto, che è 
                    // stato lavorato nella stazione passata come parametro
                    Classes.productionLog log = contex.productionLogs.OrderByDescending(i => i.id)
                        .Where(l => l.Stazione == (int)stazioneSaldatrice && l.Waste == false).FirstOrDefault();
                    // assegno il valore di scarto
                    log.Waste = true;


                    Classes.production2plc ricetta = contex.production2plc.Where(l => l.Lotto == log.Lotto).FirstOrDefault();
                    if (ricetta.NumeroParziale > 0)
                    {
                        ricetta.NumeroParziale--;
                    }
                    contex.SaveChanges();
                    string stazione;
                    if ((int)log.Stazione == 0) stazione = "Sinistra";
                    else stazione = "Destra";
                    message = $"Scartata la lavorazione con Lotto {log.Lotto}, Codice Articolo {log.CodiceArticolo}, lavorata dalla stazione {stazione},\n";
                    message += $"Alle ore {log.OraLog}, Turno {log.Turno}";
                    return message;
                }

                
            }
            catch (Exception ex)
            {
                return( "Error - SubRecepyNumber " + ex.Message);
            }
            
        }
    }
}
