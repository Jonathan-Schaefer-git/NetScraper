using Newtonsoft.Json;

namespace NetScraper
{
	public class SettingObject
	{
		public DateTime StartedScraping { get; set; }
		public int SimultaneousPool { get; set; }
		public bool ShouldRun { get; set; }
		public long ScrapesCompleted { get; set; }
		public int BatchesCompleted { get; set; }
		public string ConnectionString { get; set; } = String.Empty;
	}
	public static class LogWriter
	{
		public static async Task<bool> WriteLinkBuffer(List<string> links)
		{
			try
			{
				var jsonstring = JsonConvert.SerializeObject(links);
				await File.WriteAllTextAsync(CoreHandler.fileBuffer, jsonstring);
				return true;
			}
			catch (Exception)
			{
				return false;
			}

		}
		public static async Task<List<string>> ReadLinkBuffer(bool benchmark = false)
		{
			try
			{
				if (!benchmark)
				{
					using (StreamReader r = new StreamReader(CoreHandler.fileBuffer))
					{
						string jsonstring = await r.ReadToEndAsync();
						List<string> links = JsonConvert.DeserializeObject<List<string>>(jsonstring);
						return links;
					}
				}
				else
				{
					using (StreamReader r = new StreamReader(CoreHandler.fileTestBatch))
					{
						string jsonstring = await r.ReadToEndAsync();
						List<string> links = JsonConvert.DeserializeObject<List<string>>(jsonstring);
						return links;
					}
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
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
					writer.WriteValue(CoreHandler.shouldRun);
					writer.WritePropertyName("ScrapesCompleted");
					writer.WriteValue(CoreHandler.Scrapes);
					writer.WritePropertyName("BatchesCompleted");
					writer.WriteValue(CoreHandler.Batch);
					writer.WritePropertyName("ConnectionString");
					writer.WriteValue(CoreHandler.ConnectionString);
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