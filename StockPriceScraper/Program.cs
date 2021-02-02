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

namespace StockPriceScraper
{
    /// <summary>
    /// This class reads the Stockprices from a configured website and saves the data into a csv file
    /// </summary>
    class Program
    {

        // Items from the configuration file
        private static string stockBaseUrl = string.Empty;
        private static string eurXpath = string.Empty;
        private static string usdXpath = string.Empty;
        private static string outputfilePath = string.Empty;
        private static string stockNameXpath = string.Empty;
        private static string wknXpath = string.Empty;

        // Array with stock Names
        private static readonly List<string> stockNames = new();

        // List of Stock objects containing webscraped data
        private static readonly List<StockData> lstStocks = new();


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
            var success = true;

            try
            {
                // setup stream to read file
                var xmldoc = new XmlDocument();
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
                    var stockName = stock.InnerText;
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
            var websiteUrls = new List<string>();
            var tasks = new List<Task<StockData>>();

            // create list of urls to download
            foreach (string stockName in stockNames)
            {
                var stockUrl = String.Format(stockBaseUrl, stockName);
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
            using var client = new WebClient();

            var htmlContent = client.DownloadString(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // taking the inner text and replacing some not wanted characters
            var stockNameNode = htmlDoc.DocumentNode.SelectSingleNode(stockNameXpath);
            // do some cleanup - might not work for others...
            var stockName = stockNameNode.InnerText.Replace("&nbsp;", "");

            // extracting the WKN number from the attribute of this node
            var stockWknNode = htmlDoc.DocumentNode.SelectSingleNode(wknXpath);
            var stockWkn = stockWknNode.GetAttributeValue("cpval", "NotFound");

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
            var stock = new StockData(stockName, stockWkn, stockPriceEur, stockPriceUsd);
            Console.WriteLine("downloaded data from: " + url);
            return stock;
        }

        /// <summary>
        /// basic output of the gathered stock data
        /// </summary>
        static void printAllStockData()
        {
            var maxLenghtOfName = 0;

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
}