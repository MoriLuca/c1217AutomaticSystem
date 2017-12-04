using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner.Classes
{
    public static class PlcVariableName
    {
        //variabile per lo scambio del segnale boleano per la presenza della
        //connessione con il dispositivo
        public static  string HandShake { get; } = "HandShake";

        // Stringhe -- variabili contenenti il codice articolo
        public static string CodiceArticolo_1 { get; } = "ProduzionePezziDaServer[1].CodArticolo";
        public static string CodiceArticolo_2 { get; } = "ProduzionePezziDaServer[2].CodArticolo";
        public static string CodiceArticolo_3 { get; } = "ProduzionePezziDaServer[3].CodArticolo";
        public static string CodiceArticolo_4 { get; } = "ProduzionePezziDaServer[4].CodArticolo";


        //Interi -- numero di pezzi da eseguire
        public static string NumeroPezzi_1 { get; } = "ProduzionePezziDaServer[1].NumeroPezzi";
        public static string NumeroPezzi_2 { get; } = "ProduzionePezziDaServer[2].NumeroPezzi";
        public static string NumeroPezzi_3 { get; } = "ProduzionePezziDaServer[3].NumeroPezzi";
        public static string NumeroPezzi_4 { get; } = "ProduzionePezziDaServer[4].NumeroPezzi";

    }
}
