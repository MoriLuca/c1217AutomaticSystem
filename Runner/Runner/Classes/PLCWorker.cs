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
        private bool _EndOfTheGameStatus;
        private bool? _CheckForWasteStatus;
        //imposto il bit a false cosi la prima volta che apro il programma viene forzato a 
        private bool _statoConnessione = false;

        // lista utilizzata per verificare la necessita di scrivere la lista aggioranta sul plc
        List<Classes.production2plc> _ultimaListaProduzione = new List<production2plc>();

        //PLC utilizzato per l'applicazione
        //private NXCompolet _plc = new NXCompolet();
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

            #region sendEmail
            try
            {
                Luca.EmailService s = new Luca.EmailService(conf);
                List<Luca.EmailAddress> l = new List<Luca.EmailAddress>()
                        {
                            new Luca.EmailAddress()
                            {
                                Name = "Poliplast Supervisore",
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
                    Subject = "Accensione programma supervisione",
                    Content = "<h1>Nuova accensione</h1>" +
                        $"<p>Orario : {DateTime.Now}</p>",
                    FromAddresses = l,
                    ToAddresses = lt
                };

                s.Send(m);
            }
            catch (Exception ex)
            {
                _log.WriteLog("Errore invio email Produzione : " + ex.Message);
            }
            #endregion

            if (!_plc.Active)
                _plc.Active = true;

            #region Aggiornamento Report Lavorazioni HMI
            //try
            //{
            //    UpdateRportGiorni1(Classes.PlcVariableName.ContatoreLavorazioneDestra);
            //    UpdateRportGiorni2(Classes.PlcVariableName.ContatoreLavorazioneSinistra);
            //    UpdateRportTotale(Classes.PlcVariableName.ContatoreLavorazioneDestra, Classes.PlcVariableName.ContatoreLavorazioneSinistra);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    _log.WriteLog("Errore aggiornameto lavorazioni : " + ex.Message);
            //}
            #endregion

            #region Threads 
            AsyncHeartBeat();
            #endregion
        }
        public PLCWorker(bool test)
        {
            _comunicationLock = new object();
            conf.SmtpServer = "smtp.gmail.com";
            conf.SmtpPort = 465;
            conf.SmtpUsername = "wmori.luca@gmail.com";
            conf.SmtpPassword = "plOK12@#@#";
            //AsyncHeartBeat();
            CheckEndOfTheGame();
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

        public static void ConsoleWriteOnEventWarning(string s)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(_newLine);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
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

            while (true)
            {
                // tempo arbitrario per test di connessione
                Thread.Sleep(5000);

                //comunicazione con plc e lettura variabile di scambio segnale

                if (!_plc.Active) _plc.Active = true;

                //Imposto il bit a true, cosi sono sicuro che per andare false la lettura
                //è avvenuta ed è stato il PLC a riportarla a false
                bool heartBIT = true;
                #region Lettura Heartbeat
                try
                {
                    lock (_comunicationLock)
                    {
                        heartBIT = (bool)_plc.ReadVariable(PlcVariableName.HandShake);
                    }
                }
                catch (Exception ex)
                {
                    if (_statoConnessione)
                    {
                        string mex = $"Lost Connection. Error reading Handshake varaible {PlcVariableName.HandShake} :" + ex.Message;
                        ConsoleWriteOnEventError(mex);
                        _log.WriteLog(mex);
                        _statoConnessione = false;
                    }
                    continue;
                }
                #endregion

                //Se la lettura dal plc è ancora di un bool true, il plc non ha ancora aggiornato il valore
                //attendo e ripeto l'operazione
                if (heartBIT) continue;
                //Se il PLC ha riportato lo stato a false
                else
                {
                    #region Verifica Stato Connessione
                    //Se lo stato della connessione era impostato su false, vuol dire che la connessione
                    //è stata appena ristabilita
                    if (!_statoConnessione)
                    {
                        //Aggiorno il valore per sapere il prossimo ciclo che la connessione era gia attiva
                        _statoConnessione = true;
                        //Aggiornamento ore su plc, solo sulla riattivazione della connessione
                        //utilizzo lo stesso lock per impostare a true il bit di handshake
                        try
                        {
                            lock (_comunicationLock)
                            {
                                _plc.WriteVariable(PlcVariableName.NuovaOra, (ushort)DateTime.Now.Hour);
                                _plc.WriteVariable(PlcVariableName.NuovoMinuto, (ushort)DateTime.Now.Minute);
                                _plc.WriteVariable(PlcVariableName.NuoviSecondi, (ushort)DateTime.Now.Second);
                                _plc.WriteVariable(PlcVariableName.HandShake, true);
                            }
                            //report connessione ristabilita
                            string mex = "Heartbeat: Stabilita Comunicazione con PLC OK! - PLC Address : " + _plc.PeerAddress;
                            ConsoleWriteOnEventSuccess(mex);
                            _log.WriteLog(mex);
                        }
                        catch (Exception ex)
                        {
                            string mex = "Riconnessione a PLC : Errore scrittura heartbeat/aggiornamento ore " + ex.Message;
                            ConsoleWriteOnEventError(mex);
                            _log.WriteLog(mex);
                            if (_heartBeatStatus) _heartBeatStatus = false;
                        }

                    }
                    #endregion

                    //HeartBeat letto, di seguito il resto delle azioni da compiere
                    //quando la comunicazione è ok

                    //Controllo se ci sono pezzi completati oppure se ci sono scarti da aggiornare
                    if (CheckEndOfTheGame() || CheckForWaste())
                    {
                        //Aggiornamento report su plc
                        UpdateRportGiorni1(PlcVariableName.ContatoreLavorazioneDestra);
                        UpdateRportGiorni2(PlcVariableName.ContatoreLavorazioneSinistra);
                        UpdateRportTotale(PlcVariableName.ContatoreLavorazioneDestra, PlcVariableName.ContatoreLavorazioneSinistra);
                        try
                        {
                            lock (_comunicationLock)
                            {
                                _plc.WriteVariable(PlcVariableName.HandShake, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            string mex = "Scrittura Heartbeat  : Errore scrittura heartbeat  " + ex.Message;
                            ConsoleWriteOnEventError(mex);
                            _log.WriteLog(mex);
                        }
                    }
                    //Verifico se ci sono aggiornamenti nelle ricette sul database
                    #warning attivare recepy qui sotto    
                    CheckRecepyChange();

                }

            }
        }

        /// <summary>
        /// Check if there are any new items to log     
        /// </summary>
        /// <returns>return a bool where there was or not any item to save</returns>
        public bool CheckEndOfTheGame()
        {
            //Se viene rilevata la presenza di fine lavorazione, viene salvata
            //nel database, altrimenti non verrà eseguita nessuna azione

            bool wasTheGameEnded = false;
            //comunicazione con plc e lettura variabile di scambio segnale
            #region Lettura bit fine lavorazione
            try
            {
                //Controllo se è attiva la variabile di fine lavoro
                lock (_comunicationLock)
                {
                    if (!_plc.Active) _plc.Active = true;
                    wasTheGameEnded = (bool)_plc.ReadVariable(PlcVariableName.EndOfTheGame);
                }
            }
            catch (Exception ex)
            {
                string mex = $"CheckEndOfTheGame - Error reading varaible {PlcVariableName.EndOfTheGame} :" + ex.Message;
                ConsoleWriteOnEventError(mex);
                _log.WriteLog(mex);
            }
            #endregion

            //Se non c'è nessuna lavorazione finita, return
            if (!wasTheGameEnded) return false;
            //Altrimenti procedo alla lettura del pezzo finito
            else
            {
                productionLog p = new productionLog();
                string success = "";
                #region Lettura pezzo prodotto
                try
                {
                    lock (_comunicationLock)
                    {
                        //Riabilito la possibilità dello scarto su plc
                        _plc.WriteVariable(Classes.PlcVariableName.EndOfTheGame, false);


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

                        ////Debug test item
                        //p = new productionLog()
                        //{
                        //    OraLog = DateTime.Now,
                        //    CodiceArticolo = 100.18109.ToString(),
                        //    Lotto = "DX",
                        //    TempoCiclo = 490,
                        //    Waste = false, //Forzato a falso perche il pezzo appena finito non puo essere scarto
                        //    Stazione = 0,
                        //    Turno = 1,
                        //    IdLavorazione = 4
                        //};
                    }
                }
                catch (Exception ex)
                {
                    string mex = $"Errore lettura pezzo prodotto :" + ex.Message;
                    ConsoleWriteOnEventError(mex);
                    _log.WriteLog(mex);
                }
                #endregion


                #region Salvataggio database
                //salvataggio log nel database
                if (Database.WriteLog(p) == Database.SavingNewLogResult.Errore)
                {
                    ConsoleWriteOnEventError("Errore salvataggio Nuovo pezzo.");
                    _log.WriteLog("Errore salvataggio Nuovo pezzo.");
                    return false;
                }
                #endregion



                #region Creazione stringa resoconto lavorazione
                try
                {
                    TimeSpan durataCiclo = TimeSpan.FromSeconds((float)p.TempoCiclo / 10);
                    success += "\nNuovo pezzo prodotto :\n";
                    success += $"Data e Ora : {p.OraLog}" + Environment.NewLine;
                    success += $"Codice Articolo : {p.CodiceArticolo}" + Environment.NewLine;
                    success += $"Lotto : {p.Lotto}" + Environment.NewLine;
                    success += $"TempoCiclo : {durataCiclo.ToString(@"hh\:mm\:ss")}" + Environment.NewLine;
                    success += $"Stazione : {p.Stazione}" + Environment.NewLine;
                    success += $"Turno : {p.Turno}" + Environment.NewLine;

                }
                catch (Exception ex)
                {
                    string mex = $"Errore composizione Stringa per display su lavorazione eseguita :" + ex.Message;
                    ConsoleWriteOnEventError(mex);
                    _log.WriteLog(mex);
                }
                #endregion

                ConsoleWriteOnEventSuccess(success);


                #endregion

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
                    _log.WriteLog("Errore invio email Produzione : " + ex.Message);
                }

                #endregion

                ConsoleWriteOnEventSuccess("Controllo Fine Pezzo eseguito correttamente");
                return true;

            }
        }

        /// <summary>
        /// Controllo per richiesta Scarti
        /// </summary>
        public bool CheckForWaste()
        {
            //Se viene rilevata la presenza di fine lavorazione, viene salvata
            //nel database, altrimenti non verrà eseguita nessuna azione
            bool DoWeHaveAnyWaste_Left = false;
            bool DoWeHaveAnyWaste_Right = false;
            int QuantitaScartiDestra = 0;
            int QuantitaScartiSinistra = 0;


            #region Lettura PLC
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
            }
            catch (Exception ex)
            {
                ConsoleWriteOnEventError("Check for waste Error on plc tags reading: " + ex.Message);
                _log.WriteLog("Check for waste Error on plc tags reading: " + ex.Message);
                return false;
            }
            #endregion

            //se non è segnalato nessuno scarto return false, per indicare niente è avvenuto
            if (!DoWeHaveAnyWaste_Left && !DoWeHaveAnyWaste_Right) return false;
            //se invece è presente uno scarto


            else
            {
                //Lavorazione scartata da stazione sinistra
                if (DoWeHaveAnyWaste_Left)
                {
                    #region Restore PLC Waste Bool
                    try
                    {
                        _plc.WriteVariable(PlcVariableName.LastOneIsWasteLeft, false);
                    }
                    catch (Exception ex)
                    {
                        string mex = $"Check for waste - Error on reset tag {PlcVariableName.LastOneIsWasteLeft}, " + ex.Message;
                        ConsoleWriteOnEventError(mex);
                        _log.WriteLog(mex);
                        return false;
                    }
                    #endregion

                    #region Gestione scarto inferiore a 1
                    //se è stato premuto lo scarto ma il valore da scartare è minore di 1
                    if (QuantitaScartiSinistra < 1)
                    {
                        string mex = "Can not target waste when the number of wasted item on Left is less than 1";
                        ConsoleWriteOnEventWarning(mex);
                        _log.WriteLog(mex);
                        return false;
                    }
                    #endregion

                    #region Lettura Id lavorazione
                    int id;
                    try
                    {
                        lock (_comunicationLock)
                        {
                            id = Convert.ToInt32(_plc.ReadVariable(PlcVariableName.ContatoreLavorazioneSinistra));
                        }
                    }
                    catch (Exception)
                    {
                        string txt = $"CheckWaste : error on reading item id, tag {PlcVariableName.ContatoreLavorazioneSinistra}";
                        ConsoleWriteOnEventWarning(txt);
                        _log.WriteLog(txt);
                        return false;
                    }
                    #endregion

                    #region Forloop sub recepy
                    for (int i = 0; i < QuantitaScartiSinistra; i++)
                    {
                        // Eseguo la funzione di sub recepy, e controllo lo stato del risultato
                        switch (Database.SubRecepyNumber(PlcVariableName.StazioneSaldatrice.Sinistra, id))
                        {
                            case Database.SavingNewWaste.Errore:
                                string m1 = $"Check for waste - Error on subrecepyNumber.";
                                ConsoleWriteOnEventError(m1);
                                _log.WriteLog(m1);
                                break;
                            case Database.SavingNewWaste.LogNonTrovato:
                                string m2 = $"Check for waste - Error on subrecepyNumber.Log not found ";
                                ConsoleWriteOnEventError(m2);
                                _log.WriteLog(m2);
                                break;
                            case Database.SavingNewWaste.SoloLogAggiornato:
                                string m3 = $"Check for waste - Aggiornato solo il log, ricetta non trovata";
                                ConsoleWriteOnEventWarning(m3);
                                _log.WriteLog(m3);
                                break;
                            case Database.SavingNewWaste.SalvatoEAggiorantaTabellaOrdini:
                                string m4 = $"Check for waste - Aggiornamento log e ricetta avventuo correttamente";
                                ConsoleWriteOnEventSuccess(m4);
                                _log.WriteLog(m4);
                                break;
                        }
                    }
                    #endregion

                    return true;
                }

                //Lavorazione scartata da stazione destra
                else
                {
                    #region Restore PLC Waste Bool
                    try
                    {
                        _plc.WriteVariable(PlcVariableName.LastOneIsWasteRight, false);
                    }
                    catch (Exception ex)
                    {
                        string mex = $"Check for waste - Error on reset tag {PlcVariableName.LastOneIsWasteRight}, " + ex.Message;
                        ConsoleWriteOnEventError(mex);
                        _log.WriteLog(mex);
                        return false;
                    }
                    #endregion

                    #region Gestione scarto inferiore a 1
                    //se è stato premuto lo scarto ma il valore da scartare è minore di 1
                    if (QuantitaScartiDestra < 1)
                    {
                        string mex = "Can not target waste when the number of wasted item on Right id less than 1";
                        ConsoleWriteOnEventWarning(mex);
                        _log.WriteLog(mex);
                        return false;
                    }
                    #endregion

                    #region Lettura Id lavorazione
                    int id;
                    try
                    {
                        lock (_comunicationLock)
                        {
                            id = Convert.ToInt32(_plc.ReadVariable(PlcVariableName.ContatoreLavorazioneDestra));
                        }
                    }
                    catch (Exception)
                    {
                        string txt = $"CheckWaste : error on reading item id, tag {PlcVariableName.ContatoreLavorazioneDestra}";
                        ConsoleWriteOnEventWarning(txt);
                        _log.WriteLog(txt);
                        return false;
                    }
                    #endregion

                    #region Forloop sub recepy
                    for (int i = 0; i < QuantitaScartiDestra; i++)
                    {
                        // Eseguo la funzione di sub recepy, e controllo lo stato del risultato
                        switch (Database.SubRecepyNumber(PlcVariableName.StazioneSaldatrice.Destra, id))
                        {
                            case Database.SavingNewWaste.Errore:
                                string m1 = $"Check for waste - Error on subrecepyNumber.";
                                ConsoleWriteOnEventError(m1);
                                _log.WriteLog(m1);
                                break;
                            case Database.SavingNewWaste.LogNonTrovato:
                                string m2 = $"Check for waste - Error on subrecepyNumber.Log not found ";
                                ConsoleWriteOnEventError(m2);
                                _log.WriteLog(m2);
                                break;
                            case Database.SavingNewWaste.SoloLogAggiornato:
                                string m3 = $"Check for waste - Aggiornato solo il log, ricetta non trovata";
                                ConsoleWriteOnEventWarning(m3);
                                _log.WriteLog(m3);
                                break;
                            case Database.SavingNewWaste.SalvatoEAggiorantaTabellaOrdini:
                                string m4 = $"Check for waste - Aggiornamento log e ricetta avventuo correttamente";
                                ConsoleWriteOnEventSuccess(m4);
                                _log.WriteLog(m4);
                                break;
                        }
                    }
                    #endregion

                    return true;
                }

            }
        }

        /// <summary>
        /// La funzione CheckRecepyChange scrive le ricette lette dal database, sul PLC
        /// </summary>
        public void CheckRecepyChange()
        {

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


        #region async Launcher
        public void AsyncHeartBeat()
        {

            Thread asyncBeater = new Thread(HeartBeat);
            asyncBeater.IsBackground = true;
            asyncBeater.Start();

        }
        #endregion

    }
}