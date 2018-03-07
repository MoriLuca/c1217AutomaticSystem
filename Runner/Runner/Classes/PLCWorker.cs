using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMRON.Compolet.CIP;
using System.Threading;
using System.IO;

namespace Runner.Classes
{
    public class PLCWorker
    {
        #region proprietà
        private object _comunicationLock;
        private string _newLine = "******************************************************************************";
        string _writeOnPaper;
        //i seguenti booleani servono per non ripetere continuamente lo stato di errore
        private bool? _heartBeatStatus;
        private bool? _EndOfTheGameStatus;
        // lista utilizzata per verificare la necessita di scrivere la lista aggioranta sul plc
        List<Classes.production2plc> _ultimaListaProduzione = new List<production2plc>();
        //PLC utilizzato per l'applicazione
        private NXCompolet _plc = new NXCompolet();
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

        private void writeEventOnPaper(string s)
        {
            string mex = DateTime.Now + " - " + s + Environment.NewLine;
            _writeOnPaper += mex;
            try
            {
                string folderName = DateTime.Now.Date.ToString("dd_MM_yy");
                if (!Directory.Exists($"Log/{folderName}")) Directory.CreateDirectory($"Log/{folderName}");
                if (!File.Exists($"Log/{folderName}/report.txt")) File.Create($"Log/{folderName}/report.txt").Close();
                File.AppendAllText($"Log/{folderName}/report.txt", mex);
                _writeOnPaper = "";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore scrittura log su file txt : "+ex.Message);
            }
        }

        private void ConsoleWriteOnEventSuccess(string s)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.White;
            writeEventOnPaper(s);
        }

        private void ConsoleWriteOnEventError(string s)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(_newLine + Environment.NewLine);
            Console.ForegroundColor = ConsoleColor.White;
            writeEventOnPaper(s);
        }

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
                        uselessBool = (bool?)_plc.ReadVariable("HandShake");
                        if (!uselessBool.Value)
                        {
                            _plc.WriteVariable("HandShake", true);
                            //controllo se lo stato è differente dall'ultima volta
                            if (!_heartBeatStatus.HasValue || !_heartBeatStatus.Value)
                            {
                                _heartBeatStatus = true;
                                ConsoleWriteOnEventSuccess("Heartbeat: Comunicazione con PLC OK! - PLC Address : " + _plc.PeerAddress);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!_heartBeatStatus.HasValue || _heartBeatStatus.Value )
                        {
                            _heartBeatStatus = false;
                            ConsoleWriteOnEventError("Heartbeat Error: Eccezione in lettura PLC : " + ex.Message);
                        }
                        
                    }
                    finally
                    {
                        _plc.Active = false;
                    }
                }
                // tempo arbitrario per test di connessione
                Thread.Sleep(5000);
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
                    if (listaProduzione.Count != _ultimaListaProduzione.Count) _ultimaListaProduzione = new List<production2plc>(listaProduzione.Count);
                    bool ricetteUguali = true;
                    try
                    {
                        for (int i = 0; i < listaProduzione.Count; i++)
                        {
                            if (!listaProduzione[i].IsEqualTo(_ultimaListaProduzione[i]))
                            {
                                ricetteUguali = false;
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {

                        ricetteUguali = false;
                    }
                    
                    if (!ricetteUguali)
                    {
                        _ultimaListaProduzione = listaProduzione;

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
                            watch.Stop();
                            ConsoleWriteOnEventSuccess("Scrittura ricette aggiornate avvenuta in : " + watch.ElapsedMilliseconds + " ms");
                        }
                        catch (Exception ex)
                        {
                            watch.Stop();
                            ConsoleWriteOnEventError("Screeba Error [Srcittura ricette su PLC] : " + ex.Message);
                        }
                        finally
                        {
                            _plc.Active = false;
                        }
                    }
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

            bool? wasTheGameEnded = null;

            while (true)
            {

                //comunicazione con plc e lettura variabile di scambio segnale
                lock (_comunicationLock)
                {
                    try

                    {
                        _plc.Active = true;
                        //Controllo se è attiva la variabile di fine lavoro
                        wasTheGameEnded = (bool)_plc.ReadVariable(Classes.PlcVariableName.EndOfTheGame);


                        if (wasTheGameEnded.Value)
                        {
                            productionLog p = new productionLog()
                            {
                                OraLog = DateTime.Now,
                                CodiceArticolo = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.CodiceArticolo),
                                Lotto = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Lotto),
                                TempoCiclo = (int)(_plc.ReadVariable(Classes.PlcVariableName.DataToLog.TempoCiclo)),
                                Waste = false, //Forzato a falso perche il pezzo appena finito non puo essere scarto
                                Stazione = 0,//(int)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Stazione),
                                Turno = 1//(int)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Turno)
                            };

                            //salvataggio log nel database
                            Classes.Database.WriteLog(p);
                            //Riabilito la possibilità dello scarto su plc
                            _plc.WriteVariable(Classes.PlcVariableName.EndOfTheGame, false);
                            TimeSpan durataCiclo = TimeSpan.FromSeconds((float)p.TempoCiclo / 10);
                            string success = "Nuovo pezzo prodotto :\n";
                            success += $"Data e Ora : {p.OraLog}" + Environment.NewLine;
                            success += $"Codice Articolo : {p.CodiceArticolo}"+Environment.NewLine;
                            success += $"Lotto : {p.Lotto}" + Environment.NewLine;
                            success += $"TempoCiclo : {durataCiclo.ToString(@"hh\:mm\:ss")}" + Environment.NewLine;
                            success += $"Stazione : {p.Stazione}" + Environment.NewLine;
                            success += $"Turno : {p.Turno}" + Environment.NewLine;

                            ConsoleWriteOnEventSuccess(success);
                        }
                        if(!_EndOfTheGameStatus.HasValue || !_EndOfTheGameStatus.HasValue)
                        {
                            _EndOfTheGameStatus = true;
                            ConsoleWriteOnEventSuccess("Controllo Fine Pezzo eseguito correttamente");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!_EndOfTheGameStatus.HasValue || _EndOfTheGameStatus.HasValue)
                        {
                            _EndOfTheGameStatus = false;
                            ConsoleWriteOnEventError("End Of The Game Error : " + ex.Message);
                        }
                        
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
                        DoWeHaveAnyWaste_Left = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWasteLeft);
                        DoWeHaveAnyWaste_Right = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWasteRight);
                        if (DoWeHaveAnyWaste_Left.Value || DoWeHaveAnyWaste_Right.Value)
                        {
                            if (DoWeHaveAnyWaste_Left.Value)
                            {
                                // riporto il valore del plc a false, in modo da riabilitare il comando
                                _plc.WriteVariable(Classes.PlcVariableName.LastOneIsWasteLeft, false);
                                string mex = Database.SubRecepyNumber(PlcVariableName.StazioneSaldatrice.Sinistra);
                                if (!mex.Contains("Error")) ConsoleWriteOnEventSuccess(mex);
                                else ConsoleWriteOnEventError(mex);
                            }
                            else 
                            {
                                // riporto il valore del plc a false, in modo da riabilitare il comando
                                _plc.WriteVariable(Classes.PlcVariableName.LastOneIsWasteRight, false);
                                string mex = Database.SubRecepyNumber(PlcVariableName.StazioneSaldatrice.Destra);
                                if (!mex.Contains("Error")) ConsoleWriteOnEventSuccess(mex);
                                else ConsoleWriteOnEventError(mex);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleWriteOnEventError("Check for waste Error: "+ex.Message);
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