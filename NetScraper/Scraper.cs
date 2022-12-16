using HtmlAgilityPack;
using ScrapySharp.Network;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace NetScraper
{
	internal static class Scraper
	{
		private static ScrapingBrowser scrapingbrowser = new ScrapingBrowser();
		private static HtmlWeb web = new HtmlWeb();
		public static Document GetSources(Document doc)
		{
			scrapingbrowser.Timeout = new TimeSpan(2);
			scrapingbrowser.AllowAutoRedirect = true;
			return doc;
		}

		public static async Task<Document> ScrapFromLinkAsync(string url)
		{
			Console.WriteLine("Called Scraper for {0}", url);
			//Open a new Document
			var document = new Document();
			document.Absoluteurl = new Uri(url);
			//Set Document parameters
			//Get HTMLDocument and time it
			var stopwatch = Stopwatch.StartNew();
			document.HTML = await GetDocument(document);
			stopwatch.Stop();
			
			//Check if Website responded
			if (document.HTML != null)
			{
				document.DateTime = DateTime.UtcNow;
				//Webpage responded set appropriate Status
				document.Status = true;
				//Set Response time of Website
				document.ResponseTime = stopwatch.ElapsedMilliseconds;

				var contentstringTask = Task.Run(() => Parser.ConvertDocToString(document));
				var jslinksTask = Task.Run(() => Parser.RetrieveJSLinks(document));
				var csslinksTask = Task.Run(() => Parser.RetrieveCSSLinks(document));
				var linklistTask = Task.Run(() => Parser.ParseLinks(document));
				var linklist = await linklistTask;
				if(linklist != null)
				{
					linklist.RemoveAll(x => String.IsNullOrEmpty(x));
				}
				document.Links = linklist;

				//Tasks that are dependent on the result of Linkslist
				var prioritisedlinksTask = Task.Run(() => Parser.FindPrioritisedLinks(document));
				var imagedataTask = Task.Run(() => Parser.RetrieveImageData(document));
				
				//emailTask is reliant on the Contentstring
				document.ContentString = await contentstringTask;
				var emailTask = Task.Run(() => Parser.GetEmailOutOfString(document));

				//Wait for all Tasks to finish execution
				await Task.WhenAll(jslinksTask, csslinksTask, emailTask, prioritisedlinksTask, imagedataTask);
				
				//Set properties
				document.PrioritisedLinks = prioritisedlinksTask.Result;
				document.CSSLinks = csslinksTask.Result;
				document.Emails= emailTask.Result;
				document.JSLinks= jslinksTask.Result;
				document.ImageData = imagedataTask.Result;
				
				//Get JS & CSS Links Count
				if (document.CSSLinks != null && document.JSLinks != null)
				{
					document.CSSCount = document.CSSLinks.Count();
					document.JSCount = document.JSLinks.Count();
				}
				else
				{
					document.CSSCount = 0;
					document.JSCount = 0;
				}

				try
				{
					if (document.ContentString != null)
					{
						string s = document.ContentString;
						document.ApproxByteSize = ASCIIEncoding.Unicode.GetByteCount(document.ContentString);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
				return document;
			}
			else
			{
				document.DateTime = DateTime.UtcNow;
				Console.WriteLine("Website hasn't responded");
				document.Status = false;
				return document;
			}
		}

		public static async Task<HtmlDocument>? GetDocument(Document document)
		{
			web.PreRequest = delegate (HttpWebRequest webRequest)
			{
				webRequest.Timeout = 1000;
				webRequest.AllowAutoRedirect= false;
				return true;
			};

			try
			{
				var doc = web.LoadFromWebAsync(document.Absoluteurl.ToString());
				var x = await doc;
				if (x == null)
				{
					Console.WriteLine("No valid HTML Doc");
				}
				else
				{
					return x;
				}
			}
			catch (Exception)
			{
				Console.WriteLine("No valid website");
				return new HtmlDocument();
			}
			return null;
		}
	}
}