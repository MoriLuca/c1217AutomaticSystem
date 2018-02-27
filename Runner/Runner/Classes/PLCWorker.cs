//#define NJ
#undef NJ
#define NX
//#undef NX

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
            _plc.PeerAddress = "10.0.50.121";
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
                        Console.Write("Heartbeat Error:");
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {

                        _plc.Active = false;
                    }
                }

                Thread.Sleep(3500);
            }

        }

        public void AsyncHeartBeat()
        {

            Thread asyncBeater = new Thread(HeartBeat);
            asyncBeater.IsBackground = true;
            asyncBeater.Start();

        }

        //Funzione da utilizzare solamente in caso di debug
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

                        for (int i = 0; i < 8; i++)
                        {

                            _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[i], (Int16)random.Next(0, 20000));
                            _plc.WriteVariable(Classes.PlcVariableName.NumeroPezziAttuale[i], (Int16)random.Next(0, 20000));
                            _plc.WriteVariable(Classes.PlcVariableName.Lotti[i], random.Next(0, 20000).ToString());
                            _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[i], random.Next(0, 20000).ToString());


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
                    watch.Stop();
                    Console.WriteLine(watch.ElapsedMilliseconds);

                }

                Thread.Sleep(2000);
            }
        }

        public void Screeba()
        {
            while (true)
            {
                lock (_comunicationLock)
                {
                    List<Classes.production2plc> listaProduzione = Classes.Database.ReadRecepies();
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        _plc.Active = true;

                        for (int i = 0; i < listaProduzione.Count; i++)
                        {
                            if (listaProduzione[i].Lotto != null)
                            {
                                _plc.WriteVariable(Classes.PlcVariableName.NumeroPezzi[i], (Int16)listaProduzione[i].NumeroPezziTotali);
                                _plc.WriteVariable(Classes.PlcVariableName.NumeroPezziAttuale[i], (Int16)listaProduzione[i].NumeroParziale);
                                _plc.WriteVariable(Classes.PlcVariableName.Lotti[i], listaProduzione[i].Lotto);
                                _plc.WriteVariable(Classes.PlcVariableName.CodiceArticoli[i], listaProduzione[i].CodiceArticolo);
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        Console.Write("Screeba Error : ");
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _plc.Active = false;
                    }
                    watch.Stop();
                    Console.WriteLine(watch.ElapsedMilliseconds);

                }

                Thread.Sleep(5000);
            }
        }

        public void AsyncScreebaLoop()
        {

            Thread debugLoop = new Thread(Screeba);
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
                            Classes.Database.WriteLog(new productionLog() { 
                                CodiceArticolo = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.CodiceArticolo),
                                Lotto = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Lotto),
                                TempoCiclo = (int)(_plc.ReadVariable(Classes.PlcVariableName.DataToLog.TempoCiclo)),
                                Waste = false,
                            });
                            _plc.WriteVariable(Classes.PlcVariableName.EndOfTheGame, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write("End Of The Game Error :");
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

        public void AsyncCheckEndOfTheGame()
        {

            Thread asyncEndOfTheGame = new Thread(CheckEndOfTheGame);
            asyncEndOfTheGame.IsBackground = true;
            asyncEndOfTheGame.Start();

        }

        public void CheckForWaste()
        {
            //Se viene rilevata la presenza di fine lavorazione, viene salvata
            //nel database, altrimenti non verrà eseguita nessuna azione
            bool? DoWeHaveAnyWaste = null;

            while (true)
            {

                //comunicazione con plc e lettura variabile di scambio segnale
                lock (_comunicationLock)
                {
                    try
                    {
                        _plc.Active = true;
                        DoWeHaveAnyWaste = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWaste);
                        if (DoWeHaveAnyWaste != null && DoWeHaveAnyWaste == true)
                        {
                            _plc.WriteVariable(Classes.PlcVariableName.LastOneIsWaste, false);
                            Database.SubRecepyNumber();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Check for waste Error:");
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

        public void AsyncCheckForWaste()
        {
            Thread wasteCollector = new Thread(CheckForWaste);
            wasteCollector.IsBackground = true;
            wasteCollector.Start();
        }

    }
}