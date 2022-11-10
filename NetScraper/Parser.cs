using HtmlAgilityPack;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace NetScraper
{
	internal class Parser
	{
		// positive look behind for ">", one or more whitespace (non-greedy), positive lookahead for "<"
		private static readonly Regex InsignificantHtmlWhitespace = new Regex(@"(?<=>)\s+?(?=<)");

		private static readonly Regex cHttpUrlsRegex = new Regex(@"(?<url>((http|https):[/][/]|www.)([a-z]|[A-Z]|[0-9]|[_/.=&?%-]|[~])*)", RegexOptions.IgnoreCase);

		// Known not to handle HTML comments or CDATA correctly, which we don't use.
		public static string RemoveInsignificantHtmlWhiteSpace(string html)
		{
			return InsignificantHtmlWhitespace.Replace(html, String.Empty).Trim();
		}

		//public static string UrlRegex(Document document)
		//{
		//		}

		public static List<ImageData>? RetrieveImageData(Document doc)
		{
			var document = doc.HTML;

			if (document != null && doc.absoluteurl != null)
			{
				List<ImageData> imageDataList = new List<ImageData>();

				foreach (HtmlNode node in document.DocumentNode.SelectNodes("//img"))
				{
					ImageData imagedata = new ImageData();

					string attributealt = node.GetAttributeValue("alt", "");
					string attributelinks = node.GetAttributeValue("src", "");
					imagedata.alts = attributealt;
					imagedata.links = GetAbsoluteUrlString(doc.absoluteurl.ToString(), attributelinks);
					imageDataList.Add(imagedata);
				}
				return imageDataList;
			}
			return null;
		}

		public static List<string>? RetrieveImageDescription(Document doc)
		{
			List<string>? descriptions = new List<string>();
			if (doc.HTML != null && doc.absoluteurl != null)
			{
				var document = doc.HTML;
				var p = document.DocumentNode.SelectNodes("//alt");
				foreach (var item in p)
				{
					if (item != null)
					{
						descriptions.Add(item.ToString());
					}
				}
				return descriptions;
			}
			return null;
		}

		private static string GetAbsoluteUrlString(string baseUrl, string url)
		{
			var uri = new Uri(url, UriKind.RelativeOrAbsolute);
			if (!uri.IsAbsoluteUri)
				uri = new Uri(new Uri(baseUrl), uri);
			return uri.ToString();
		}

		public static List<string>? ParseLinks(Document document)
		{
			//Without Image Links
			if (document.HTML != null && document.absoluteurl != null)
			{
				var w = new List<string>();
				var doc = document.HTML;
				HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");
				foreach (var n in nodes)
				{
					string href = n.Attributes["href"].Value;
					w.Add(GetAbsoluteUrlString(document.absoluteurl.ToString(), href));
				}
				return w.ToList();
			}
			return null;
		}

		public static List<string>? GetEmailOutOfString(Document document)
		{
			//Console.WriteLine("Called GetEmail");
			List<string>? result = new List<string>();
			if (document.ContentString != null)
			{
				//Console.WriteLine("Content String wasn't null");
				Regex regex = new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b", RegexOptions.IgnoreCase);
				MatchCollection matches = regex.Matches(document.ContentString);
				if (matches.Count == 0)
				{
					Console.WriteLine("No E-Mails Found");
				}
				foreach (Match match in matches)
				{
					//Console.WriteLine(match.Value);
					result.Add(match.Value);
				}

				List<string>? email = result.Distinct().ToList();

				return email;
			}
			Console.WriteLine("ContentString was null");
			return null;
		}

		public static bool IsImageUrl(string url)
		{
			if (url is not null)
			{
				HttpClient httpClient = new HttpClient();

				var req = (HttpWebRequest)WebRequest.Create(url);
				req.Method = "HEAD";
				using (var resp = req.GetResponse())
				{
					return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
							   .StartsWith("image/");
				}
			}
			return false;
		}

		/*
		public static List<string>? ExtractURLs(Document document)
		{
			//Console.WriteLine("Called ExtractURLs");
			if(document.HTML != null)
			{
				//Console.WriteLine("HTML wasn't null");
				var doc = document.HTML;
				var linkTags = doc.DocumentNode.Descendants("link");
				var linkedPages = doc.DocumentNode.Descendants("a")
												  .Select(a => a.GetAttributeValue("href", null))
												  .Where(u => !String.IsNullOrEmpty(u));
				List<string> LinkList = linkedPages.ToList();
				return	LinkList;
			}
			Console.WriteLine("HTML was null");
			return null;
		}
		*/

		public static string? ConvertDocToString(Document doc)
		{
			if (doc.HTML == null)
			{
				return null;
			}
			else
			{
				var htmldoc = doc.HTML;

				return Regex.Replace((RemoveInsignificantHtmlWhiteSpace(htmldoc.DocumentNode.OuterHtml) ?? "").Replace("'", @"\'").Trim(), @"[\r\n]+", " ");
			}
		}

		public static IEnumerable<string> ExtractHttpUrls(string aText, string? aMatch = null)
		{
			if (String.IsNullOrEmpty(aText))
				yield break;
			var matches = cHttpUrlsRegex.Matches(aText);
			var vMatcher = aMatch == null ? null : new Regex(aMatch);
			foreach (Match match in matches)
			{
				var vUrl = HttpUtility.UrlDecode(match.Groups["url"].Value);
				if (vMatcher == null || vMatcher.IsMatch(vUrl))
					yield return vUrl;
			}
		}
	}
}