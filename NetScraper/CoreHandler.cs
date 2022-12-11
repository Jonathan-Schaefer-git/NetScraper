using MongoDB.Driver;

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

		private static async void Main()
		{
			Console.WriteLine(".NETScraper developed by Jona4Dev");
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
					await PostgreSQL.ResetOutstandingAsync();
					await PostgreSQL.ResetMainDataAsync();
					await PostgreSQL.ResetPrioritisedAsync();
					break;

				case "start":
					Console.WriteLine();
					break;
			}

			List<string> list = new List<string>();
			list.Add("https://sn.wikipedia.org/wiki/");
			list.Add("https://www.wikipedia.org/");
			list.Add("https://de.wikipedia.org/");
			PostgreSQL.PushOutstandingAsync(list);
			StartedScraping = DateTime.Now;
			await LogWriter.WriteSettingsJsonAsync();

			RunScraper();
		}

		private static async void RunScraper()
		{
			//Called RunScraper
			Console.WriteLine("Called Scraping Method");

			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();
			List<string> outstanding = new List<string>();

			outstanding.AddRange(await PostgreSQL.GetPrioritisedAsync());
			outstanding.AddRange(await PostgreSQL.GetOutstandingAsync(BatchLimit - outstanding.Count));
			Console.WriteLine("Outstanding Count: " + outstanding.Count());
			while (Shouldrun)
			{
				Console.WriteLine("Started Cycle");
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
						var scrapingTask = await Scraping(list);

						foreach (var document in scrapingTask)
						{
							if (document.Links != null)
							{
								outstandingLinks.AddRange(document.Links);
							}
							if (document.PrioritisedLinks != null)
							{
								prioritisedLinks.AddRange(document.PrioritisedLinks);
							}
						}
					}
					foreach (var link in outstanding)
					{
						outstandingLinks.Remove(link);
						prioritisedLinks.Remove(link);
					}

					List<string> outstandingLinksnd = new HashSet<string>(outstandingLinks).ToList();
					List<string> prioritisedLinksnd = new HashSet<string>(prioritisedLinks).ToList();

					Console.WriteLine("Found {0} Links", outstandingLinksnd.Count);
					Console.WriteLine("#Prioritised Links {0}", prioritisedLinksnd.Count);
					TriggerPushLinks(outstandingLinksnd, prioritisedLinksnd);
					Batch++;
					Scrapes = await PostgreSQL.GetScrapingCount();
					await LogWriter.WriteSettingsJsonAsync();
				}
				else
				{
					Console.WriteLine("No links to Scrap");
				}
			}
		}
		
		private static async Task<List<Document>> Scraping(IEnumerable<string> links)
		{
			Console.WriteLine("Control Point A");
			var tasks = links.Select(x => Task.Run(() => Scraper.ScrapFromLinkAsync(x))).ToArray();
			Console.WriteLine("Control Point B");
			await Task.WhenAll(tasks);
			
			
			var documents = new List<Document>();
			
			foreach (var task in tasks)
			{
				Task.Run(() => PostgreSQL.PushDocumentAsync(task.Result));
				documents.Add(task.Result);
			}
			
			Console.WriteLine("Leaving Scraping Method");
			return documents;
		}

		private static async Task<bool> TriggerPushLinks(List<string> outstandingLinks, List<string> prioritisedLinks)
		{
			Task<bool>[] tasks = new Task<bool>[1];
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

		private static List<IEnumerable<string>> ListSplitter(List<string> Source, int size)
		{
			List<IEnumerable<string>> listOfLists = new List<IEnumerable<string>>();
			for (int i = 0; i < Source.Count(); i += size)
			{
				listOfLists.Add(Source.Skip(i).Take(size));
			}
			return listOfLists;
		}
	}
}