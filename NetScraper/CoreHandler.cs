using MongoDB.Driver.Linq;
using System.Diagnostics;


namespace NetScraper
{
	internal static class CoreHandler
	{
		public static DateTime StartedScraping = DateTime.Now;
		public static int BatchLimit = 10000;
		public static int Batch = 0;
		public static long Scrapes = 0;
		public static int SimultaneousPool = 70;
		public static bool Shouldrun = true;
		public static string ConnectionString = "Host=localhost;Username=postgres;Password=1598;Database=Netscraper";
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileBuffer = Path.Combine(filepath, "links.json");
		public static string fileSettings = Path.Combine(filepath, "settings.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");

		private static async Task Main()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(".NETScraper developed by Jona4Dev");
			Console.WriteLine("Reading Settings from {0}", fileSettings);
			var settings = await LogWriter.LoadJsonAsync();
			Batch = settings.BatchesCompleted;
			Console.WriteLine("https://github.com/Jona4Play/NetScraper");
			Console.WriteLine("=========================================");
			Console.WriteLine("Type 'help' to get the list of commands");
			var startlinks = new List<string>();
			while (true)
			{
				Console.ForegroundColor = ConsoleColor.White;
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
						Console.WriteLine("=========================================");
						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("reset - Reset table and start scraping (Need for providing links to default stacks)");
						Console.WriteLine("load - Load links from file to default stack");
						Console.WriteLine("writebuffer - Write default stack to file");
						Console.WriteLine("init - Initialize default stack with standard links");
						Console.WriteLine("info - Get the rows of Maindata");
						Console.WriteLine("exit - Exits the application");
						Console.WriteLine("start - Continue from existing stack");
						Console.WriteLine("debug - Run debug method");
						Console.WriteLine("help - Supply this menu");
						Console.ForegroundColor = ConsoleColor.White;
						break;

					case "debug":
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine("Launching Debug");
						Console.ForegroundColor = ConsoleColor.White;
						await DebugMethod(startlinks);
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
						startlinks = await LogWriter.ReadLinkBuffer();
						if (startlinks is not null)
						{
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine("[Info]: Loaded {0} links", startlinks.Count);
						}
						else
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("[Error]: Failed loading links");
						}
						Console.ForegroundColor = ConsoleColor.White;
						break;

					case "writebuffer":
						var writestate = await LogWriter.WriteLinkBuffer(startlinks);
						if (writestate)
						{
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine("[Info]: Writing to file was successful");
						}
						else
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("[Error]: Writing to file failed");
						}
						Console.ForegroundColor = ConsoleColor.White;
						break;

					case "init":
						Console.WriteLine("=========================================");
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("[Info]: Supplying Program with start links");
						Console.ForegroundColor = ConsoleColor.White;
						startlinks.Add("https://www.wikipedia.org/");
						startlinks.Add("https://de.wikipedia.org");
						break;

					case "reset":
						Console.WriteLine("Reseting Database");
						Task<bool>[] resettasks = new Task<bool>[] { PostgreSQL.ResetMainDataAsync() };
						await Task.WhenAll(resettasks);
						if (resettasks[0].Result is true)
						{

							Console.WriteLine("=========================================");
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine("Resetting Tables was successful");
							Console.ForegroundColor = ConsoleColor.White;
						}
						else
						{
							Console.WriteLine("=========================================");
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("[Error]: Resetting wasn't successful");
							Console.ForegroundColor = ConsoleColor.White;
						}
						break;

					case "info":
						var s = await PostgreSQL.GetScrapingCount();
						Console.WriteLine("=========================================");
						if (s is not -1)
						{
							Console.ForegroundColor = ConsoleColor.Cyan;
							Console.WriteLine("[Info]: There are {0} entries in maindata", s);
						}
						else
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("[Error]: Maindata has no rows or doesn't exist");
						}
						Console.ForegroundColor = ConsoleColor.White;
						break;

					case "exit":
						Console.WriteLine("=========================================");
						Console.WriteLine("Goodbye");
						Environment.Exit(0);
						break;
				}
			}
		}
		private static async Task<bool> DebugMethod(List<string> startlinks)
		{
			var documents = new List<Document>();
			foreach (var item in startlinks)
			{
				documents.Add(await Scraper.ScrapFromLinkAsync(item, true));
			}
			await PostgreSQL.PushDocumentListAsync(documents);
			return true;
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
				List<Task<bool>> pushtasks = new List<Task<bool>>();
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

						pushtasks.Add(PostgreSQL.PushDocumentListAsync(documents));
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
					var settingstate = await LogWriter.WriteSettingsJsonAsync();
					await Task.WhenAll(pushtasks);

					if (pushtasks.All(a => a.Result) || pushtasks.All(a => !a.Result))
					{
						Console.WriteLine("All documents were pushed successfully");
					}
					else
					{
						Console.WriteLine("One or more document pushes failed");
					}

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


		public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
		{
			for (int i = 0; i < Math.Ceiling(source.Count / (Double)size); i++)
				yield return new List<T>(source.Skip(size * i).Take(size));
		}
	}
}