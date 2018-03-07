using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)

        {
            Console.Title = "Runner V[2.0]";
            using (var dbContext = new Classes.ProduzioneEntities())
            {
                if (dbContext.Database.Exists()) Console.WriteLine("Connessione db OK");
                else Console.WriteLine("Connessione db Non presente.");
            }
            Classes.PLCWorker _plc = new Classes.PLCWorker();
            Console.WriteLine("-- Inizio Programma Runner--\n");
            _plc.AsyncHeartBeat();
            _plc.AsyncScreebaLoop();
            _plc.AsyncCheckEndOfTheGame();
            // _plc.AsyncCheckForWaste();
            Console.ReadLine();
        }
    }
}
