using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

		public static void WriteLineToLog(string content, int i)	
		{
			if(Linecount <= CoreHandler.BatchLimit)
			{
				File.AppendAllText(CoreHandler.fileName, content);
			}
			else
			{

				
			}
		}
	}

}
