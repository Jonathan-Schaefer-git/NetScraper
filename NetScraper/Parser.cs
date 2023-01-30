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
			html.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
			return InsignificantHtmlWhitespace.Replace(html, String.Empty).Trim();
		}

		//private static Regex hrefregex = new Regex(@"(?i)\b((?:https?://|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s!()[]{};:'".,<>?«»“”‘’]))");
		private static Regex hrefregex = new Regex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		public static List<ImageData>? RetrieveImageData(Document doc)
		{
			if(doc.HTMLString is not null)
			{
				var imagedatalist = new List<ImageData>();
				string pattern = @"<img src=""(?<src>.+?)"" alt=""(?<description>.+?)"">";
				Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
				MatchCollection matches = regex.Matches(doc.HTMLString);

				foreach (Match match in matches)
				{
					if (match.Success)
					{
						ImageData img = new ImageData();
						
						img.Link = GetAbsoluteUrlString(doc.Absoluteurl.ToString() ,match.Groups["src"].Value);
						img.Alt = match.Groups["description"].Value;
						imagedatalist.Add(img);
					}
				}
				return imagedatalist;
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

			try
			{
				if (doc.HTMLString is not null)
				{
					var htmlstring = doc.HTMLString;
					var values = htmlstring.Split("\"");
					var jsFiles = values.Where(value => value.Contains(".css"));

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
			catch (Exception)
			{
				return null;
			}
		}

		public static List<string>? RetrieveJSLinks(Document doc)
		{
			var linklist = new List<string>();
			try
			{
				if (doc.HTMLString is not null)
				{
					var htmlstring = doc.HTMLString;
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
			catch (Exception)
			{
				return null;
			}
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
		public static string GetSLD(string? link)
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
					string domain = GetSLD(link);
					if(domain is not null && domain != x)
					{
						foreach (var ignore in CoreHandler.ignoreList)
						{
							if (domain != ignore)
							{
								prioritisedlinks.Add(link);
							}
						}
					}
				}

				return prioritisedlinks;
			}
			return null;
		}

		public static List<string>? ParseLinks(Document document)
		{
			if (document.Absoluteurl is not null && document.HTMLString is not null)
			{
				var w = new List<string>();
				var docstring = document.HTMLString;

				var links = Regex.Matches(docstring, @"<a\s+(?:[^>]*?\s+)?href=""([^""]*)""")
							 .Cast<Match>()
							 .Select(m => m.Groups[1].Value)
							 .ToList();

				if (links is not null)
				{
					foreach (var link in links)
					{
						if(link is not null or "")
						{
							if (link.StartsWith("http://") || link.StartsWith("https://"))
							{
								w.Add(link);
							}
							else
							{
								w.Add(GetAbsoluteUrlString(document.Absoluteurl.ToString(), link));
							}
						}
					}
					return w;
				}
				return null;
			}
			return null;
		}

		public static List<string>? GetEmailOutOfString(Document document)
		{
			List<string>? result = new List<string>();

			if (document.HTMLString is not null)
			{
				Regex regex = new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
				MatchCollection matches = regex.Matches(document.HTMLString);
				if (matches.Count == 0)
				{
					return null;
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
			return null;
		}

		public static string? ConvertDocToString(Document doc)
		{
			if (doc.HTMLString is null)
			{
				return null;
			}
			return Regex.Replace((RemoveInsignificantHtmlWhiteSpace(doc.HTMLString) ?? "").Replace("'", @"\'").Trim(), @"[\r\n]+", " ");
			
		}
	}
}