using CsvHelper;
using System.Globalization;

namespace NetScraper
{
	internal static class CSVLog
	{
		public static void WriteCSVLog(CSVData csvData)
		{
			using (var writer = new StreamWriter(CoreHandler.fileNameCSV))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecord(csvData);
				csv.NextRecord();
			}
		}
	}

	[Serializable]
	internal class CSVData
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