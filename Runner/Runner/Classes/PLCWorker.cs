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
        #region proprietà
        private object _comunicationLock;

        //PLC utilizzato per l'applicazione

#if (NX)
        private NXCompolet _plc = new NXCompolet();
#endif
#if (NJ)
        private NJCompolet _plc = new NJCompolet();
#endif
        #endregion

        #region costruttore
        public PLCWorker()
        {
            _comunicationLock = new object();
            _plc.PeerAddress = "10.0.50.121";
            _plc.LocalPort = 2;
        }
        #endregion

        #region metodi
        ///<summary>Test Connessione con plc </summary>
        public void HeartBeat()
        {


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
                        if (!uselessBool.Value)
                        {
                            _plc.WriteVariable("HandShake", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Heartbeat Error: Eccezione in lettura PLC.");
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _plc.Active = false;
                    }
                }
                // tempo arbitrario per test di connessione
                Thread.Sleep(3500);
            }

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

        /// <summary>
        /// La funzione screeba scrive le ricette lette dal database, sul PLC
        /// </summary>
        public void Screeba()
        {
            while (true)
            {
                lock (_comunicationLock)
                {
                    //lettura ricette da database
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
                        Console.Write("Screeba Error [Srcittura ricette su PLC] : ");
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _plc.Active = false;
                    }
                    watch.Stop();
                    Console.WriteLine("Scrittura ricette aggiornate avvenuta in : "+watch.ElapsedMilliseconds+" ms");

                }

                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// Check fine pezzo per salvataggio db
        /// </summary>
        public void CheckEndOfTheGame()
        {
            //Se viene rilevata la presenza di fine lavorazione, viene salvata
            //nel database, altrimenti non verrà eseguita nessuna azione

            bool? wasTheGameEnded_Left = null;
            bool? wasTheGameEnded_Right = null;

            while (true)
            {

                //comunicazione con plc e lettura variabile di scambio segnale
                lock (_comunicationLock)
                {
                    try
                    {
                        _plc.Active = true;
                        //Controllo se è attiva la variabile di fine lavoro
                        wasTheGameEnded_Left = (bool)_plc.ReadVariable(Classes.PlcVariableName.EndOfTheGame);
#warning cambiare variabile da leggere
                        wasTheGameEnded_Right = (bool)_plc.ReadVariable(Classes.PlcVariableName.EndOfTheGame);


                        if (wasTheGameEnded_Left.Value || wasTheGameEnded_Right.Value)
                        {
                            //salvataggio log nel database
                            Classes.Database.WriteLog(new productionLog()
                            {
                                CodiceArticolo = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.CodiceArticolo),
                                Lotto = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Lotto),
                                TempoCiclo = (int)(_plc.ReadVariable(Classes.PlcVariableName.DataToLog.TempoCiclo)),
                                Waste = false,
#warning aggiungere variabile da impostare per salvataggio da destra o sinistra
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

        /// <summary>
        /// Controllo per richiesta Scarti
        /// </summary>
        public void CheckForWaste()
        {
            //Se viene rilevata la presenza di fine lavorazione, viene salvata
            //nel database, altrimenti non verrà eseguita nessuna azione
            bool? DoWeHaveAnyWaste_Left = null;
            bool? DoWeHaveAnyWaste_Right = null;

            while (true)
            {

                //comunicazione con plc e lettura variabile di scambio segnale
                lock (_comunicationLock)
                {
                    try
                    {
                        _plc.Active = true;
                        DoWeHaveAnyWaste_Left = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWaste);
                        DoWeHaveAnyWaste_Right = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWaste);
                        #warning aggiungere giusta variabile
                        if (DoWeHaveAnyWaste_Left.Value || DoWeHaveAnyWaste_Right.Value)
                        {
                            #warning aggiungere controllo per scarto di destra o di sinistra
                            // riporto il valore del plc a false, in modo da riabilitare il comando
                            _plc.WriteVariable(Classes.PlcVariableName.LastOneIsWaste, false);
                            Database.SubRecepyNumber(DoWeHaveAnyWaste_Left.Value ? true : false);
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
        #endregion


        #region async Launcher
        public void AsyncHeartBeat()
        {

            Thread asyncBeater = new Thread(HeartBeat);
            asyncBeater.IsBackground = true;
            asyncBeater.Start();

        }
        public void AsyncCheckForWaste()
        {
            Thread wasteCollector = new Thread(CheckForWaste);
            wasteCollector.IsBackground = true;
            wasteCollector.Start();
        }
        public void AsyncCheckEndOfTheGame()
        {

            Thread asyncEndOfTheGame = new Thread(CheckEndOfTheGame);
            asyncEndOfTheGame.IsBackground = true;
            asyncEndOfTheGame.Start();

        }
        public void AsyncScreebaLoop()
        {

            Thread debugLoop = new Thread(Screeba);
            debugLoop.IsBackground = true;
            debugLoop.Start();

        }
        #endregion

    }
}