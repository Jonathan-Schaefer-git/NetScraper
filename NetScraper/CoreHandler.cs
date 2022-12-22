using MongoDB.Driver.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace NetScraper
{
	internal static class CoreHandler
	{
		public static DateTime StartedScraping = DateTime.Now;
		public static int BatchLimit = 5000;
		public static int Batch = 0;
		public static long Scrapes = 0;
		public static int SimultaneousPool = 70;
		public static bool shouldRun = true;
		public static string ConnectionString = "Host=localhost;Username=postgres;Password=1598;Database=Netscraper";
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileBuffer = Path.Combine(filepath, "links.json");
		public static string fileTestBatch = Path.Combine(filepath, "testbatch.json");
		public static string fileSettings = Path.Combine(filepath, "settings.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");

		private static async Task Main()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(".NETScraper developed by Jona4Dev");
			Console.WriteLine("Reading Settings from {0}", fileSettings);
			var oldsettings = await LogWriter.LoadJsonAsync();
			PostgreSQL.cs = oldsettings.ConnectionString;
			//! This sets the webclient timeout for scraping
			Scraper.webclient.Timeout = TimeSpan.FromSeconds(1);
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
						Console.WriteLine("load - Load links from file to default stack. Use -t parameter to load benchmarking batch");
						Console.WriteLine("writebuffer - Write default stack to file");
						Console.WriteLine("init - Initialize default stack with standard links");
						Console.WriteLine("info - Get the rows of Maindata");
						Console.WriteLine("exit - Exits the application");
						Console.WriteLine("res - Reset startlinks");
						Console.WriteLine("start - Continue from existing stack");
						Console.WriteLine("debug - Run debug method");
						Console.WriteLine("exp - Run experimental scraper");
						Console.WriteLine("help - Supply this menu");
						Console.ForegroundColor = ConsoleColor.White;
						break;

					case "load -t":
						Console.WriteLine("=========================================");
						Console.WriteLine("[Info]: Loading Benchmarking links");
						startlinks = await LogWriter.ReadLinkBuffer(true);
						Console.WriteLine("[Info]: Loaded {0} links", startlinks.Count);
						break;

					case "debug":
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine("Launching Debug");
						Console.ForegroundColor = ConsoleColor.White;
						await DebugMethod(startlinks);
						break;

					case "start":
						Console.WriteLine();
						var state = await RunScraper(startlinks);
						switch (state)
						{
							case -1:
								WriteErrorMessage("No links were provided. Try again after running >init");
								break;
							case 0:
								WriteSuccessMessage("Scraping was stopped through interaction with the settings file");
								break;
							case 1:
								Console.WriteLine("This is in single run mode. Remove return 1;");
								break;
						}
						break;

					case "load":
						startlinks = await LogWriter.ReadLinkBuffer();
						if (startlinks.Count is not 0)
						{
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine("[Info]: Loaded {0} links", startlinks.Count);
						}
						else
						{
							WriteErrorMessage("No links in link buffer");
						}
						Console.ForegroundColor = ConsoleColor.White;
						break;

					case "writebuffer":
						var writestate = await LogWriter.WriteLinkBuffer(startlinks);
						if (writestate)
						{
							WriteSuccessMessage("Wrote Buffer");
						}
						else
						{
							WriteErrorMessage("Failed writing buffer");
						}
						break;

					case "init":
						WriteSuccessMessage("Supplied program with startlinks");
						startlinks.Add("https://www.wikipedia.org/");
						startlinks.Add("https://de.wikipedia.org");
						break;

					case "res":
						WriteSuccessMessage("Reset startlinks");
						startlinks.Clear();
						break;

					case "reset -s":
						SettingObject settings = new SettingObject();
						settings.SimultaneousPool = SimultaneousPool;
						settings.ShouldRun = true;
						settings.BatchesCompleted = Batch;
						settings.ConnectionString = ConnectionString;
						settings.ScrapesCompleted = Scrapes;
						var settingstate = await LogWriter.WriteSettingsJsonAsync(settings);
						if (settingstate)
						{
							WriteSuccessMessage("Reset settings.json");
						}
						else
						{
							WriteErrorMessage("Something went wrong trying to write settings.json");
						}
						break;

					case "reset":
						Console.WriteLine("Reseting Database");
						Task<bool>[] resettasks = new Task<bool>[] { PostgreSQL.ResetMainDataAsync() };
						await Task.WhenAll(resettasks);
						if (resettasks[0].Result is true)
						{
							WriteSuccessMessage("Reset was successful");
						}
						else
						{
							WriteErrorMessage("Table Reset failed");
						}
						break;

					case "exp":
						Console.WriteLine("=========================================");
						Console.WriteLine("[Info]: Started Experimental");
						Console.WriteLine("=========================================");
						//var expstate = await RunScraperAlternativeScheduler(startlinks);
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

		private static async Task<int> RunScraper(List<string> startlinks)
		{
			List<string> outstanding = new List<string>();
			ConcurrentBag<string> clinks = new ConcurrentBag<string>();
			if (outstanding.Count is 0 && startlinks.Count is 0)
			{
				Console.WriteLine("No links provided. Exiting");
				return -1;
			}

			var setting = await LogWriter.LoadJsonAsync();
			setting.StartedScraping = DateTime.Now;
			await LogWriter.WriteSettingsJsonAsync(setting);

			while (shouldRun)
			{
				

				Console.WriteLine("Start Cycle");
				if (outstanding.Count is 0)
				{
					Console.WriteLine("Outstanding is null using startlinks");
					foreach (var item in startlinks)
					{
						Console.WriteLine(item);
						clinks.Add(item);
					}
				}
				else
				{
					Console.WriteLine("Outstanding isn't null");
					foreach (var item in outstanding)
					{
						clinks.Add(item);
					}
					outstanding.Clear();
				}

				Console.WriteLine("Links to Scrap: " + clinks.Count);
				var documents = new ConcurrentBag<Document>();

				ParallelOptions parallel = new ParallelOptions();
				parallel.CancellationToken = CancellationToken.None;

				//! Launching tasks via Parallel.ForEachAsync() as it determines the maximal degree of parallelism automatically to maximize performance
				await Parallel.ForEachAsync(clinks, parallel, async (item, _) =>
				{
					documents.Add(await Scraper.ScrapFromLinkAsync(item));
				});

				//! Push Results to DB
				var state = PostgreSQL.PushDocumentListAsync(documents);
				var write = LogWriter.WriteLinkBuffer(outstanding);


				Console.WriteLine("Document Count: " + documents.Count);

				//! Free links of duplicates and excess
				outstanding = CleanUpOutstanding(ref outstanding, ref documents);
				clinks.Clear();
				Console.WriteLine("Outstanding Count next gen: " + outstanding.Count);

				var oldsettings = await LogWriter.LoadJsonAsync();
				shouldRun = oldsettings.ShouldRun;
				Console.WriteLine("Should Run: " + shouldRun);
				Batch = oldsettings.BatchesCompleted;

				//! Wait for the result of the DB Push && writing to file
				await write;
				await state;
				if (state.Result)
				{
					WriteSuccessMessage("Pushed to DB");
				}
				else
				{
					WriteErrorMessage("DB push failed");
				}
				
				if (write.Result)
				{
					WriteSuccessMessage("Wrote buffer", true);
				}
				else
				{
					WriteErrorMessage("Failed writing buffer", true);
				}
				var x = await PostgreSQL.GetScrapingCount();
				Batch++;
				documents.Clear();
				oldsettings.BatchesCompleted = Batch;
				oldsettings.ScrapesCompleted = x;
				var settingstate = await LogWriter.WriteSettingsJsonAsync(oldsettings);
				if (settingstate)
				{
					WriteSuccessMessage("Wrote settings", true);
				}
				else
				{
					WriteErrorMessage("Failed writing settings.json", true);
				}

				//! This is used for single run testing and should be commented by default
				//return 1;
			}
			//! Returning to menu
			return 0;
		}

		public static void WriteSuccessMessage(string content, bool bottomline = false)
		{
			Console.WriteLine("=========================================");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("[Success]: " + content);
			Console.ForegroundColor = ConsoleColor.White;
			if(bottomline)
				Console.WriteLine("=========================================");
		}

		public static void WriteErrorMessage(string content, bool bottomline = false)
		{
			Console.WriteLine("=========================================");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[Error]: " + content);
			Console.ForegroundColor = ConsoleColor.White;
			if(bottomline)
				Console.WriteLine("=========================================");
		}

		//! Overload for Parallel.ForEach with ConcurrentBag
		private static List<string> CleanUpOutstanding(ref List<string> oustandinglastgen, ref ConcurrentBag<Document> documents)
		{
			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();

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

			foreach (var link in oustandinglastgen)
			{
				outstandingLinks.Remove(link);
				prioritisedLinks.Remove(link);
			}

			outstandingLinks.RemoveAll(item => !item.StartsWith("http"));
			prioritisedLinks.RemoveAll(item => !item.StartsWith("http"));

			outstandingLinks.RemoveAll(item => item.EndsWith("pdf") || item.EndsWith("gif") || item.EndsWith("png") || item.EndsWith("webp") || item.EndsWith("svg") || item.EndsWith("jpg"));
			prioritisedLinks.RemoveAll(item => item.StartsWith("pdf") || item.EndsWith("gif") || item.EndsWith("png") || item.EndsWith("webp") || item.EndsWith("svg") || item.EndsWith("jpg"));

			List<string> outstandingLinksnd = new HashSet<string>(outstandingLinks).ToList();
			List<string> prioritisedLinksnd = new HashSet<string>(prioritisedLinks).ToList();

			switch (prioritisedLinksnd.Count > BatchLimit)
			{
				case true:
					Console.WriteLine("Path: True");
					prioritisedLinksnd.RemoveRange(BatchLimit, prioritisedLinksnd.Count - BatchLimit);
					return prioritisedLinksnd;

				case false:
					if (prioritisedLinksnd.Count == BatchLimit)
					{
						Console.WriteLine("Path: False: ==");
						return prioritisedLinksnd;
					}
					else
					{
						if (prioritisedLinksnd.Count + outstandingLinksnd.Count >= 5000)
						{
							Console.WriteLine("Path: False: >=");
							prioritisedLinksnd.AddRange(outstandingLinksnd.GetRange(prioritisedLinksnd.Count, BatchLimit - prioritisedLinksnd.Count));
							return prioritisedLinksnd;
						}
						else
						{
							Console.WriteLine("Path: False: <");
							prioritisedLinksnd.AddRange(outstandingLinksnd);
							return prioritisedLinksnd;
						}
					}
			}
		}

		//! This is a bad alternative Scheduler
		/*
		private static async Task<bool> RunScraperAlternativeScheduler(List<string> startlinks)
		{
			
			Console.WriteLine("Called Alternative Scheduler");
			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();
			List<string> outstanding = new List<string>();

			outstanding.AddRange(startlinks);
			while (shouldRun)
			{
				Console.WriteLine("Started Cycle");
				
				//! Get Links to be scraped from DB up to a maximum of 5k strings per batch

				outstanding.AddRange(outstanding);
				Console.WriteLine("Outstanding Count: " + outstanding.Count());

				//! Check settings (Used for web interface)
				var setting = await LogWriter.LoadJsonAsync();
				shouldRun = setting.ShouldRun;
				SimultaneousPool = setting.SimultaneousPool;

				Console.WriteLine("Concurrent Pool is: " + SimultaneousPool);

				if (outstanding != null)
				{
					var documents = new List<Document>();
					var tasks = outstanding.Select(x => Task.Run(() => Scraper.ScrapFromLinkAsync(x, true))).ToArray();
					var timer = Stopwatch.StartNew();
					await Task.WhenAll(tasks);
					foreach (var task in tasks)
					{
						documents.Add(task.Result);
					}

					timer.Stop();
					Console.WriteLine("Scraping took: " + timer.ElapsedMilliseconds + "ms");
					var pushtask = Task.Run(() => PostgreSQL.PushDocumentListAsync(documents));

					outstanding = CleanUpOutstanding(outstanding, documents);


					var getscrapecount = PostgreSQL.GetScrapingCount();

					Scrapes = await getscrapecount;
					Batch++;
					//var settingstate = await LogWriter.WriteSettingsJsonAsync();
					

					

					/*var writestate = await LogWriter.WriteLinkBuffer(outstanding);
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
					return false;
				}
				
				return true;
				
			}
			
			return true;
		}
		*/
		//! This method is reserved for testing singular features
		public static async Task<bool> DebugMethod(List<string> startlinks)
		{
			return true;
		}
		//! Legacy Method (refrain from usage)
		/*
		private static async Task<bool> RunScraperLegacy(List<string> startlinks)
		{
			
			//Called RunScraper
			Console.WriteLine("Called Scraping Method");

			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();
			List<string> outstanding = new List<string>();

			while (shouldRun)
			{
				Console.WriteLine("Started Cycle");
				List<Task<bool>> pushtasks = new List<Task<bool>>();
				//Get Links to be scraped from DB up to a maximum of 20k strings per batch
				if (outstanding.Count is 0)
					outstanding.AddRange(startlinks);

				Console.WriteLine("Links to be scraped: " + outstanding.Count());

				//Check settings (Used for web interface)
				var setting = await LogWriter.LoadJsonAsync();
				shouldRun = setting.ShouldRun;
				SimultaneousPool = setting.SimultaneousPool;

				Console.WriteLine("Concurreny Pool is: " + SimultaneousPool);

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

				outstanding = CleanUpOutstanding(outstanding, prioritisedLinks, outstandingLinks);

				Console.WriteLine("Link Count for next batch: " + outstanding.Count);
				var getscrapecount = PostgreSQL.GetScrapingCount();

				Scrapes = await getscrapecount;
				Batch++;
				//var settingstate = await LogWriter.WriteSettingsJsonAsync();
				await Task.WhenAll(pushtasks);

				if (pushtasks.All(a => a.Result) || pushtasks.All(a => !a.Result))
				{
					Console.WriteLine("All documents were pushed successfully");
				}
				else
				{
					Console.WriteLine("One or more document pushes failed");
				}
				/*
				var writestate = await LogWriter.WriteLinkBuffer(outstanding);
				if (writestate)
				{
					Console.WriteLine("Writing buffer was successful");
				}
				else
				{
					Console.WriteLine("Writing buffer failed");
				}
				
			}
			
			return true;
		}
		*/
		//! Deprecated Task Scheduler
		/*
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
		*/

		//! Bad alternative Task Scheduler
		/*
		private static async Task<List<Document>> QueingAlternative(IEnumerable<string> links)
		{
			
			ConcurrentBag<string> clinks = new ConcurrentBag<string>(links);
			ParallelOptions parallel = new ParallelOptions();
			ConcurrentBag<Document> documents = new ConcurrentBag<Document>();

			var tasks = new ConcurrentBag<Task<Document>>();

			foreach (var link in links)
			{
				tasks.Add(Task.Run(() => Scraper.ScrapFromLinkAsync(link)));
			}

			await Task.WhenAll(tasks);

			foreach (var task in tasks)
			{
				documents.Add(task.Result);
			}

			return documents.ToList();
		}
		*/

		//! This method is responsible for removing duplicates and bad links from the batch
		/*
		private static List<string> CleanUpOutstanding(ref List<string> oustandinglastgen, ref List<Document> documents)
		{
			List<string> outstandingLinks = new List<string>();
			List<string> prioritisedLinks = new List<string>();

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

			foreach (var link in oustandinglastgen)
			{
				outstandingLinks.Remove(link);
				prioritisedLinks.Remove(link);
			}

			outstandingLinks.RemoveAll(item => !item.StartsWith("http"));
			prioritisedLinks.RemoveAll(item => !item.StartsWith("http"));

			outstandingLinks.RemoveAll(item => item.EndsWith("pdf") || item.EndsWith("gif") || item.EndsWith("png") || item.EndsWith("webp") || item.EndsWith("svg") || item.EndsWith("jpg"));
			prioritisedLinks.RemoveAll(item => item.StartsWith("pdf") || item.EndsWith("gif") || item.EndsWith("png") || item.EndsWith("webp") || item.EndsWith("svg") || item.EndsWith("jpg"));

			List<string> outstandingLinksnd = new HashSet<string>(outstandingLinks).ToList();
			List<string> prioritisedLinksnd = new HashSet<string>(prioritisedLinks).ToList();

			switch (prioritisedLinksnd.Count > BatchLimit)
			{
				case true:
					prioritisedLinksnd.RemoveRange(BatchLimit, prioritisedLinksnd.Count - BatchLimit);
					return prioritisedLinksnd;

				case false:
					if (prioritisedLinksnd.Count == BatchLimit)
					{
						return prioritisedLinksnd;
					}
					else
					{
						if(prioritisedLinksnd.Count + outstandingLinksnd.Count >= 5000)
						{
							prioritisedLinksnd.AddRange(outstandingLinksnd.GetRange(prioritisedLinksnd.Count, BatchLimit - prioritisedLinksnd.Count));
							return prioritisedLinksnd;
						}
						else
						{
							prioritisedLinksnd.AddRange(outstandingLinksnd);
							return prioritisedLinksnd;
						}
					}
			}
		}
		*/

		//! Overload for legacy method
		/*
		private static List<string> CleanUpOutstanding(List<string> oustandinglastgen, List<string> prioritised, List<string> normal)
		{
			foreach (var link in oustandinglastgen)
			{
				normal.Remove(link);
				prioritised.Remove(link);
			}

			normal.RemoveAll(item => !item.StartsWith("http"));
			prioritised.RemoveAll(item => !item.StartsWith("http"));

			List<string> outstandingLinksnd = new HashSet<string>(normal).ToList();
			List<string> prioritisedLinksnd = new HashSet<string>(prioritised).ToList();

			switch (prioritisedLinksnd.Count > BatchLimit)
			{
				case true:
					prioritisedLinksnd.RemoveRange(BatchLimit, prioritisedLinksnd.Count - BatchLimit);
					return prioritisedLinksnd;

				case false:
					if (prioritisedLinksnd.Count == BatchLimit)
					{
						return prioritisedLinksnd;
					}
					else
					{
						try
						{
							prioritisedLinksnd.AddRange(outstandingLinksnd.GetRange(prioritisedLinksnd.Count, BatchLimit - prioritisedLinksnd.Count));
							return prioritisedLinksnd;
						}
						catch (Exception)
						{
							return prioritisedLinksnd;
						}

					}
			}
		}
		
		public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
		{
			for (int i = 0; i < Math.Ceiling(source.Count / (Double)size); i++)
				yield return new List<T>(source.Skip(size * i).Take(size));
		}
		*/
	}
}