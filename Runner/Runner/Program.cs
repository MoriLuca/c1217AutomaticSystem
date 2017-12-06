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
            Classes.PLCWorker _plc = new Classes.PLCWorker();
            _plc.AsyncHeartBeat();
            _plc.AsyncScreebaLoop();
            _plc.AsyncCheckEndOfTheGame();
            _plc.AsyncCheckForWaste();
            while (true)
            {
                Thread.Sleep(300);
            }
        }
    }
}
