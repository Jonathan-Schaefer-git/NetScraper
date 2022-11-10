using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetScraper
{
	[Serializable]
	internal class DocumentSerializable
	{
		public static DocumentSerializable Convert(Document doc)
		{
			DocumentSerializable documentSerializable = new DocumentSerializable();
			documentSerializable.URL = doc.absoluteurl;
			documentSerializable.imagedata = doc.ImageData;
			documentSerializable.Emails = doc.Emails;
			documentSerializable.DateTime = doc.DateTime;
			documentSerializable.Links = doc.Links;
			documentSerializable.ID = doc.ID;
			documentSerializable.ResponseTime = doc.ResponseTime;
			return documentSerializable;
		}
		public long ID { get; set; }
		public DateTime DateTime { get; set; }
		public Uri? URL { get; set; }
		public double ResponseTime { get; set; }
		public List<string>? Emails { get; set; }
		public List<ImageData>? imagedata { get; set; }
		public IEnumerable<string>? Links { get; set; }
	}
}
