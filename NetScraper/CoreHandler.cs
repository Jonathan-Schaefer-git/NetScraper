using MongoDB.Driver;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NetScraper
{
	internal static class CoreHandler
	{
		public static int BatchLimit = 20000;
		public static int Batch = 0;
		public static long Scrapes = 0;
		public static int SimultaneousPool = 100;
		public static bool Shouldrun = true;
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileName = Path.Combine(filepath, "log.json");
		public static string fileSettings = Path.Combine(filepath, "settings.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");

		private static void Main()
		{
			Console.WriteLine(".NETScraper developed by Jona4Dev");
			Console.WriteLine("Loading settings from {0}", fileSettings);
			LogWriter.WriteSettingsJson(DateTime.Now);
			Console.WriteLine("https://github.com/Jona4Play/NetScraper");
			Console.WriteLine("=========================================");
			Console.WriteLine("Type 'help' to get the list of commands");
			var input = Console.ReadLine();
			
			switch (input)
			{
				default:
					Console.WriteLine("Unknown Command use 'help' to get the list of valid commands");
					break;

				case "help":
					Console.WriteLine();
					break;
				case "new":
					Console.WriteLine("Rerolling Outstanding");
					PostgreSQL.ResetOutstanding();
					PostgreSQL.ResetMainData();
					PostgreSQL.ResetPrioritised();
					break;

				case "start":
					Console.WriteLine();
					break;
			}

			List<string> list = new List<string>();
			list.Add("https://sn.wikipedia.org/wiki/");
			PostgreSQL.PushOutstanding(list);
			

			RunScraper();
		}

		private static void RunScraper()
		{
			//Called RunScraper
			Console.WriteLine("Called Scraping Method");
			List<Document> documents = new List<Document>();
			List<string> jsonStrings = new List<string>();
			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();
			List<string> outstanding = new List<string>();
			
			outstanding.AddRange(PostgreSQL.GetPrioritised());
			Console.WriteLine(outstanding.Count());
			outstanding.AddRange(PostgreSQL.GetOutstanding(BatchLimit - outstanding.Count));
			var setting = LogWriter.LoadJson();
			Shouldrun = setting.ShouldRun;
			SimultaneousPool = setting.SimultaneousPool;
			if (Shouldrun && outstanding != null)
			{
				foreach (var site in outstanding)
				{
					var watch = Stopwatch.StartNew();
					var w = Scraper.ScrapFromLink(site);
					watch.Stop();
					Console.WriteLine("Approximate Size for {0} is {1} and took {2}", w.Absoluteurl, w.ApproxByteSize, watch.ElapsedMilliseconds);
					if (w.Links != null)
					{
						outstandingLinks.AddRange(w.Links);
					}
					else
					{
						Console.WriteLine("No Links at this site {0}", w.Absoluteurl);
					}
					if (w.PrioritisedLinks != null)
					{
						prioritisedLinks.AddRange(w.PrioritisedLinks);
					}
					else
					{
						Console.WriteLine("No Prioritised Links at this site {0}", w.Absoluteurl);
					}
					documents.Add(w);
				}

				foreach (var link in outstanding)
				{
					outstandingLinks.Remove(link);
					prioritisedLinks.Remove(link);
				}
				foreach (var item in prioritisedLinks)
				{
					Console.WriteLine(item);
				}
				outstandingLinks.Distinct();
				prioritisedLinks.Distinct();
				Console.WriteLine("Found {0} Links",outstandingLinks.Count);
				Console.WriteLine("#Prioritised Links {0}", prioritisedLinks.Count);
				PostgreSQL.PushOutstanding(outstandingLinks);
				PostgreSQL.PushPrioritised(prioritisedLinks);
				PostgreSQL.PushDocumentList(documents);
				Batch++;
				Scrapes = PostgreSQL.GetScrapingCount();
			}
			else
			{
				Console.WriteLine("Shouldn't run or no Links to Scrap");
			}
		}
		private static void AsyncPushDocument()
		{

		}
	}
}