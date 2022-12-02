using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace NetScraper
{
	internal static class CoreHandler
	{
		public static int BatchLimit = 20000;
		public static int Batch = 0;
		public static bool shouldrun = true;
		public static string filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).Parent.FullName;
		public static string fileName = Path.Combine(filepath, "log.json");
		public static string fileNameCSV = Path.Combine(filepath, "log.csv");
		
		private static void Main()
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
			list.Add("https://de.wikipedia.org/wiki/Condor_Flugdienst");
			list.Add("https://www.bild.de/");
			list.Add("https://www.bild.de/");
			list.Add("https://www.bild.de/");
			list.Add("https://www.bild.de/");
			list.Add("https://www.bild.de/");
			PostgreSQL.PushOutstanding(list);
			
			//RunScraper();
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
			Batch = 0;
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
					Console.WriteLine("Approximate Size for {0} is {1}", w.absoluteurl, w.ApproxByteSize);
					if (w.Links != null)
					{
						foreach (var item in w.Links)
						{
							outstandingLinks.Add(item);
						}
						documents.Add(w);
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
						Console.WriteLine(doc.absoluteurl);
						js.Serialize(writer, DocumentSerializable.Convert(doc));
						
					}
					writer.WriteEndArray();
					writer.WriteEndObject();

				}
				
				foreach (var link in outstanding)
				{
					outstandingLinks.Remove(link);
				}
				Console.WriteLine("-----");
				foreach (var item in outstandingLinks)
				{
					Console.WriteLine(item);
				}
				var x = outstandingLinks.Distinct();
				PostgreSQL.PushOutstanding(x.ToList());
				//PostgreSQL.PushDocuments(documents);
				Batch++;
			}
		}
	}
}