using HtmlAgilityPack;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace NetScraper
{
	internal class Document
	{
		public double ResponseTime { get; set; }
		public long ID { get; set; }
		public string? contentstring { get; set; }
		public DateTime dateTime { get; set; }
		public Uri? absoluteurl { get; set; }
		public string? contenttype { get; set; }
		public HtmlDocument? HTML { get; set; }
	}
}
