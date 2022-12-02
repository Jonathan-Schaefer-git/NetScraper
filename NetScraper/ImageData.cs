namespace NetScraper
{
	internal class ImageData
	{
		public List<ImageData> ListData = new List<ImageData>();
		public string? Link { get; set; }
		public string? Alt { get; set; }
		public string? Relativelocation { get; set; }
	}
}