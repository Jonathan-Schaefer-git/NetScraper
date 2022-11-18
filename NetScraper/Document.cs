using HtmlAgilityPack;

namespace NetScraper
{
	[Serializable]
	internal class Document
	{
		public long ID { get; set; }
		public bool Status { get; set; }
		public double ResponseTime { get; set; }
		public List<string>? Emails { get; set; } = null;
		public List<ImageData>? ImageData { get; set; }

		//ER = External Resource
		public List<string>? ERLinks { get; set; }
		public DateTime DateTime { get; set; }
		public Uri? absoluteurl { get; set; } = null;
		public long ApproxByteSize { get; set; }
		public IEnumerable<string>? Links { get; set; } = null;
		public HtmlDocument? HTML { get; set; } = null;
		public string? ContentString { get; set; } = null;
	}
}