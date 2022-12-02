using Newtonsoft.Json;
using System.Diagnostics;

namespace NetScraper
{
	internal static class CoreHandler
	{
		public static int BatchLimit = 20000;
		public static int Batch = 0;
		public static int SimultanousPool = 0;
		public static bool shouldrun = true;
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileName = Path.Combine(filepath, "log.json");
		public static string fileSettings = Path.Combine(filepath, "settings.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");

		private static void Main()
		{
			Console.WriteLine(".NETScraper developed by Jona4Dev");
			Console.WriteLine("Loading settings from {0}", fileSettings);
			LogWriter.ReadSettingsJson();
			LogWriter.WriteSettingsJson(DateTime.UtcNow);
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

				case "reroll":
					Console.WriteLine("Rerolling Maindata");
					PostgreSQL.ResetMainData();
					break;
				case "new":
					Console.WriteLine("Rerolling Outstanding");
					PostgreSQL.ResetOutstanding();
					break;

				case "start":
					Console.WriteLine();
					break;
			}
			List<string> list = new List<string>();
			list.Add("https://wikipedia.org");
			PostgreSQL.PushOutstanding(list);

			RunScraper();
			//Main Method to start from

			//var htmlstring = Parser.ConvertDocToString(doc);
			//Console.WriteLine(htmlstring);

			//File.WriteAllText(@"C:\\Users\\jona4\\Desktop\Text.txt", htmlstring);
			/*
			if (doc != null)
			{
				var x = CSVData.CSVDataConvert(doc);
				CSVLog.WriteCSVLog(x);
				var w = DocumentSerializable.Convert(doc);
				string jsonString = JsonConvert.SerializeObject(w);
				File.WriteAllTextAsync(fileName, jsonString);
				Console.WriteLine(fileName);
			}
			*/
		}

		private static void RunScraper()
		{
			//Called RunScraper
			Console.WriteLine("Called Scraping Method");
			List<Document> documents = new List<Document>();
			List<string> jsonStrings = new List<string>();
			List<string> outstandingLinks = new List<string>();
			List<string> outstanding = new List<string>();
			outstanding.AddRange(PostgreSQL.GetOutstanding());

			if (shouldrun && outstanding != null)
			{
				foreach (var site in outstanding)
				{
					var watch = Stopwatch.StartNew();
					var w = Scraper.ScrapFromLink(site);
					watch.Stop();
					Console.WriteLine("Approximate Size for {0} is {1} and took {2}", w.Absoluteurl, w.ApproxByteSize, watch.ElapsedMilliseconds);
					if (w.Links != null)
					{
						foreach (var item in w.Links)
						{
							outstandingLinks.Add(item);
						}
						documents.Add(w);
					}
					else
					{
						Console.WriteLine("No Links at this site {0}", w.Absoluteurl);
					}
				}

				JsonSerializer js = new JsonSerializer();
				using (StreamWriter sw = new StreamWriter(fileName))
				using (JsonWriter writer = new JsonTextWriter(sw))
				{
					writer.WriteStartObject();
					writer.WritePropertyName("Documents");
					writer.WriteStartArray();
					foreach (var doc in documents)
					{
						Console.WriteLine(doc.Absoluteurl);
						js.Serialize(writer, DocumentSerializable.Convert(doc));
					}
					writer.WriteEndArray();
					writer.WriteEndObject();
				}

				foreach (var link in outstanding)
				{
					outstandingLinks.Remove(link);
				}
				var x = outstandingLinks.Distinct();
				Console.WriteLine("Found {0} Links",outstandingLinks.Count);
				PostgreSQL.PushOutstanding(x.ToList());
				PostgreSQL.PushDocuments(documents);
				Batch++;
			}
			else
			{
				Console.WriteLine("Shouldn't run or no Links to Scrap");
			}
		}
	}
}