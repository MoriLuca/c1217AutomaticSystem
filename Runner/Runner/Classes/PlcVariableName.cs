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
        public static string LastOneIsWaste { get; } = "ScartoUltimoPezzo";
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

        #endregion

        #region Plc 2 Server
        //Struttura dati 
        public struct DataToLog
        {
            public static string CodiceArticolo { get; } = "ProduzionePezziAServer.CodArticolo";
            public static string NumeroPezziProdotti { get; } = "ProduzionePezziAServer.NumeroPezzi";
            public static string TempoCiclo { get; } = "ProduzionePezziAServer.CodArticolo";
            public static string Lotto { get; } = "ProduzionePezziAServer.Lotto";
        }
        #endregion
    }
}
