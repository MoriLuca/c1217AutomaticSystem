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
                    contex.productionLogs.Add(new productionLog {
                        CodiceArticolo = dataToLog.CodiceArticolo,
                        Lotto = dataToLog.Lotto,
                        TempoCiclo = dataToLog.TempoCiclo,
                    });
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

        public static void SubRecepyNumber()
        {
            try
            {
                using (var contex = new Classes.ProduzioneEntities())
                {
                    
                    Classes.productionLog log = contex.productionLogs.OrderByDescending(i => i.id).FirstOrDefault();
                    log.Waste = true;
                    Classes.production2plc ricetta = contex.production2plc.Where(l => l.Lotto == log.Lotto).FirstOrDefault();
                    if (ricetta.NumeroParziale > 0)
                    {
                        ricetta.NumeroParziale--;
                    }
                    contex.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }
        }
    }
}
