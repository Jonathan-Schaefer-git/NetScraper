using MongoDB.Driver.Linq;
using System.Threading.Tasks;


namespace NetScraper
{
	internal static class CoreHandler
	{
		public static DateTime StartedScraping = DateTime.Now;
		public static int BatchLimit = 20000;
		public static int Batch = 0;
		public static long Scrapes = 0;
		public static int SimultaneousPool = 100;
		public static bool Shouldrun = true;
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileName = Path.Combine(filepath, "log.json");
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
					Task<bool>[] resettasks = new Task<bool>[] { PostgreSQL.ResetOutstandingAsync(), PostgreSQL.ResetMainDataAsync(), PostgreSQL.ResetPrioritisedAsync() };
					await Task.WhenAll(resettasks);
					Console.WriteLine("Reset Tables");
					break;

				case "start":
					Console.WriteLine();
					break;
			}

			List<string> list = new List<string>();
			list.Add("https://www.wikipedia.org/");
			list.Add("https://de.wikipedia.org/");
			list.Add("https://got.wikipedia.org/");
			var pushstate = await PostgreSQL.PushOutstandingAsync(list);
			Console.WriteLine("State of start link push: " + pushstate);
			StartedScraping = DateTime.Now;
			await LogWriter.WriteSettingsJsonAsync();

			await RunScraper();
		}

		private static async Task RunScraper()
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
				outstanding.AddRange(await PostgreSQL.GetPrioritisedAsync());
				outstanding.AddRange(await PostgreSQL.GetOutstandingAsync(BatchLimit - outstanding.Count()));
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
						var documents = await Scraping(list);

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

					var pushtask = TriggerPushLinksAsync(outstandingLinksnd, prioritisedLinksnd);
					var getscrapecount = PostgreSQL.GetScrapingCount();
					Console.WriteLine("Found {0} Links", outstandingLinksnd.Count);
					Console.WriteLine("#Prioritised Links {0}", prioritisedLinksnd.Count);

					var state = await pushtask;
					Scrapes = await getscrapecount;
					Batch++;
					await LogWriter.WriteSettingsJsonAsync();
					
				}
				else
				{
					Console.WriteLine("No links to Scrap");
				}
				
			}
		}
		/*
				var docs = new List<Document>();
				foreach (var link in outstanding)
				{
					Console.WriteLine("Checkpoint A");
					try
					{
						docs.Add(await Scraper.ScrapFromLinkAsync(link));
					}
					catch (Exception)
					{
						throw;
					}
					
					Console.WriteLine("Checkpoint B");
				}
				Console.WriteLine("Worked");
				*/
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