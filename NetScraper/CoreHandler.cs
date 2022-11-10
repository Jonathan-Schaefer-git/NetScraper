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
		public static int BatchCounter = (int)(ID - (ID % 20000));
		public static bool shouldrun = false;
		public static string fileName = Path.Combine(Directory.GetCurrentDirectory(), "log.json");
		private static void Main(string[] args)
		{
			//Main Method to start from
			var doc = Scraper.ScrapFromLink("https://davidewiest.com");

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
		private static async void RunScraper(string url)
		{
			var doc = Scraper.ScrapFromLink(url);
		}
		
	}
}
