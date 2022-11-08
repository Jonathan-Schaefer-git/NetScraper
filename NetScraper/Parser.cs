using HtmlAgilityPack;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace NetScraper
{
	internal class Parser
	{
		// positive look behind for ">", one or more whitespace (non-greedy), positive lookahead for "<"
		private static readonly Regex InsignificantHtmlWhitespace = new Regex(@"(?<=>)\s+?(?=<)");

		// Known not to handle HTML comments or CDATA correctly, which we don't use.
		public static string RemoveInsignificantHtmlWhiteSpace(string html)
		{
			return InsignificantHtmlWhitespace.Replace(html, String.Empty).Trim();
		}


		public static string? ConvertDocToString(Document doc)
		{
			if(doc.HTML == null)
			{
				return null;
			}
			else
			{
				var htmldoc = doc.HTML;
				
				return Regex.Replace((RemoveInsignificantHtmlWhiteSpace(htmldoc.DocumentNode.OuterHtml) ?? "").Replace("'", @"\'").Trim(), @"[\r\n]+", " ");
			}
		} 
	}
}