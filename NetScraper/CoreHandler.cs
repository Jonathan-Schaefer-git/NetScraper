using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetScraper
{
	internal class CoreHandler
	{
		public static long ID = 0;
		public static int BatchCounter = 0;
		private static void Main(string[] args)
		{
			//Main Method to start from
			var doc = Scraper.ScrapFromLink("https://www.mongodb.com/blog/post/quick-start-c-sharp-and-mongodb-creating-documents");
			var htmlstring = Parser.ConvertDocToString(doc);
			Console.WriteLine(htmlstring);
			File.WriteAllText(@"C:\\Users\\jona4\\Desktop\Text.txt", htmlstring);
		}
	}
}
