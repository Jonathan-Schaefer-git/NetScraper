﻿using HtmlAgilityPack;

namespace NetScraper
{
	[Serializable]
	internal class Document
	{
		public bool Status { get; set; }
		public double ResponseTime { get; set; }
		public List<string>? Emails { get; set; }
		public List<ImageData>? ImageData { get; set; }
		//ER = External Resource
		public List<string>? CSSLinks { get; set; }
		public int CSSCount { get; set; }
		public int JSCount { get; set; }
		public List<string>? JSLinks { get; set; }

		public DateTime DateTime { get; set; }
		public Uri? Absoluteurl { get; set; }
		public long ApproxByteSize { get; set; }
		public IEnumerable<string>? Links { get; set; } = null;
		public List<string>? PrioritisedLinks { get; set; } = null;
		public string? HTMLString { get; set; } = null;
	}
}