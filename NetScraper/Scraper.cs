using CsvHelper;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;

namespace NetScraper
{
	static class Scraper
	{
		static ScrapingBrowser scrapingbrowser = new ScrapingBrowser();
		public static Document ScrapFromLink(string url)
		{
			Console.WriteLine("Called Scraper");
			//Open a new Document
			var document = new Document();
			//Set Document parameters

			//Get HTMLDocument and time it
			var stopwatch = Stopwatch.StartNew();
			document.HTML = GetDocument(url);
			
			stopwatch.Stop();

			var breakfastTasks = new List<Task> { eggsTask, baconTask, toastTask };

			var parseLinks = Parser.ParseLinks(document);
			var imageData = Parser.RetrieveImageData(document);
			document.absoluteurl = new Uri(url);
			//Get all linked pages from extracted Document
			document.Links = Parser.ParseLinks(document);
			document.ImageData = Parser.RetrieveImageData(document);
			document.ContentString = Parser.ConvertDocToString(document);
			document.Emails = Parser.GetEmailOutOfString(document);
			document.DateTime = DateTime.UtcNow;
			document.ResponseTime = stopwatch.ElapsedMilliseconds;

			//Debug block:
			/*
			if (document.Links != null)
			{
				foreach (var item in document.Links)
				{
					Console.WriteLine(item);
				}
			}
			else
			{
				Console.WriteLine("Links were null");
			}
			*/

			

			//Return Document
			return document;
			
		}
		
		public static HtmlDocument GetDocument(string url)
		{
			HtmlWeb web = new HtmlWeb();
			HtmlDocument doc = web.Load(url);
			return doc;
		}
		
	}
}