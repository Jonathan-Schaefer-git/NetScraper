﻿using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;

namespace NetScraper
{
	internal static class Scraper
	{
		private static HtmlWeb web = new HtmlWeb();

		private static HttpClientHandler handler = new HttpClientHandler()
		{
			AllowAutoRedirect = true,
			MaxAutomaticRedirections = 4
		};

		public static HttpClient webclient = new HttpClient(handler);

		public static async Task<Document> ScrapFromLinkAsync(string url, bool verbose = false)
		{
			if (verbose)
			{
				Console.WriteLine("Called Scraper for {0}", url);
			}

			//Open a new Document
			var document = new Document();
			try
			{
				document.Absoluteurl = new Uri(url);
			}
			catch (Exception)
			{
				document.Absoluteurl = new Uri("");
				document.Status = false;
				document.DateTime = DateTime.UtcNow;
			}

			//Get HTMLDocument and time it

			var stopwatch = Stopwatch.StartNew();
			try
			{
				document.HTMLString = await GetHTMLString(document);
			}
			catch (Exception)
			{
				document.HTMLString = null;
			}
			stopwatch.Stop();

			if (verbose)
				Console.WriteLine("Getting {0} took {1}ms", document.Absoluteurl, stopwatch.ElapsedMilliseconds);

			//Check if Website responded
			if (document.HTMLString is not null or "")
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
				var emailTask = Task.Run(() => Parser.GetEmailOutOfString(document));
				var linklist = await linklistTask;
				if (linklist != null)
				{
					linklist.RemoveAll(x => String.IsNullOrEmpty(x));
				}
				document.Links = linklist;

				//Tasks that are dependent on the result of Linkslist
				var prioritisedlinksTask = Task.Run(() => Parser.FindPrioritisedLinks(document));
				var imagedataTask = Task.Run(() => Parser.RetrieveImageData(document));

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
					if (document.HTMLString is not null)
					{
						document.ApproxByteSize = ASCIIEncoding.Unicode.GetByteCount(document.HTMLString);
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

		//! This is a test method and it may not be reliable and/or scalable
		public static async Task<Document> ScrapExperimental(string url, bool verbose)
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
			document.HTMLString = await GetHTMLString(document);
			stopwatch.Stop();

			if (verbose)
				Console.WriteLine("Getting {0} took {1}ms", document.Absoluteurl, stopwatch.ElapsedMilliseconds);

			//Check if Website responded
			if (document.HTMLString is not null or "")
			{
				document.DateTime = DateTime.UtcNow;

				//Webpage responded set appropriate Status
				document.Status = true;

				//Set Response time of Website
				document.ResponseTime = stopwatch.ElapsedMilliseconds;
				document.JSLinks = Parser.RetrieveJSLinks(document);
				document.CSSLinks = Parser.RetrieveCSSLinks(document);
				var linklist = Parser.ParseLinks(document);
				if (linklist != null)
				{
					linklist.RemoveAll(x => String.IsNullOrEmpty(x));
				}
				document.Links = linklist;

				//Tasks that are dependent on the result of Linkslist
				document.PrioritisedLinks = Parser.FindPrioritisedLinks(document);
				document.ImageData = Parser.RetrieveImageData(document);

				//emailTask is reliant on the Contentstring
				document.Emails = Parser.GetEmailOutOfString(document);

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
					if (document.HTMLString is not null)
					{
						document.ApproxByteSize = ASCIIEncoding.Unicode.GetByteCount(document.HTMLString);
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

		public static Task<string>? GetHTMLString(Document document)
		{
			return webclient.GetStringAsync(document.Absoluteurl);
		}
	}
}