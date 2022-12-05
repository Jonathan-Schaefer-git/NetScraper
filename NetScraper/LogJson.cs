using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetScraper
{
	internal class LogJSON
	{
		public static void LogLatestDocuments(List<Document> docs)
		{
			JsonSerializer js = new JsonSerializer();
			using (StreamWriter sw = new StreamWriter(CoreHandler.fileName))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				writer.WriteStartObject();
				writer.WritePropertyName("Documents");
				writer.WriteStartArray();
				foreach (var doc in docs)
				{
					Console.WriteLine(doc.Absoluteurl);
					js.Serialize(writer, DocumentSerializable.Convert(doc));
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
		}
	}
}
