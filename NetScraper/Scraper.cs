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
			HtmlWeb web = new HtmlWeb();
			document.HTML = GetDocument(url);
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
			document.dateTime = DateTime.Now;
			document.absoluteurl = w.AbsoluteUrl;


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