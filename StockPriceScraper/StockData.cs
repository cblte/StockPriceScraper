using System;

namespace StockPriceScraper
{
    /// <summary>
    /// Holds stock data
    /// </summary>
    public class StockData
    {
        /// <summary>
        /// Ticks
        /// </summary>
        public long Ticks { get; }

        /// <summary>
        /// Stock name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Stock WKN
        /// </summary>
        public string Wkn { get; }

        /// <summary>
        /// Stock price in EUR
        /// </summary>
        public double PriceEUR { get; }

        /// <summary>
        /// Stock price in USD
        /// </summary>
        public double PriceUSD { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StockData"/> class.
        /// </summary>
        /// <param name="stockName">Stock name</param>
        /// <param name="stockWkn">Stock WKN</param>
        /// <param name="stockPriceEur">Stock price in EUR</param>
        /// <param name="stockPriceUsd">Stock price in USD</param>
        public StockData(string stockName, string stockWkn, double stockPriceEur, double stockPriceUsd)
        {
            Ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            Name = stockName;
            Wkn = stockWkn;
            PriceEUR = stockPriceEur;
            PriceUSD = stockPriceUsd;
            // set length of maxName for the toString
        }

        /// <summary>
        /// Prints the instances values with padding
        /// </summary>
        /// <param name="namePadding">Size of padding</param>
        /// <returns></returns>
        public string ToString(int namePadding)
        {
            string s = $"Stock: {Name.PadLeft(namePadding)} (WKN: {Wkn})";
            s += $"{PriceEUR,10} EUR / ";
            s += $"{PriceUSD,10} USD";

            return s;
        }
    }
}
