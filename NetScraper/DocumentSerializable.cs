namespace NetScraper
{
	[Serializable]
	internal class DocumentSerializable
	{
		public static DocumentSerializable Convert(Document doc)
		{
			DocumentSerializable documentSerializable = new DocumentSerializable();
			documentSerializable.URL = doc.absoluteurl;
			documentSerializable.Status = doc.Status;
			//documentSerializable.imagedata = doc.ImageData;
			//documentSerializable.Emails = doc.Emails;
			documentSerializable.DateTime = doc.DateTime;
			//documentSerializable.Links = doc.Links;
			documentSerializable.ResponseTime = doc.ResponseTime;
			//documentSerializable.ERLinks = doc.ERLinks;
			return documentSerializable;
		}
		public bool Status { get; set; }
		public DateTime DateTime { get; set; }
		public Uri? URL { get; set; }
		public double ResponseTime { get; set; }
		//public List<string>? Emails { get; set; }
		//public List<ImageData>? imagedata { get; set; }
		//public long approxbytesize { get; set; }
		//public IEnumerable<string>? Links { get; set; }
		//ER = External Resource
		//public List<string>? ERLinks { get; set; }
	}
}