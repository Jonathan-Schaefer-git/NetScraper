using Newtonsoft.Json;

namespace NetScraper
{
	internal static class CoreHandler
	{
		public static long ID = 0;
		public static int BatchLimit = 20000;
		public static int Batch = (int)(ID % BatchLimit);
		public static int BatchCount = (int)(ID - (ID % BatchLimit));
		public static bool shouldrun = false;
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileName = Path.Combine(filepath, "log.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");

		private static void Main(string[] args)
		{
			Console.WriteLine(".NETScraper developed by Jona4Dev");
			Console.WriteLine("https://github.com/Jona4Play/NetScraper");
			Console.WriteLine("============================================================================");
			Console.WriteLine("Type 'help' to get the list of commands");
			//var input = Console.ReadLine();
			/*
			switch (input)
			{
				default:
					Console.WriteLine("Unknown Command use 'help' to get the list of valid commands");
					break;

				case "help":
					Console.WriteLine();
					break;

				case "start":
					Console.WriteLine();
					break;

				case "s":
					Console.WriteLine();
					break;
			}
			*/
			List<string> list = new List<string>();
			list.Add("https://wikipedia.de");
			PostgreSQL.PushOutstanding(list);

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
			BatchCount = 0;
			List<Document> documents = new List<Document>();
			List<string> jsonStrings = new List<string>();
			List<string> outstandingLinks = new List<string>();
			List<string> outstanding = new List<string>();
			outstanding.AddRange(PostgreSQL.GetOutstanding());
			if (shouldrun && outstanding != null)
			{
				foreach (var site in outstanding)
				{
					var w = Scraper.ScrapFromLink(site);
					if(w.Links != null)
					{
						foreach (var item in w.Links)
						{
							outstandingLinks.Add(item);
						}
						documents.Add(w);
					}
					BatchCount++;
				}

				
				foreach (var doc in documents)
				{
					jsonStrings.Add(JsonConvert.SerializeObject(DocumentSerializable.Convert(doc)));
				}
				File.WriteAllLines(fileName, jsonStrings);
				var x = outstandingLinks.Distinct();
				PostgreSQL.PushOutstanding(x.ToList());
				Batch++;
			}
		}
	}
}