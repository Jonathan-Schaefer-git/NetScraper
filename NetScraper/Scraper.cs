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
		static NameValueCollection nvc = new NameValueCollection();
		public static WebResponse ScrapFromLink(string url)
		{
			var requrl = new Uri(url);
			var stopwatch = Stopwatch.StartNew();
			WebResponse response = _scrapingbrowser.ExecuteRequestAsync(requrl, HttpVerb.Get, nvc);
			stopwatch.Stop();
			return response;
			
		}
	}
}