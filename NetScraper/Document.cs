using cloudscribe.HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetScraper
{
	internal class Document
	{
		public double ResponseTime { get; set; }
		public long ID { get; set; }

		public HtmlDocument? HTML { get; set; }

	}
}
