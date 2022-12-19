using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace NetScraper
{
	internal static class Scraper
	{
		private static HtmlWeb web = new HtmlWeb();

		public static async Task<Document> ScrapFromLinkAsync(string url, bool verbose = false)
		{
			if (verbose)
			{
				Console.WriteLine("Called Scraper for {0}", url);
			}

			//Open a new Document
			var document = new Document();
			document.Absoluteurl = new Uri(url);

			//Get HTMLDocument and time it
			var stopwatch = Stopwatch.StartNew();
			document.HTML = await GetDocumentAsync(document);
			stopwatch.Stop();

			if (verbose)
				Console.WriteLine("Getting {0} took {1}ms", document.Absoluteurl, stopwatch.ElapsedMilliseconds);

			//Check if Website responded
			if (document.HTML is not null)
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
				if (linklist != null)
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
				document.Emails = emailTask.Result;
				document.JSLinks = jslinksTask.Result;
				document.ImageData = imagedataTask.Result;

				//Get JS & CSS Links Count
				if (document.CSSLinks is not null && document.JSLinks is not null)
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
					if (document.ContentString is not null)
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
				//Console.WriteLine("Website hasn't responded");
				document.Status = false;
				return document;
			}
		}

		public static HtmlDocument GetDocument(Document document)
		{
			web.PreRequest = delegate (HttpWebRequest webRequest)
			{
				webRequest.Timeout = 1000;
				webRequest.AllowAutoRedirect = true;
				webRequest.MaximumAutomaticRedirections = 3;
				return true;
			};
			try
			{
				var doc = web.Load(document.Absoluteurl.ToString());
				if (doc is null)
				{
					return null;
				}
				else
				{
					return doc;
				}
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static async Task<HtmlDocument>? GetDocumentNoEC()
		{
			web.PreRequest = delegate (HttpWebRequest webRequest)
			{
				webRequest.Timeout = 1000;
				webRequest.AllowAutoRedirect = true;
				webRequest.MaximumAutomaticRedirections = 3;
				return true;
			};

			return new HtmlDocument();
		}

		public static async Task<HtmlDocument>? GetDocumentAsync(Document document)
		{
			web.PreRequest = delegate (HttpWebRequest webRequest)
			{
				webRequest.Timeout = 1000;
				webRequest.AllowAutoRedirect = false;
				webRequest.MaximumAutomaticRedirections = 4;
				return true;
			};

			try
			{
				var doc = await web.LoadFromWebAsync(document.Absoluteurl.ToString());
				if (doc is null)
				{
					//Console.WriteLine("No valid HTML Doc");
				}
				else
				{
					return doc;
				}
			}
			catch (Exception)
			{
				//Console.WriteLine("No valid website for {0}", document.Absoluteurl);
				return null;
			}
			return null;
		}
	}
}