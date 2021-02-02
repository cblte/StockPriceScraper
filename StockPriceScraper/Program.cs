using CsvHelper;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;


// Using http://xpather.com/ to find the correct xpath for each of the elements

namespace stockPriceScraper
{
    /// <summary>
    /// This class reads the Stockprices from a configured website and saves the data into a csv file
    /// </summary>
    class Program
    {

        // Items from the configuration file
        private static string stockBaseUrl = "";
        private static string eurXpath = "";
        private static string usdXpath = "";
        private static string outputfilePath = "";
        private static string stockNameXpath = "";
        private static string wknXpath;

        // Array with stock Names
        static List<string> stockNames = new List<string>();

        // List of Stock objects containing webscraped data
        static List<StockData> lstStocks = new List<StockData>();


        /// <summary>
        /// Main Method which starts it all! ;-)
        /// </summary>
        /// <param name="args">Command Line Parameters</param>
        static async Task Main(string[] args)
        {
            // read config file stock-config.xml
            Console.WriteLine("Reading configuration file.");
            if (!ReadConfigurationFile())
            {
                Console.WriteLine("Bad config file. Exiting.");
                System.Environment.Exit(1);
            }
            Console.WriteLine("Starting downloads.");
            await RunDownloadStockDataAsync();
            printAllStockData();
            WriteDataToCsv();

        }

        /// <summary>
        /// Reads data from the configuration file
        /// </summary>
        /// <returns>returns True if config file could be read, False if not</returns>
        static bool ReadConfigurationFile()
        {
            bool success = true;

            try
            {
                // setup stream to read file
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(@"stock-config.xml");

                // reading config
                stockBaseUrl = xmldoc.DocumentElement.SelectSingleNode("/config/basics/base-url").InnerText;
                eurXpath = xmldoc.DocumentElement.SelectSingleNode("/config/basics/eur-xpath").InnerText;
                usdXpath = xmldoc.DocumentElement.SelectSingleNode("/config/basics/usd-xpath").InnerText;
                stockNameXpath = xmldoc.DocumentElement.SelectSingleNode("/config/basics/stockname-xpath").InnerText;
                wknXpath = xmldoc.DocumentElement.SelectSingleNode("/config/basics/wkn-xpath").InnerText;
                outputfilePath = xmldoc.DocumentElement.SelectSingleNode("/config/basics/output-file").InnerText;

                // reading stocks
                XmlNodeList stocks = xmldoc.DocumentElement.SelectNodes("/config/stocks/stock");
                foreach (XmlNode stock in stocks)
                {
                    string stockName = stock.InnerText;
                    stockNames.Add(stockName);
                }
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e.Message);
            }

            return success;
        }

        /// <summary>
        /// Capsule Method to measure the time for the downloads.
        /// Not a bad thing actually, because we dont want the main method
        /// to be async :)
        /// </summary>
        static async Task RunDownloadStockDataAsync()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // wait here for all downloads to be finished
            await DownloadWebsitesAsync();
            watch.Stop();
            var elapsed = watch.ElapsedMilliseconds;
            Console.WriteLine($"Total execution time async: {elapsed}");
        }

        static async Task DownloadWebsitesAsync()
        {
            List<string> websiteUrls = new List<string>();
            List<Task<StockData>> tasks = new List<Task<StockData>>();

            // create list of urls to download
            foreach (string stockName in stockNames)
            {
                string stockUrl = String.Format(stockBaseUrl, stockName);
                tasks.Add(Task.Run(() => DownloadWebsite(stockUrl)));
            }

            // wait for all tasks to be finished
            var results = await Task.WhenAll(tasks);

            foreach (var stock in results)
            {
                lstStocks.Add(stock);
            }

        }

        /// <summary>
        /// Downloads the website and extracts the stock data from it.
        /// </summary>
        /// <param name="url">the url to download</param>
        static StockData DownloadWebsite(string url)
        {
            WebClient client = new WebClient();
            string htmlContent = client.DownloadString(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // taking the inner text and replacing some not wanted characters
            var stockNameNode = htmlDoc.DocumentNode.SelectSingleNode(stockNameXpath);
            // do some cleanup - might not work for others...
            string stockName = stockNameNode.InnerText.Replace("&nbsp;", "");

            // extracting the WKN number from the attribute of this node
            var stockWknNode = htmlDoc.DocumentNode.SelectSingleNode(wknXpath);
            string stockWkn = stockWknNode.GetAttributeValue("cpval", "NotFound");

            // gathering the prices in EUR and USD 
            var stockPriceEurNode = htmlDoc.DocumentNode.SelectSingleNode(eurXpath);
            var stockPriceUsdNode = htmlDoc.DocumentNode.SelectSingleNode(usdXpath);

            double stockPriceUsd = 0.0;
            double stockPriceEur = 0.0;

            if (stockPriceEurNode.InnerHtml.Contains("EUR"))
            {
                stockPriceEur = Double.Parse(stockPriceEurNode.FirstChild.InnerText);
            }
            if (stockPriceUsdNode.InnerHtml.Contains("USD"))
            {
                stockPriceUsd = Double.Parse(stockPriceUsdNode.FirstChild.InnerText);
            }

            // creating a Stock object to save in the list
            StockData stock = new StockData(stockName, stockWkn, stockPriceEur, stockPriceUsd);
            Console.WriteLine("downloaded data from: " + url);
            return stock;
        }

        /// <summary>
        /// basic output of the gathered stock data
        /// </summary>
        static void printAllStockData()
        {
            int maxLenghtOfName = 0;

            Console.WriteLine("Gathered Stock Data");
            Console.WriteLine("-------------------");

            // find longest name
            foreach (var stock in lstStocks)
            {
                if (stock.Name.Length > maxLenghtOfName)
                {
                    maxLenghtOfName = stock.Name.Length;
                }
            }

            foreach (var stock in lstStocks)
            {
                Console.WriteLine(stock.ToString(maxLenghtOfName));
            }
        }

        /// <summary>
        /// simple csvwriter which writes all data to disk
        /// </summary>
        private static void WriteDataToCsv()
        {
            // we are using "using" here because we only need the writer for 
            // a short time and can then 'dispose' it
            using var writer = new StreamWriter(outputfilePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(lstStocks);
        }
    }

    class StockData
    {
        public long Ticks { get; set; }
        public string Name { get; set; }
        public string wkn { get; set; }
        public double priceEUR { get; set; }
        public double priceUSD { get; set; }

        public StockData(string stockName, string stockWkn, double stockPriceEur, double stockPriceUsd)
        {
            Ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            Name = stockName;
            wkn = stockWkn;
            priceEUR = stockPriceEur;
            priceUSD = stockPriceUsd;
            // set length of maxName for the toString
        }

        public string ToString(int namePadding)
        {
            string s = $"Stock: {Name.PadLeft(namePadding)} (WKN: {wkn})";
            s += $"{priceEUR.ToString().PadLeft(10)} EUR / ";
            s += $"{priceUSD.ToString().PadLeft(10)} USD";

            return s;
        }
    }

}