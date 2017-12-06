#define NJ
//#undef NJ
//#define NX
#undef NX

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMRON.Compolet.CIP;
using System.Threading;

namespace Runner.Classes
{
    public class PLCWorker
    {

        private object _comunicationLock;
        //PLC utilizzato per l'applicazione

#if (NX) 
        private NXCompolet _plc = new NXCompolet();
#endif
#if (NJ)
        private NJCompolet _plc = new NJCompolet();
#endif


        public PLCWorker()
        {
            _comunicationLock = new object();
            _plc.PeerAddress = "192.168.1.201";
            _plc.LocalPort = 2;
        }

        public void HeartBeat()
        {
            //funzione per testare la comunicazione con il plc.
            //questa variabile booleana che viene portata ad uno da questo porgramma, 
            //verrà riportata a 0 dal plc come verifica di effettiva connessione

            bool? uselessBool = null;

            while (true)
            {

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

                Thread.Sleep(2000);
            }

        }

        public void AsyncHeartBeat()
        {

            Thread asyncBeater = new Thread(HeartBeat);
            asyncBeater.IsBackground = true;
            asyncBeater.Start();

        }

        public void ScreebaRandom()
        {
            while (true)
            {
                lock (_comunicationLock)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        Random random = new Random();
                        _plc.Active = true;
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[0], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[1], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[2], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[3], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[4], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[5], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[6], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[7], (Int16)random.Next(0, 20000));

                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[0], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[1], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[2], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[3], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[4], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[5], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[6], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[7], random.Next(0, 20000).ToString());

                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[0], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[1], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[2], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[3], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[4], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[5], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[6], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[7], random.Next(0, 20000).ToString());

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _plc.Active = false;
                    }
                    watch.Stop();
                    Console.WriteLine(watch.ElapsedMilliseconds);

                }

                Thread.Sleep(2000);
            }
        }

        public void ScreebaR()
        {
            while (true)
            {
                lock (_comunicationLock)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        Random random = new Random();
                        _plc.Active = true;
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[0], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[1], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[2], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[3], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[4], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[5], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[6], (Int16)random.Next(0, 20000));
                        _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[7], (Int16)random.Next(0, 20000));

                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[0], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[1], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[2], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[3], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[4], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[5], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[6], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.Lotti[7], random.Next(0, 20000).ToString());

                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[0], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[1], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[2], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[3], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[4], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[5], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[6], random.Next(0, 20000).ToString());
                        _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[7], random.Next(0, 20000).ToString());

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _plc.Active = false;
                    }
                    watch.Stop();
                    Console.WriteLine(watch.ElapsedMilliseconds);

                }

                Thread.Sleep(2000);
            }
        }

        public void ScreebaLoop()
        {

            Thread debugLoop = new Thread(ScreebaRandom);
            debugLoop.IsBackground = true;
            debugLoop.Start();

        }

        public void CheckEndOfTheGame()
        {

            //Se viene rilevata la presenza di fine lavorazione, viene salvata
            //nel database, altrimenti non verrà eseguita nessuna azione

            bool? wasTheGameEnded = null;

            while (true)
            {

                //comunicazione con plc e lettura variabile di scambio segnale
                lock (_comunicationLock)
                {
                    try
                    {
                        _plc.Active = true;
                        wasTheGameEnded = (bool)_plc.ReadVariable(Classes.PlcVariableName.EndOfTheGame);
                        if (wasTheGameEnded != null && wasTheGameEnded == true)
                        {
                            //salvataggio log nel database
                            _plc.WriteVariable(Classes.PlcVariableName.EndOfTheGame, false);
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

                Thread.Sleep(3000);
            }

        }

        public void AsyncCheckEndOfTheGame()
        {

            Thread asyncEndOfTheGame = new Thread(CheckEndOfTheGame);
            asyncEndOfTheGame.IsBackground = true;
            asyncEndOfTheGame.Start();

        }

    }
}