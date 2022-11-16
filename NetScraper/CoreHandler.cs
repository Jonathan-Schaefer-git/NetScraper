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
			var input = Console.ReadLine();

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
			//Main Method to start from
			var doc = Scraper.ScrapFromLink("https://wikipedia.de");

			//var htmlstring = Parser.ConvertDocToString(doc);
			//Console.WriteLine(htmlstring);

			//File.WriteAllText(@"C:\\Users\\jona4\\Desktop\Text.txt", htmlstring);
			if (doc != null)
			{
				var x = CSVData.CSVDataConvert(doc);
				CSVLog.WriteCSVLog(x);
				var w = DocumentSerializable.Convert(doc);
				string jsonString = JsonConvert.SerializeObject(w);
				File.WriteAllTextAsync(fileName, jsonString);
				Console.WriteLine(fileName);
			}
		}

		private static void RunScraper(string url)
		{
			if (shouldrun)
			{
				foreach(var site in )
				{
					var doc = Scraper.ScrapFromLink(url);

					DBManager.PushDataToDB(doc);
					var w = DocumentSerializable.Convert(doc);
					string jsonString = JsonConvert.SerializeObject(w);
				}
				Batch++;
			}
		}
	}
}