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
				return lines;
			}
		}

		public static void WriteLineToLog(List<string> contents)
		{
			var linestobewritten = contents.Count();
			var linesfree = CoreHandler.BatchLimit - Linecount;
			if (linesfree > linestobewritten)
			{
				File.AppendAllLines(CoreHandler.fileName, contents);
			}
			else if (Linecount + contents.Count > CoreHandler.BatchLimit && Linecount == 20000)
			{
				foreach (var line in contents)
				{
					using (StreamWriter swr = new StreamWriter(CoreHandler.fileName, false))
					{
						swr.WriteLine(line);
					}
				}
			}
			else if(linestobewritten > linesfree)
			{
				while(Linecount < CoreHandler.BatchLimit)
				{
					using (StreamWriter swr = new StreamWriter(CoreHandler.fileName, true))
					{
						foreach (var item in contents)
						{
							swr.WriteLine(item);
						}
					}
				}
				using (StreamWriter swr = new StreamWriter(CoreHandler.fileName, false))
				{
					foreach (var item in contents)
					{
						swr.WriteLine(item);
					}
				}
			}
		}
	}
}