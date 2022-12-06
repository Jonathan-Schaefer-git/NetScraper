using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetScraper
{
	public class SettingObject
	{
		public DateTime StartedScraping { get; set; }
		public int SimultaneousPool { get; set; }
		public bool ShouldRun { get; set; }
		public long ScrapesCompleted { get; set; }
	}
	public static class LogWriter
	{
		public static JsonTextReader reader = new JsonTextReader(new StreamReader(CoreHandler.fileSettings));
		public static void WriteSettingsJson()
		{
			JsonSerializer js = new JsonSerializer();
			using (StreamWriter sw = new StreamWriter(CoreHandler.fileSettings))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				writer.WriteStartObject();
				writer.WritePropertyName("StartedScraping");
				writer.WriteValue(CoreHandler.StartedScraping);
				writer.WritePropertyName("SimultaneousPool");
				writer.WriteValue(CoreHandler.SimultaneousPool);
				writer.WritePropertyName("ShouldRun");
				writer.WriteValue(CoreHandler.Shouldrun);
				writer.WritePropertyName("ScrapesCompleted");
				writer.WriteValue(CoreHandler.Scrapes);
				writer.WriteEndObject();
				writer.Close();
			}
		}
		public static SettingObject LoadJson()
		{
			using (StreamReader r = new StreamReader(CoreHandler.fileSettings))
			{
				string json = r.ReadToEnd();
				var setting = new SettingObject();
				setting = JsonConvert.DeserializeObject<SettingObject>(json);
				return setting;
			}
		}
		
	}
}