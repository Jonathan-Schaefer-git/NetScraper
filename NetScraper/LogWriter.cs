using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;

namespace NetScraper
{
	public class LogWriter
	{
		public struct Settings
		{
			public int SimulPool { get; set; }
			public bool ShouldRun { get; set; }
		}
		public static void WriteSettingsJson(DateTime time)
		{
			JsonSerializer js = new JsonSerializer();
			using (StreamWriter sw = new StreamWriter(CoreHandler.fileSettings))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				writer.WriteStartObject();
				writer.WritePropertyName("Started Scraping");
				writer.WriteValue(time);
				writer.WritePropertyName("Simultanous Pool");
				writer.WriteValue(CoreHandler.SimultanousPool);
				writer.WritePropertyName("Should Run");
				writer.WriteValue(CoreHandler.shouldrun);
				writer.WriteEndObject();
			}
		}	
		public static void ReadSettingsJson()
		{
			var dt = new DateTime();
			using (StreamReader sr = new StreamReader(CoreHandler.fileSettings))
			using (JsonTextReader reader = new JsonTextReader(sr))
			{
				JObject keys = (JObject)JToken.ReadFrom(reader);
				dt = (DateTime)keys.GetValue("Started Scraping");
			}

		}
	}
}