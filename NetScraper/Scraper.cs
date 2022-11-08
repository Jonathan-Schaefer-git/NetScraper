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
		static ScrapingBrowser _scrapingbrowser = new ScrapingBrowser();
		public static Document ScrapFromLink(string url)
		{
			var document = new Document();
			var stopwatch = Stopwatch.StartNew();
			document.HTML = GetDocument(url);
			document.dateTime = DateTime.UtcNow;
			var w = document.HTML;
			stopwatch.Stop();
			document.ResponseTime = stopwatch.ElapsedMilliseconds;
			return document;
		}
		public static Document ScrapResource(string url)
		{
			var document = new Document();
			var stopwatch = Stopwatch.StartNew();
			var requesturl = new Uri(url);
			var w = _scrapingbrowser.DownloadWebResource(requesturl);
			document.contenttype = w.ContentType;
			document.dateTime = DateTime.UtcNow;
			document.absoluteurl = w.AbsoluteUrl;
			document.ID = CoreHandler.ID;


			//GetResponseTime
			stopwatch.Stop();
			document.ResponseTime = stopwatch.ElapsedMilliseconds;
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