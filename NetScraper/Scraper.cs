using HtmlAgilityPack;
using ScrapySharp.Network;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace NetScraper
{
	internal static class Scraper
	{
		private static ScrapingBrowser scrapingbrowser = new ScrapingBrowser();

		public static Document GetSources(Document doc)
		{
			scrapingbrowser.Timeout = new TimeSpan(2);
			scrapingbrowser.AllowAutoRedirect = true;
			return doc;
		}

		public static Document ScrapFromLink(string url)
		{
			Console.WriteLine("Called Scraper for {0}", url);
			//Open a new Document
			var document = new Document();
			document.Absoluteurl = new Uri(url);
			//Set Document parameters
			//Get HTMLDocument and time it
			var stopwatch = Stopwatch.StartNew();
			document.HTML = GetDocument(document);
			stopwatch.Stop();
			
			//Check if Website responded
			if (document.HTML != null)
			{

				document.DateTime = DateTime.UtcNow;
				//Webpage responded
				document.Status = true;
				//Get all linked pages from extracted Document
				var linklist = Parser.ParseLinks(document);
				if(linklist != null)
				{
					linklist.RemoveAll(x => String.IsNullOrEmpty(x));
				}
				document.Links = linklist;
				//Find Prioritised Links
				document.PrioritisedLinks = Parser.FindPrioritisedLinks(document);
				document.ImageData = Parser.RetrieveImageData(document);
				document.ContentString = Parser.ConvertDocToString(document);
				document.Emails = Parser.GetEmailOutOfString(document);
				document.ResponseTime = stopwatch.ElapsedMilliseconds;
				document.JSLinks = Parser.RetrieveJSLinks(document);
				document.CSSLinks = Parser.RetrieveCSSLinks(document);
				if(document.CSSLinks != null && document.JSLinks != null)
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

		public static HtmlDocument? GetDocument(Document document)
		{
			HtmlWeb web = new HtmlWeb();
			web.PreRequest = delegate (HttpWebRequest webRequest)
			{
				webRequest.Timeout = 1000;
				return true;
			};
			try
			{
				HtmlDocument doc = web.Load(document.Absoluteurl);
				if (doc == null)
					Console.WriteLine("No valid HTML Doc");
				return doc;
			}
			catch (Exception)
			{
				Console.WriteLine("No valid website");
				return null;
			}
		}
	}
}