using CsvHelper;
using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetScraper
{
	static class CSVLog
	{
		public static void WriteCSVLog(string path, CSVData csvData)
		{
			using (var writer = new StreamWriter(path))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords((System.Collections.IEnumerable)csvData);
			}
		}
	}


	[Serializable]
	class CSVData
	{
		public static CSVData CSVDataConvert(Document doc)
		{
			CSVData data = new CSVData();
			data.ID = doc.ID;
			data.URL = doc.absoluteurl;
			data.Status = doc.Status;
			data.DateTime = doc.DateTime;
			return data;
		}

		public long ID { get; set; }
		public bool Status { get; set; }
		public DateTime DateTime { get; set; }
		public Uri? URL { get; set; }
	}
}
