using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner.Classes
{
    public static class PlcVariableName
    {
        #region PlcBooleansSignals
        //variabile per lo scambio del segnale boleano per la presenza della
        //connessione con il dispositivo
        public static string HandShake { get; } = "HandShake";

        //Trigger di fine lavorazione di un singolo paraurti
        public static string EndOfTheGame { get; } = "TriggerSalvataggioDati";

        //Trigger di scarto, l'ultima lavorazione conlusa, verrà segnata come scarto
        public static string LastOneIsWasteRight { get; } = "ScartoUltimoPezzoDx";
        public static string LastOneIsWasteLeft { get; } = "ScartoUltimoPezzoSx";
        public static string QuantitaScartiDestra { get; } = "QuantitaScartiDx";
        public static string QuantitaScartiSinistra { get; } = "QuantitaScartiSx";

        public static string ContatoreLavorazioneDestra { get; } = "ContatoreLavorazioneDx";
        public static string ContatoreLavorazioneSinistra { get; } = "ContatoreLavorazioneSx";

        public static string ProduzioneUltimoCodice1 { get; } = "ProduzioneTotlaeUltimoCodice1";
        public static string Ora_Inizio_Turno_1 { get; } = "Ora_Inizio_Turno_1";

        public static string pathReportGlobale1 { get; } = "ReportGlobale1";
        public static string pathReportGlobale2 { get; } = "ReportGlobale2";
        public static string pathReportGiornate1 { get; } = "ReportGiornate1";
        public static string pathReportGiornate2 { get; } = "ReportGiornate2";

        public static string Giorno { get; } = "Giorno";
        public static string TotaleProduzione { get; } = "TotaleProduzione";
        public static string TotaleBuoni { get; } = "TotaleBuoni";
        public static string TotaleScarti { get; } = "TotaleScarti";
        public static string TotaleTurno1 { get; } = "TotaleTurno1";
        public static string TotaleTurno2 { get; } = "TotaleTurno2";
        public static string TotaleTurno3 { get; } = "TotaleTurno3";
        public static string TotaleBuoniTurno1 { get; } = "TotaleBuoniTurno1";
        public static string TotaleBuoniTurno2 { get; } = "TotaleBuoniTurno2";
        public static string TotaleBuoniTurno3 { get; } = "TotaleBuoniTurno3";
        public static string TotaleScartiTurno1 { get; } = "TotaleScartiTurno1";
        public static string TotaleScartiTurno2 { get; } = "TotaleScartiTurno2";
        public static string TotaleScartiTurno3 { get; } = "TotaleScartiTurno3";

        #endregion

        #region enum
        public enum StazioneSaldatrice
        {
            Destra = 0,
            Sinistra = 1
        }
        #endregion

        #region Server 2 Plc

        // Stringhe -- variabili contenenti il codice articolo
        public static List<string> CodiceArticoli = new List<string>()
        {
            "ProduzionePezziDaServer[1].CodArticolo",
            "ProduzionePezziDaServer[2].CodArticolo",
            "ProduzionePezziDaServer[3].CodArticolo",
            "ProduzionePezziDaServer[4].CodArticolo",
            "ProduzionePezziDaServer[5].CodArticolo",
            "ProduzionePezziDaServer[6].CodArticolo",
            "ProduzionePezziDaServer[7].CodArticolo",
            "ProduzionePezziDaServer[8].CodArticolo",
        };

        // Stringhe -- variabili contenenti il codice articolo
        public static List<string> Lotti = new List<string>()
        {
            "ProduzionePezziDaServer[1].Lotto",
            "ProduzionePezziDaServer[2].Lotto",
            "ProduzionePezziDaServer[3].Lotto",
            "ProduzionePezziDaServer[4].Lotto",
            "ProduzionePezziDaServer[5].Lotto",
            "ProduzionePezziDaServer[6].Lotto",
            "ProduzionePezziDaServer[7].Lotto",
            "ProduzionePezziDaServer[8].Lotto",
        };

        //Interi -- numero di pezzi da eseguire
        public static List<string> NumeroPezzi = new List<string>()
        {
            "ProduzionePezziDaServer[1].NumeroPezzi",
            "ProduzionePezziDaServer[2].NumeroPezzi",
            "ProduzionePezziDaServer[3].NumeroPezzi",
            "ProduzionePezziDaServer[4].NumeroPezzi",
            "ProduzionePezziDaServer[5].NumeroPezzi",
            "ProduzionePezziDaServer[6].NumeroPezzi",
            "ProduzionePezziDaServer[7].NumeroPezzi",
            "ProduzionePezziDaServer[8].NumeroPezzi",
        };

        //Interi -- numero di pezzi da eseguire
        public static List<string> NumeroPezziAttuale = new List<string>()
        {
            "ProduzionePezziDaServer[1].NumeroPezziAttuale",
            "ProduzionePezziDaServer[2].NumeroPezziAttuale",
            "ProduzionePezziDaServer[3].NumeroPezziAttuale",
            "ProduzionePezziDaServer[4].NumeroPezziAttuale",
            "ProduzionePezziDaServer[5].NumeroPezziAttuale",
            "ProduzionePezziDaServer[6].NumeroPezziAttuale",
            "ProduzionePezziDaServer[7].NumeroPezziAttuale",
            "ProduzionePezziDaServer[8].NumeroPezziAttuale",
        };

        #endregion

        #region Plc 2 Server
        //Struttura dati 
        public struct DataToLog
        {
            public static string CodiceArticolo { get; } = "ProduzionePezziAServer.CodArticolo";
            public static string NumeroPezziProdotti { get; } = "ProduzionePezziAServer.NumeroPezzi";
            public static string TempoCiclo { get; } = "ProduzionePezziAServer.TempoCiclo";
            public static string Lotto { get; } = "ProduzionePezziAServer.Lotto";
            public static string Stazione { get; } = "ProduzionePezziAServer.Stazione"; //destra o sinistra
            public static string Turno { get; } = "ProduzionePezziAServer.Turno";
            public static string IdLavorazione { get; } = "ProduzionePezziAServer.IdLavorazione";
        }
        #endregion
    }
}
