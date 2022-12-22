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

		public static async Task<bool> WriteSettingsJsonAsync(SettingObject obj)
		{
			try
			{
				string json = JsonConvert.SerializeObject(obj);
				Console.WriteLine(json);
				File.WriteAllText(CoreHandler.fileSettings, json);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Something went wrong");
				Console.WriteLine(ex);
				return false;
			}
		}

		public static async Task<SettingObject> LoadJsonAsync()
		{
			var setting = new SettingObject();
			using (StreamReader r = new StreamReader(CoreHandler.fileSettings))
			{
				string json = await r.ReadToEndAsync();

				setting = JsonConvert.DeserializeObject<SettingObject>(json);

				r.Close();
			}
			return setting;
		}
	}
}