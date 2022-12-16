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
		public int BatchesCompleted { get; set; }
	}
	public static class LogWriter
	{
		public static JsonTextReader reader = new JsonTextReader(new StreamReader(CoreHandler.fileSettings));
		public static async Task<bool> WriteSettingsJsonAsync()
		{
			JsonSerializer js = new JsonSerializer();
			using (StreamWriter sw = new StreamWriter(CoreHandler.fileSettings))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				try
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
					writer.WritePropertyName("BatchesCompleted");
					writer.WriteValue(CoreHandler.Batch);
					writer.WriteEndObject();
					await writer.CloseAsync();
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}
		public static async Task<SettingObject> LoadJsonAsync()
		{
			using (StreamReader r = new StreamReader(CoreHandler.fileSettings))
			{
				string json = await r.ReadToEndAsync();
				var setting = new SettingObject();
				setting = JsonConvert.DeserializeObject<SettingObject>(json);
				return setting;
			}
		}
	}
}