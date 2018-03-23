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

        Luca.Logger _log = new Luca.Logger(@"\GiDi_Runner\PLCWorker\");
        static Luca.EmailConfiguration conf = new Luca.EmailConfiguration();
        static MimeKit.MimeMessage ma = new MimeKit.MimeMessage();

        #region proprietà
        private object _comunicationLock;
        private static string _newLine = new string('*', Console.WindowWidth);

        //i seguenti booleani servono per non ripetere continuamente lo stato di errore
        private bool _heartBeatStatus;
        private bool? _EndOfTheGameStatus;
        private bool? _CheckForWasteStatus;

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

            conf.SmtpServer = "smtp.gmail.com";
            conf.SmtpPort = 465;
            conf.SmtpUsername = "wmori.luca@gmail.com";
            conf.SmtpPassword = "plOK12@#@#";

            if (!_plc.Active)
                _plc.Active = true;

            #region Aggiornamento Report Lavorazioni HMI
            try
            {
                UpdateRportGiorni1(Classes.PlcVariableName.ContatoreLavorazioneDestra);
                UpdateRportGiorni2(Classes.PlcVariableName.ContatoreLavorazioneSinistra);
                UpdateRportTotale(Classes.PlcVariableName.ContatoreLavorazioneDestra, Classes.PlcVariableName.ContatoreLavorazioneSinistra);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _log.WriteLog("Errore aggiornameto lavorazioni : " + ex.Message);
            }
            #endregion

            #region Threads 
            AsyncHeartBeat();
            AsyncScreebaLoop();
            AsyncCheckEndOfTheGame();
            AsyncCheckForWaste();
            #endregion
        }
        #endregion

        #region metodi

        public static void ConsoleWriteOnEventSuccess(string s)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void ConsoleWriteOnEventError(string s)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(_newLine + Environment.NewLine);
            Console.ForegroundColor = ConsoleColor.White;
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

                        if (!_plc.Active) _plc.Active = true;
                        uselessBool = (bool?)_plc.ReadVariable("HandShake");
                        if (!uselessBool.Value)
                        {
                            _plc.WriteVariable("HandShake", true);

                            //scrittura ore su plc
                            _plc.WriteVariable(PlcVariableName.NuovaOra, (ushort)DateTime.Now.Hour);
                            _plc.WriteVariable(PlcVariableName.NuovoMinuto, (ushort)DateTime.Now.Minute);
                            _plc.WriteVariable(PlcVariableName.NuoviSecondi, (ushort)DateTime.Now.Second);

                            //controllo se lo stato è differente dall'ultima volta
                            if (!_heartBeatStatus)
                            {
                                _heartBeatStatus = true;
                                string mex = "Heartbeat: Stabilita Comunicazione con PLC OK! - PLC Address : " + _plc.PeerAddress;
                                ConsoleWriteOnEventSuccess(mex);
                                _log.WriteLog(mex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string mex = "Heartbeat Error: Eccezione in lettura PLC : " + ex.Message;
                        ConsoleWriteOnEventError(mex);
                        _log.WriteLog(mex);
                        if (_heartBeatStatus) _heartBeatStatus = false;
                    }
                }
                // tempo arbitrario per test di connessione
                Thread.Sleep(5000);
            }

        }

        /// <summary>
        /// La funzione screeba scrive le ricette lette dal database, sul PLC
        /// </summary>
        public void Screeba()
        {
            while (true)
            {
                Thread.Sleep(5000);

                lock (_comunicationLock)
                {
                    //lettura ricette da database
                    bool ricetteUguali = true;
                    List<Classes.production2plc> listaProduzione = Classes.Database.ReadRecepies();
                    if (listaProduzione == null || listaProduzione.Count < 1)
                    {
                        string mex = "The production list, retrived from the database, is null or contain less than 1 items";
                        ConsoleWriteOnEventError(mex);
                        _log.WriteLog(mex);
                        continue;

                    }
                    else
                    {
                        if (listaProduzione.Count != _ultimaListaProduzione.Count)
                        {
                            _ultimaListaProduzione = new List<production2plc>(listaProduzione.Count);
                            ricetteUguali = false;
                        }
                    }

                    //Se la lista ha lo stesso numero di elementi, controllo se sono
                    //uguali
                    if (ricetteUguali)
                    {
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
                        catch (Exception ex)
                        {
                            string mex = "Error on Verifying if the recepies are equals to the old ones - " + ex.Message;
                            ConsoleWriteOnEventError(mex);
                            _log.WriteLog(mex);
                            //In caso di erroe di questo tipo, procedo alla riscrittura delle nuove ricette.
                            ricetteUguali = false;
                        }
                    }

                    //se le ricette contengono delle differenze, avviene l'aggiornamento
                    if (!ricetteUguali)
                    {

                        var watch = System.Diagnostics.Stopwatch.StartNew();

                        try
                        {
                            if (!_plc.Active) _plc.Active = true;
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
                            //Aggiorno le ultime ricette con quelle correnti per il prossimo check
                            _ultimaListaProduzione = listaProduzione;
                            ConsoleWriteOnEventSuccess("Scrittura ricette aggiornate avvenuta in : " + watch.ElapsedMilliseconds + " ms");
                        }
                        catch (Exception ex)
                        {
                            watch.Stop();
                            string mex = "Screeba Error [Srcittura ricette su PLC] : " + ex.Message;
                            ConsoleWriteOnEventError(mex);
                            _log.WriteLog(mex);
                        }
                    }
                }
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
                Thread.Sleep(5000);
                //comunicazione con plc e lettura variabile di scambio segnale

                try
                {
                    //Controllo se è attiva la variabile di fine lavoro
                    lock (_comunicationLock)
                    {
                        if (!_plc.Active) _plc.Active = true;
                        wasTheGameEnded = (bool?)_plc.ReadVariable(Classes.PlcVariableName.EndOfTheGame);
                    }
                    if (!wasTheGameEnded.Value) continue;
                    if (wasTheGameEnded.Value)
                    {
                        productionLog p;
                        lock (_comunicationLock)
                        {
                            p = new productionLog()
                            {
                                OraLog = DateTime.Now,
                                CodiceArticolo = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.CodiceArticolo),
                                Lotto = (string)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Lotto),
                                TempoCiclo = (int)(_plc.ReadVariable(Classes.PlcVariableName.DataToLog.TempoCiclo)),
                                Waste = false, //Forzato a falso perche il pezzo appena finito non puo essere scarto
                                Stazione = (int)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Stazione),
                                Turno = (int)_plc.ReadVariable(Classes.PlcVariableName.DataToLog.Turno),
                                IdLavorazione = Convert.ToInt32(_plc.ReadVariable(Classes.PlcVariableName.DataToLog.IdLavorazione))
                            };
                        }

                        //salvataggio log nel database
                        Classes.Database.WriteLog(p);
                        lock (_comunicationLock)
                        {
                            //Riabilito la possibilità dello scarto su plc
                            _plc.WriteVariable(Classes.PlcVariableName.EndOfTheGame, false);
                        }

                        TimeSpan durataCiclo = TimeSpan.FromSeconds((float)p.TempoCiclo / 10);
                        string success = "\nNuovo pezzo prodotto :\n";
                        success += $"Data e Ora : {p.OraLog}" + Environment.NewLine;
                        success += $"Codice Articolo : {p.CodiceArticolo}" + Environment.NewLine;
                        success += $"Lotto : {p.Lotto}" + Environment.NewLine;
                        success += $"TempoCiclo : {durataCiclo.ToString(@"hh\:mm\:ss")}" + Environment.NewLine;
                        success += $"Stazione : {p.Stazione}" + Environment.NewLine;
                        success += $"Turno : {p.Turno}" + Environment.NewLine;

                        ConsoleWriteOnEventSuccess(success);

                        //Aggiornamento report su plc
                        UpdateRportGiorni1(Classes.PlcVariableName.ContatoreLavorazioneDestra);
                        UpdateRportGiorni2(Classes.PlcVariableName.ContatoreLavorazioneSinistra);
                        UpdateRportTotale(Classes.PlcVariableName.ContatoreLavorazioneDestra, Classes.PlcVariableName.ContatoreLavorazioneSinistra);

                        #region sendEmail
                        try
                        {
                            Luca.EmailService s = new Luca.EmailService(conf);
                            List<Luca.EmailAddress> l = new List<Luca.EmailAddress>()
                        {
                            new Luca.EmailAddress()
                            {
                                Name = "Robot Poliplast",
                                Address = "Robottino@Poliplast.com"
                            }
                        };
                            List<Luca.EmailAddress> lt = new List<Luca.EmailAddress>()
                        {
                            new Luca.EmailAddress()
                            {
                                Name = "Luca Mori",
                                Address = "luca.mori@gidiautomazione.it"
                            }
                        };

                            Luca.EmailMessage m = new Luca.EmailMessage()
                            {
                                Subject = "NuovoPezzoProdotto",
                                Content = "<h1>Prodotto nuovo Pezzo</h1>" +
                                    $"<p>Orario : {p.OraLog}</p>" +
                                    $"<p>Stazione : {p.Stazione}</p>" +
                                    $"<p>TempoLavorazione : {p.TempoCiclo}</p>",
                                FromAddresses = l,
                                ToAddresses = lt
                            };

                            s.Send(m);
                        }
                        catch (Exception ex)
                        {
                            _log.WriteLog("Errore invio email Produzione : "+ex.Message);
                        }

                        #endregion


                        if (!_EndOfTheGameStatus.HasValue || !_EndOfTheGameStatus.Value)
                        {
                            _EndOfTheGameStatus = true;
                            ConsoleWriteOnEventSuccess("Controllo Fine Pezzo eseguito correttamente");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!_EndOfTheGameStatus.HasValue || _EndOfTheGameStatus.Value)
                    {
                        _EndOfTheGameStatus = false;
                        ConsoleWriteOnEventError("End Of The Game Error : " + ex.Message);
                    }


                }
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
            int QuantitaScartiDestra;
            int QuantitaScartiSinistra;


            while (true)
            {

                //comunicazione con plc e lettura variabile di scambio segnale

                try
                {
                    lock (_comunicationLock)
                    {
                        DoWeHaveAnyWaste_Left = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWasteLeft);
                        DoWeHaveAnyWaste_Right = (bool)_plc.ReadVariable(Classes.PlcVariableName.LastOneIsWasteRight);
                        QuantitaScartiDestra = (int)_plc.ReadVariable(Classes.PlcVariableName.QuantitaScartiDestra);
                        QuantitaScartiSinistra = (int)_plc.ReadVariable(Classes.PlcVariableName.QuantitaScartiSinistra);
                    }
                    if (DoWeHaveAnyWaste_Left.Value || DoWeHaveAnyWaste_Right.Value)
                    {
                        if (DoWeHaveAnyWaste_Left.Value)
                        {
                            for (int i = 0; i < QuantitaScartiSinistra; i++)
                            {
                                int id;
                                lock (_comunicationLock)
                                {
                                    id = Convert.ToInt32(_plc.ReadVariable(Classes.PlcVariableName.ContatoreLavorazioneSinistra));
                                }
                                // riporto il valore del plc a false, in modo da riabilitare il comando
                                string mex = Database.SubRecepyNumber(PlcVariableName.StazioneSaldatrice.Sinistra, id);
                                if (!mex.Contains("Error")) ConsoleWriteOnEventSuccess(mex);
                                else
                                {
                                    ConsoleWriteOnEventError(mex);
                                    _log.WriteLog(mex);
                                }

                            }
                            _plc.WriteVariable(Classes.PlcVariableName.LastOneIsWasteLeft, false);
                        }
                        else
                        {
                            int id = Convert.ToInt32(_plc.ReadVariable(Classes.PlcVariableName.ContatoreLavorazioneDestra));
                            for (int i = 0; i < QuantitaScartiDestra; i++)
                            {
                                // riporto il valore del plc a false, in modo da riabilitare il comando
                                string mex = Database.SubRecepyNumber(PlcVariableName.StazioneSaldatrice.Destra, id);
                                if (!mex.Contains("Error")) ConsoleWriteOnEventSuccess(mex);
                                else ConsoleWriteOnEventError(mex);
                            }
                            _plc.WriteVariable(Classes.PlcVariableName.LastOneIsWasteRight, false);
                        }

                        //Aggiornamento report su PLC
                        UpdateRportGiorni1(Classes.PlcVariableName.ContatoreLavorazioneDestra);
                        UpdateRportGiorni2(Classes.PlcVariableName.ContatoreLavorazioneSinistra);
                        UpdateRportTotale(Classes.PlcVariableName.ContatoreLavorazioneDestra, Classes.PlcVariableName.ContatoreLavorazioneSinistra);

                        if (!_CheckForWasteStatus.HasValue || !_CheckForWasteStatus.Value)
                        {
                            _CheckForWasteStatus = true;
                            ConsoleWriteOnEventSuccess("Waste eseguito, ricalcolo pezzi prodotti riuscito");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!_CheckForWasteStatus.HasValue || _CheckForWasteStatus.Value)
                    {
                        _CheckForWasteStatus = false;
                        ConsoleWriteOnEventError("Check for waste Error: " + ex.Message);
                    }


                }

                Thread.Sleep(2000);
            }
        }

        public void UpdateRportGiorni1(string stringaId)
        {

            int offsetOreTurno;
            int idLavorazione;

            try
            {
                lock (_comunicationLock)
                {
                    offsetOreTurno = Convert.ToInt32(_plc.ReadVariable(Classes.PlcVariableName.Ora_Inizio_Turno_1));
                    idLavorazione = Convert.ToInt32(_plc.ReadVariable(stringaId));
                }
                DateTime dayStart = DateTime.Now.Date.AddHours(offsetOreTurno);
                DateTime dayEnd = dayStart.AddHours(24);



                using (var context = new Classes.ProduzioneEntities())
                {
                    //Aggiornamento report Array 7 giorni
                    for (int i = 1; i < 8; i++)
                    {
                        //salto il primo ciclo, perche non devo sottrarre nessun giorno.
                        //tutto perche l 'array sul plc parte da 1
                        if (i > 1)
                        {
                            dayStart = dayStart.AddDays(-1);
                            dayEnd = dayEnd.AddDays(-1);
                        }


                        int totale = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoni = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScarti = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoniTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                                && t.Turno == 1 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoniTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                                && t.Turno == 2 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoniTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                                && t.Turno == 3 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScartiTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                                && t.Turno == 1 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScartiTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                                && t.Turno == 2 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScartiTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                                && t.Turno == 3 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;

                        lock (_comunicationLock)
                        {
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.Giorno, dayStart.Date.ToString("dd/M/yy"));
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleProduzione, totale);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleBuoni, totaleBuoni);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleScarti, totaleScarti);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleTurno1, totaleBuoniTurno1 + totaleScartiTurno1);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleTurno2, totaleBuoniTurno2 + totaleScartiTurno2);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleTurno3, totaleBuoniTurno3 + totaleScartiTurno3);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleBuoniTurno1, totaleBuoniTurno1);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleBuoniTurno2, totaleBuoniTurno2);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleBuoniTurno3, totaleBuoniTurno3);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleScartiTurno1, totaleScartiTurno1);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleScartiTurno2, totaleScartiTurno2);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate1 + $"[{i}]." + PlcVariableName.TotaleScartiTurno3, totaleScartiTurno3);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _log.WriteLog("Errore scrittura Report Giornate 1: " + ex.Message);
            }

        }

        public void UpdateRportGiorni2(string stringaId)
        {
            int offsetOreTurno;
            int idLavorazione;

            try
            {
                lock (_comunicationLock)
                {
                    offsetOreTurno = Convert.ToInt32(_plc.ReadVariable(Classes.PlcVariableName.Ora_Inizio_Turno_1));
                    idLavorazione = Convert.ToInt32(_plc.ReadVariable(stringaId));
                }
                DateTime dayStart = DateTime.Now.Date.AddHours(offsetOreTurno);
                DateTime dayEnd = dayStart.AddHours(24);

                using (var context = new Classes.ProduzioneEntities())
                {
                    //Aggiornamento report Array 7 giorni
                    for (int i = 1; i < 8; i++)
                    {
                        //salto il primo ciclo, perche non devo sottrarre nessun giorno.
                        //tutto perche l 'array sul plc parte da 1
                        if (i > 1)
                        {
                            dayStart = dayStart.AddDays(-1);
                            dayEnd = dayEnd.AddDays(-1);
                        }


                        int totale = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoni = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScarti = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoniTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                                && t.Turno == 1 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoniTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                                && t.Turno == 2 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleBuoniTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                                && t.Turno == 3 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScartiTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                                && t.Turno == 1 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScartiTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                                && t.Turno == 2 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;
                        int totaleScartiTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                                && t.Turno == 3 && t.OraLog > dayStart && t.OraLog < dayEnd).ToList().Count;

                        lock (_comunicationLock)
                        {
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.Giorno, dayStart.Date.ToString("dd/M/yy"));
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleProduzione, totale);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleBuoni, totaleBuoni);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleScarti, totaleScarti);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleTurno1, totaleBuoniTurno1 + totaleScartiTurno1);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleTurno2, totaleBuoniTurno2 + totaleScartiTurno2);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleTurno3, totaleBuoniTurno3 + totaleScartiTurno3);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleBuoniTurno1, totaleBuoniTurno1);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleBuoniTurno2, totaleBuoniTurno2);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleBuoniTurno3, totaleBuoniTurno3);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleScartiTurno1, totaleScartiTurno1);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleScartiTurno2, totaleScartiTurno2);
                            _plc.WriteVariable(PlcVariableName.pathReportGiornate2 + $"[{i}]." + PlcVariableName.TotaleScartiTurno3, totaleScartiTurno3);

                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
                _log.WriteLog("Errore scrittura Report Giornate 2: " + ex.Message);
            }



        }

        public void UpdateRportTotale(string stringaId1, string stringaId2)
        {
            ReportTotale1(stringaId1);
            ReportTotale2(stringaId2);
        }

        private void ReportTotale1(string id)
        {

            try
            {
                int idLavorazione;

                lock (_comunicationLock)
                {
                    idLavorazione = Convert.ToInt32(_plc.ReadVariable(id));
                }

                using (var context = new Classes.ProduzioneEntities())
                {

                    int tot = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione).ToList().Count;
                    int totBuoni = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste).ToList().Count;
                    int totScarti = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste).ToList().Count;
                    int totBuoniTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                            && t.Turno == 1).ToList().Count;
                    int totBuoniTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                            && t.Turno == 2).ToList().Count;
                    int totBuoniTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                            && t.Turno == 3).ToList().Count;
                    int totScartiTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                            && t.Turno == 1).ToList().Count;
                    int totScartiTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                            && t.Turno == 2).ToList().Count;
                    int totScartiTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                            && t.Turno == 3).ToList().Count;
                    lock (_comunicationLock)
                    {
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleProduzione, tot);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleBuoni, totBuoni);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleScarti, totScarti);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleBuoniTurno1, totBuoniTurno1);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleBuoniTurno2, totBuoniTurno2);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleBuoniTurno3, totBuoniTurno3);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleScartiTurno1, totScartiTurno1);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleScartiTurno2, totScartiTurno2);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale1 + "." + PlcVariableName.TotaleScartiTurno3, totScartiTurno3);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
                _log.WriteLog("Errore scrittura Report Totale 1: " + ex.Message);
            }

        }

        private void ReportTotale2(string id)
        {

            try
            {
                int idLavorazione;
                lock (_comunicationLock)
                {
                    idLavorazione = Convert.ToInt32(_plc.ReadVariable(id));
                }
                using (var context = new Classes.ProduzioneEntities())
                {

                    int tot = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione).ToList().Count;
                    int totBuoni = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste).ToList().Count;
                    int totScarti = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste).ToList().Count;
                    int totBuoniTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                            && t.Turno == 1).ToList().Count;
                    int totBuoniTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                            && t.Turno == 2).ToList().Count;
                    int totBuoniTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && !t.Waste
                                            && t.Turno == 3).ToList().Count;
                    int totScartiTurno1 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                            && t.Turno == 1).ToList().Count;
                    int totScartiTurno2 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                            && t.Turno == 2).ToList().Count;
                    int totScartiTurno3 = context.productionLogs.Where(t => t.IdLavorazione == idLavorazione && t.Waste
                                            && t.Turno == 3).ToList().Count;
                    lock (_comunicationLock)
                    {
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleProduzione, tot);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleBuoni, totBuoni);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleScarti, totScarti);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleBuoniTurno1, totBuoniTurno1);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleBuoniTurno2, totBuoniTurno2);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleBuoniTurno3, totBuoniTurno3);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleScartiTurno1, totScartiTurno1);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleScartiTurno2, totScartiTurno2);
                        _plc.WriteVariable(PlcVariableName.pathReportGlobale2 + "." + PlcVariableName.TotaleScartiTurno3, totScartiTurno3);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
                _log.WriteLog("Errore scrittura Report Totale 2: " + ex.Message);
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