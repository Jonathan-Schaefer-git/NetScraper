using System.Reflection.PortableExecutable;

namespace NetScraper
{
	public class LogWriter
	{
		public static int Linecount
		{
			get
			{
				int lines = 0;
				using (TextReader reader = File.OpenText(CoreHandler.filepath))
				{
					while (reader.ReadLine() != null) { lines++; }
				}
				Console.WriteLine(lines);
				return lines;
			}
		}
		public static int AtLine { get; set; }

		public static void WriteLineToLog(List<string> contents)
		{
			/*
			while ((line = reader.ReadLine()) != null)
			{
				line_number++;

				if (line_number > lines_to_delete)
					break;
			}

			while ((line = reader.ReadLine()) != null)
			{
				writer.WriteLine(line);
			}
			*/
		}
	}
}