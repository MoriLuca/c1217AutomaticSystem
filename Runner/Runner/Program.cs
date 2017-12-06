using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            Classes.PLCWorker _plc = new Classes.PLCWorker();
            _plc.AsyncHeartBeat();
            //_plc.Screeba();
            _plc.ScreebaLoop();
            _plc.AsyncCheckEndOfTheGame();

            Console.Read();
        }
    }
}
