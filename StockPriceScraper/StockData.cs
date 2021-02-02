using System;

namespace StockPriceScraper
{
    class StockData
    {
        public long Ticks { get; set; }

        public string Name { get; set; }

        public string Wkn { get; set; }

        public double PriceEUR { get; set; }

        public double PriceUSD { get; set; }

        public StockData(string stockName, string stockWkn, double stockPriceEur, double stockPriceUsd)
        {
            Ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            Name = stockName;
            Wkn = stockWkn;
            PriceEUR = stockPriceEur;
            PriceUSD = stockPriceUsd;
            // set length of maxName for the toString
        }

        public string ToString(int namePadding)
        {
            string s = $"Stock: {Name.PadLeft(namePadding)} (WKN: {Wkn})";
            s += $"{PriceEUR,10} EUR / ";
            s += $"{PriceUSD,10} USD";

            return s;
        }
    }
}
