using HtmlAgilityPack;
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

		//private static Regex hrefregex = new Regex(@"(?i)\b((?:https?://|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s!()[]{};:'".,<>?«»“”‘’]))");
		private static Regex hrefregex = new Regex(@"href\s*=\s*(?:[""'](?<1>[^""']*)[""']|(?<1>[^>\s]+))", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		//public static string UrlRegex(Document document)
		//{
		//		}

		public static List<ImageData>? RetrieveImageData(Document doc)
		{
			var document = doc.HTML;

			if (document != null && doc.absoluteurl != null)
			{
				List<ImageData> imageDataList = new List<ImageData>();
				var x = document.DocumentNode.SelectNodes("//img");
				if (x != null)
				{
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
			}
			return null;
		}

		//ER = External Resources

		public static bool CheckLinkValidity(string url)
		{
			Uri result;
			return Uri.TryCreate(url, UriKind.Absolute, out result);
		}

		public static List<string>? RetrieveERs(Document doc)
		{
			var linklist = new List<string>();

			if (doc.HTML != null && doc.absoluteurl != null)
			{
				var htmlstring = doc.HTML.DocumentNode.OuterHtml;
				var values = htmlstring.Split("\"");
				var jsFiles = values.Where(value => value.Contains(".js"));
				var cssFiles = values.Where(value => value.Contains(".css"));
				//Console.WriteLine("Looking for JS and CSS");
				foreach (var jsfile in jsFiles)
				{
					if (!CheckLinkValidity(jsfile))
					{
						linklist.Add(GetAbsoluteUrlString(doc.absoluteurl.ToString(), jsfile));
					}
					else
					{
						linklist.Add(jsfile);
					}
				}
				foreach (var cssfile in cssFiles)
				{
					if (!CheckLinkValidity(cssfile))
					{
						linklist.Add(GetAbsoluteUrlString(doc.absoluteurl.ToString(), cssfile));
					}
					else
					{
						linklist.Add(cssfile);
					}
				}
				return linklist;
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
			if (document.HTML != null && document.absoluteurl != null)
			{
				var w = new List<string>();
				var doc = document.HTML;
				HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");
				if (nodes != null)
				{
					foreach (var n in nodes)
					{
						string href = n.Attributes["href"].Value;
						w.Add(GetAbsoluteUrlString(document.absoluteurl.ToString(), href));
						return w.ToList();
					}
				}
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

		/*
		 * public static bool IsImageUrl(string url)
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

		public static string? ConvertDocToUnfromattedString(Document doc)
		{
			if (doc.HTML == null)
			{
				return null;
			}
			else
			{
				var htmldoc = doc.HTML;

				return htmldoc.DocumentNode.OuterHtml;
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