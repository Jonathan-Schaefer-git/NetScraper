using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;


namespace NetScraper
{
	internal static class CoreHandler
	{
		public static DateTime StartedScraping = DateTime.Now;
		public static int BatchLimit = 10000;
		public static int Batch = 0;
		public static long Scrapes = 0;
		public static int SimultaneousPool = 50;
		public static bool Shouldrun = true;
		public static string ConnectionString = "Host=localhost;Username=postgres;Password=1598;Database=Netscraper";
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileBuffer = Path.Combine(filepath, "links.json");
		public static string fileSettings = Path.Combine(filepath, "settings.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");

		private static async Task Main()
		{
			Console.WriteLine(".NETScraper developed by Jona4Dev");
			Console.WriteLine("Reading Settings from {0}", fileSettings);
			var settings = await LogWriter.LoadJsonAsync();
			Batch = settings.BatchesCompleted;
			Console.WriteLine("https://github.com/Jona4Play/NetScraper");
			Console.WriteLine("=========================================");
			Console.WriteLine("Type 'help' to get the list of commands");
			var startlinks = new List<string>();
			while(true)
			{
				Console.WriteLine("=========================================");
				Console.WriteLine("Enter a Command: ");
				Console.Write(">");
				var input = Console.ReadLine();

				switch (input)
				{
					default:
						Console.WriteLine("Unknown Command use 'help' to get the list of valid commands");
						break;

					case "help":
						Console.WriteLine("new - Reset all tables and start scraping from default stack");
						Console.WriteLine("start - Continue from existing stack");
						Console.WriteLine("help - Supply this menu");
						break;

					case "start":
						Console.WriteLine();
						StartedScraping = DateTime.Now;
						await LogWriter.WriteSettingsJsonAsync();
						var state = await RunScraper(startlinks);
						if (state)
						{
							Console.WriteLine("Scraping was stopped through interaction with the settings file");
						}
						break;
					case "load":
						Console.WriteLine("Loading Links from file");
						startlinks = await LogWriter.ReadLinkBuffer();
						Console.WriteLine("Loaded {0} links", startlinks.Count);
						break;

					case "setup":
						var writestate = await LogWriter.WriteLinkBuffer(startlinks);
						Console.WriteLine("Writing to file state: {0}",writestate);
						break;
					case "init":
						Console.WriteLine("Supplying Program with start links");
						startlinks.Add("https://www.wikipedia.org/");
						startlinks.Add("https://de.wikipedia.org/");
						startlinks.Add("https://got.wikipedia.org/");
						startlinks.Add("https://play.google.com/store/apps/details?id=org.wikipedia&referrer=utm_source%3Dportal%26utm_medium%3Dbutton%26anid%3Dadmob");
						break;

					case "reset":
						Console.WriteLine("Reseting Database");
						Task<bool>[] resettasks = new Task<bool>[] { PostgreSQL.ResetOutstandingAsync(), PostgreSQL.ResetMainDataAsync(), PostgreSQL.ResetPrioritisedAsync() };
						await Task.WhenAll(resettasks);
						Console.WriteLine("Reset Tables");
						break;
				}
			}
		}

		private static async Task<bool> RunScraper(List<string> startlinks)
		{
			//Called RunScraper
			Console.WriteLine("Called Scraping Method");

			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();
			List<string> outstanding = new List<string>();
			while (Shouldrun)
			{
				Console.WriteLine("Started Cycle");
				//Get Links to be scraped from DB up to a maximum of 20k strings per batch
				outstanding.AddRange(startlinks);
				outstanding.AddRange(prioritisedLinks);
				
				Console.WriteLine("Outstanding Count: " + outstanding.Count());

				//Check settings (Used for web interface)
				var setting = await LogWriter.LoadJsonAsync();
				Shouldrun = setting.ShouldRun;
				SimultaneousPool = setting.SimultaneousPool;

				Console.WriteLine("Concurreny Pool is: " + SimultaneousPool);
				
				if (outstanding != null)
				{
					var partionedLists = outstanding.Partition(SimultaneousPool);
					Console.WriteLine("Partitioned Lists: " + partionedLists.Count());
					foreach (var list in partionedLists)
					{
						var timer = Stopwatch.StartNew();
						var documents = await Scraping(list);
						timer.Stop();
						Console.WriteLine("Scraping took: " + timer.ElapsedMilliseconds + "ms");
						foreach (var document in documents)
						{
							if (document.Links is not null)
							{
								outstandingLinks.AddRange(document.Links);
							}
							if (document.PrioritisedLinks is not null)
							{
								prioritisedLinks.AddRange(document.PrioritisedLinks);
							}
						}

						var documentstate = await PostgreSQL.PushDocumentListAsync(documents);
						Console.WriteLine("The Documents were pushed: " + documentstate);
					}

					foreach (var link in outstanding)
					{
						outstandingLinks.Remove(link);
						prioritisedLinks.Remove(link);
					}

					List<string> outstandingLinksnd = new HashSet<string>(outstandingLinks).ToList();
					List<string> prioritisedLinksnd = new HashSet<string>(prioritisedLinks).ToList();

					
					if (prioritisedLinksnd.Count > BatchLimit)
					{
						prioritisedLinksnd.RemoveRange(BatchLimit, prioritisedLinksnd.Count - BatchLimit);
					}
					else
					{
						if (outstandingLinksnd.Count > BatchLimit)
						{
							prioritisedLinksnd.AddRange(outstandingLinksnd.GetRange(prioritisedLinksnd.Count, BatchLimit - prioritisedLinksnd.Count));
						}
					}
					Console.WriteLine("Link Count for this batch: " + prioritisedLinksnd.Count);
					var getscrapecount = PostgreSQL.GetScrapingCount();
					Console.WriteLine("Found {0} Links", outstandingLinksnd.Count);
					Console.WriteLine("Found {0} Prioritised Links", prioritisedLinksnd.Count);

					prioritisedLinks = prioritisedLinksnd;

					Scrapes = await getscrapecount;
					Batch++;
					await LogWriter.WriteSettingsJsonAsync();
					var writestate = await LogWriter.WriteLinkBuffer(prioritisedLinksnd);
					if (writestate)
					{
						Console.WriteLine("Writing buffer was successful");
					}
					else
					{
						Console.WriteLine("Writing buffer failed");
					}
				}
				else
				{
					Console.WriteLine("No links to Scrap");
				}
			}
			return true;
		}
		
		private static async Task<List<Document>> Scraping(IEnumerable<string> links)
		{
			var tasks = links.Select(x => Task.Run(() => Scraper.ScrapFromLinkAsync(x))).ToArray();
			await Task.WhenAll(tasks);

			var documents = new List<Document>();
			
			foreach (var item in tasks)
			{
				documents.Add(item.Result);
			}
			return documents;
		}

		private static async Task<bool> TriggerPushLinksAsync(List<string> outstandingLinks, List<string> prioritisedLinks)
		{
			Task<bool>[] tasks = new Task<bool>[2];
			tasks[0] = PostgreSQL.PushOutstandingAsync(outstandingLinks);
			tasks[1] = PostgreSQL.PushPrioritisedAsync(prioritisedLinks);
			await Task.WhenAll(tasks);
			
			if (tasks[0].Result == true && tasks[1].Result == true)
			{
				Console.WriteLine("Links were pushed successfully");
				return true;
			}
			else
			{
				Console.WriteLine("[Error]: Links weren't pushed successfully");
				return false;
			}
		}

		public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
		{
			for (int i = 0; i < Math.Ceiling(source.Count / (Double)size); i++)
				yield return new List<T>(source.Skip(size * i).Take(size));
		}
	}
}