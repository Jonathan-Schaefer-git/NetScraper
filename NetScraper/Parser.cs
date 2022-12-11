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

			if (document != null && doc.Absoluteurl != null)
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
						string relativeposition = node.XPath;
						imagedata.Alt = attributealt;
						imagedata.Link = GetAbsoluteUrlString(doc.Absoluteurl.ToString(), attributelinks);
						imagedata.Relativelocation = relativeposition;
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

		public static List<string>? RetrieveCSSLinks(Document doc)
		{
			var linklist = new List<string>();

			if (doc.HTML != null && doc.Absoluteurl != null)
			{
				var htmlstring = doc.HTML.DocumentNode.OuterHtml;
				var values = htmlstring.Split("\"");
				var cssFiles = values.Where(value => value.Contains(".css"));
				//Console.WriteLine("Looking for JS and CSS");
				foreach (var cssfile in cssFiles)
				{
					if (!CheckLinkValidity(cssfile))
					{
						linklist.Add(GetAbsoluteUrlString(doc.Absoluteurl.ToString(), cssfile));
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

		public static List<string>? RetrieveJSLinks(Document doc)
		{
			var linklist = new List<string>();

			if (doc.HTML != null && doc.Absoluteurl != null)
			{
				var htmlstring = doc.HTML.DocumentNode.OuterHtml;
				var values = htmlstring.Split("\"");
				var jsFiles = values.Where(value => value.Contains(".js"));
				//Console.WriteLine("Looking for JS and CSS");
				foreach (var jsfile in jsFiles)
				{
					if (!CheckLinkValidity(jsfile))
					{
						linklist.Add(GetAbsoluteUrlString(doc.Absoluteurl.ToString(), jsfile));
					}
					else
					{
						linklist.Add(jsfile);
					}
				}
				return linklist;
			}
			return null;
		}

		private static string? GetAbsoluteUrlString(string baseUrl, string url)
		{
			try
			{
				var uri = new Uri(url, UriKind.RelativeOrAbsolute);
				if (!uri.IsAbsoluteUri)
					uri = new Uri(new Uri(baseUrl), uri);
				return uri.ToString();
			}
			catch (Exception)
			{
				return null;
			}
		}

		//Get Second Level Domain
		private static string GetSLD(string? link)
		{
			if (link != null)
			{
				try
				{
					var host = new Uri(link).Host;
					return host.Substring(host.LastIndexOf('.', host.LastIndexOf('.') - 1) + 1);
				}
				catch (Exception)
				{
					return "";
				}
			}
			return "";
		}

		public static List<string>? FindPrioritisedLinks(Document doc)
		{
			List<string>? prioritisedlinks = new List<string>();
			if (doc.Links != null)
			{
				var x = GetSLD(doc.Absoluteurl.ToString());
				foreach (var link in doc.Links.ToList())
				{
					if (GetSLD(link) != x && GetSLD(link) != null)
					{
						prioritisedlinks.Add(link);
					}
				}
				return prioritisedlinks;
			}
			return null;
		}

		public static List<string>? ParseLinks(Document document)
		{
			if (document.HTML != null && document.Absoluteurl != null)
			{
				var w = new List<string>();
				var doc = document.HTML;
				HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");
				if (nodes != null)
				{
					foreach (var n in nodes)
					{
						string href = n.Attributes["href"].Value;
						var x = GetAbsoluteUrlString(document.Absoluteurl.ToString(), href);
						if (!x.EndsWith(".png") || !x.EndsWith(".gif") || !x.EndsWith(".svg") || !x.EndsWith(".jpg") || !x.EndsWith(".webp") || !x.EndsWith(".pdf"))
						{
							w.Add(x);
						}
					}
					return w;
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
					if (match.Value.EndsWith(".png") || match.Value.EndsWith(".gif") || match.Value.EndsWith(".svg") || match.Value.EndsWith(".jpg") || match.Value.EndsWith(".webp") || match.Value.EndsWith(".pdf"))
					{
					}
					else
					{
						result.Add(match.Value);
					}
				}
				List<string>? email = result.Distinct().ToList();
				return email;
			}
			Console.WriteLine("Content String was null");
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
				
				if (htmldoc.DocumentNode.OuterHtml != null)
				{
					return Regex.Replace((RemoveInsignificantHtmlWhiteSpace(htmldoc.DocumentNode.OuterHtml) ?? "").Replace("'", @"\'").Trim(), @"[\r\n]+", " ");
				}
				else
				{
					return null;
				}
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