using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Text.Json.JsonSerializer;

namespace NetScraper
{
	static class CoreHandler
	{

		public static long ID = 0;
		public static int BatchLimit = 20000;
		public static int Batch = (int)(ID % BatchLimit);
		public static int BatchCount = (int)(ID - (ID % BatchLimit));
		public static bool shouldrun = false;
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileName = Path.Combine(filepath, "log.json");
		private static void Main(string[] args)
		{
			//Main Method to start from
			var doc = Scraper.ScrapFromLink("https://wikipedia.de");

			//var htmlstring = Parser.ConvertDocToString(doc);
			//Console.WriteLine(htmlstring);

			//File.WriteAllText(@"C:\\Users\\jona4\\Desktop\Text.txt", htmlstring);
			if(doc != null)
			{
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
				for (BatchCount = 0; BatchCount < BatchLimit; BatchCount++)
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
