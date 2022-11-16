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
using System.Runtime.InteropServices;
using System;

namespace NetScraper
{
	static class Scraper
	{
		static ScrapingBrowser scrapingbrowser = new ScrapingBrowser();

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
			document.absoluteurl = new Uri(url);
			//Set Document parameters
			//Get HTMLDocument and time it
			var stopwatch = Stopwatch.StartNew();
			document.HTML = GetDocument(document);
			stopwatch.Stop();
			//Check if Website responded
			if(document.HTML != null)
			{
				document.DateTime = DateTime.UtcNow;
				//Webpage responded
				document.Status = true;
				//Get all linked pages from extracted Document
				document.Links = Parser.ParseLinks(document);
				document.ImageData = Parser.RetrieveImageData(document);
				document.ContentString = Parser.ConvertDocToString(document);
				document.Emails = Parser.GetEmailOutOfString(document);
				
				document.ResponseTime = stopwatch.ElapsedMilliseconds;
				document.ERLinks = Parser.RetrieveERs(document);
				try
				{
					if (document.ContentString != null)
					{
						string s = document.ContentString;
						document.approxbytesize = ASCIIEncoding.Unicode.GetByteCount(document.ContentString);
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
			try
			{
				HtmlDocument doc = web.Load(document.absoluteurl);
				return doc;
			}
			catch (Exception)
			{
				return null;
			}
		}
		
	}
}