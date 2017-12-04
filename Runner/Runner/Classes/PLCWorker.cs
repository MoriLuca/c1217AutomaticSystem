using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMRON.Compolet.CIP;

namespace Runner.Classes
{
    public class PLCWorker
    {
        private object _comunicationLock;
        private NXCompolet _plc;

        public PLCWorker()
        {
            _comunicationLock = new object();
            _plc = new NXCompolet();
            _plc.PeerAddress = "192.168.1.201";
            _plc.LocalPort = 2;
        }
        
        public void HeartBeat()
        {
            //funzione per testare la comunicazione con il plc.
            //questa variabile booleana che viene portata ad uno da questo porgramma, 
            //verrà riportata a 0 dal plc come verifica di effettiva connessione

            bool? uselessBool = null;
            
            //comunicazione con plc e lettura variabile di scambio segnale
            lock (_comunicationLock)
            {
                try
                {
                    _plc.Active = true;
                    uselessBool = (bool)_plc.ReadVariable("HandShake");
                    if (uselessBool != null && uselessBool == false)
                    {
                        _plc.WriteVariable("HandShake", true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    
                    _plc.Active = false;
                }
            }

        }

    }
}
