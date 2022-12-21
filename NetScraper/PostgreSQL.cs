using Npgsql;
using System.Text;

namespace NetScraper
{
	internal class PostgreSQL
	{
		private static string Connectionstring
		{
			get
			{
				return cs;
			}
		}

		private static string removeduplicate = "DELETE FROM outstanding WHERE ID IN (SELECT ID FROM (SELECT id, ROW_NUMBER() OVER (partition BY name ORDER BY ID) AS RowNumber FROM outstanding) AS T WHERE T.RowNumber > 1);";
		public static string cs = "Host=192.168.2.220;Username=postgres;Password=1598;Database=Netscraper";

		public static async Task<bool> ResetMainDataAsync()
		{
			using (var con = EstablishDBConnection())
			{
				await con.OpenAsync();
				using var cmd = new NpgsqlCommand();
				cmd.Connection = con;

				cmd.CommandText = "DROP TABLE IF EXISTS maindata cascade";
				var x = await cmd.ExecuteNonQueryAsync();

				cmd.CommandText = @"CREATE TABLE maindata(id SERIAL PRIMARY KEY, status BOOLEAN, url TEXT, datetime TEXT, emails TEXT[], csscount INTEGER, jscount INTEGER, approximatesize INTEGER, links TEXT[], contentstring TEXT, imagedescriptions TEXT[], imagelinks TEXT[])";
				Console.WriteLine("Resetted maindata");
				await cmd.ExecuteNonQueryAsync();
				con.Close();
				if (x is not -1)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		public static async Task<bool> PushDocumentListAsync(List<Document> documents)
		{
			Console.WriteLine("Called PushDocuments");
			var counter = 0;
			using (var con = EstablishDBConnection())
			{
				con.Open();
				var sql = @"INSERT INTO maindata(status, url, datetime, emails, csscount, jscount, approximatesize, links, contentstring, imagedescriptions, imagelinks) VALUES(@status, @url, @datetime, @emails, @csscount, @jscount, @approximatesize, @links, @contentstring, @imagedescriptions ,@imagelinks)";


				Console.WriteLine("Documents Count: " + documents.Count());


				foreach (var doc in documents)
				{
					var cmd = new NpgsqlCommand(sql, con);
					var imagealts = new List<string>();
					var imagelinks = new List<string>();
					var imagepositions = new List<string>();

					if (doc.HTMLString is null)
					{
						cmd.Parameters.AddWithValue(@"contentstring", "");
					}
					if (doc.ImageData is null)
					{
						doc.ImageData = new List<ImageData>();
					}
					foreach (var item in doc.ImageData)
					{
						if (item.Alt is not null)
						{
							imagealts.Add(item.Alt);
						}
						else
						{
							imagealts.Add("");
						}
						if (item.Link is not null)
						{
							imagelinks.Add(item.Link);
						}
						else
						{
							imagelinks.Add("");
						}
					}
					cmd.Parameters.AddWithValue(@"status", doc.Status);
					//This Null Reference check is useless but was added as a safety precussion
					if (doc.Absoluteurl is not null)
					{
						cmd.Parameters.AddWithValue(@"url", doc.Absoluteurl.ToString());
					}
					else
					{
						cmd.Parameters.AddWithValue(@"url", "");
						throw new Exception("Website was added without Value");
					}
					cmd.Parameters.AddWithValue(@"datetime", doc.DateTime.ToString());
					if (doc.Emails is null)
					{
						cmd.Parameters.AddWithValue(@"emails", new List<string>());
					}
					else
					{
						cmd.Parameters.AddWithValue(@"emails", doc.Emails);
					}
					cmd.Parameters.AddWithValue(@"csscount", doc.CSSCount);
					cmd.Parameters.AddWithValue(@"jscount", doc.JSCount);
					cmd.Parameters.AddWithValue(@"approximatesize", doc.ApproxByteSize);

					if (doc.Links is not null)
					{
						cmd.Parameters.AddWithValue(@"links", doc.Links);
					}
					else
					{
						cmd.Parameters.AddWithValue(@"links", new List<string>());
					}
					if (doc.HTMLString is not null)
					{
						cmd.Parameters.AddWithValue(@"contentstring",Parser.RemoveInsignificantHtmlWhiteSpace(doc.HTMLString));
					}

					cmd.Parameters.AddWithValue(@"imagedescriptions", imagealts);
					cmd.Parameters.AddWithValue(@"imagelinks", imagelinks);
					cmd.Parameters.AddWithValue(@"imagerelativeposition", imagepositions);
					await cmd.PrepareAsync();
					try
					{
						counter += await cmd.ExecuteNonQueryAsync();
					}
					catch (Exception)
					{
						counter -= 1;
					}
				}
				con.Close();
				Console.WriteLine("Rows inserted into Maindata: " + counter);
				if (counter is -1)
				{
					return false;
				}
				else
				{
					return true;
				}
			}


		}
		public static async Task<long> GetScrapingCount()
		{
			using (var con = EstablishDBConnection())
			{
				await con.OpenAsync();
				var sql = "SELECT * FROM maindata WHERE ID = (SELECT MAX(id) FROM maindata)";
				var cmd = new NpgsqlCommand(sql, con);
				using NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync();

				await rdr.ReadAsync();
				try
				{
					var x = rdr.GetInt64(0);
					await con.CloseAsync();
					return x;
				}
				catch (Exception)
				{
					await con.CloseAsync();
					return -1;
				}
			}
		}
		private static NpgsqlConnection EstablishDBConnection()
		{
			try
			{
				var con = new NpgsqlConnection(cs);
				return con;
			}
			catch (Exception)
			{
				Console.WriteLine("Database couldn't be reached");
				return null;
			}
		}


		//! These methods are deprecated. Do not use
		/*
		public static async Task<bool> ResetOutstandingAsync()
		{
			using var con = EstablishDBConnection();
			await con.OpenAsync();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;
			cmd.CommandText = "DROP TABLE IF EXISTS outstanding";
			var dropstate = await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"CREATE TABLE outstanding(id SERIAL PRIMARY KEY,name TEXT)";
			Console.WriteLine("Resetted outstanding");
			var createstate = await cmd.ExecuteNonQueryAsync();
			await con.CloseAsync();
			await con.DisposeAsync();
			if (dropstate != -1 && createstate != -1)
			{
				Console.WriteLine("Resetting outstanding with true");
				return true;
			}
			else
			{
				return false;
			}
		}

		public static async Task<bool> ResetPrioritisedAsync()
		{
			using var con = EstablishDBConnection();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;
			await con.OpenAsync();
			cmd.CommandText = "DROP TABLE IF EXISTS prioritised";
			var x = await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"CREATE TABLE prioritised(id SERIAL PRIMARY KEY,link TEXT)";
			Console.WriteLine("Resetted prioritised");
			var state = await cmd.ExecuteNonQueryAsync();
			await con.CloseAsync();
			await con.DisposeAsync();
			if (state != -1 && x != -1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static async Task<bool> PushOutstandingAsync(List<string> outstanding)
		{
			if (outstanding == null)
			{
				Console.WriteLine("outstanding is null");
				return false;
			}
			using var con = EstablishDBConnection();
			await con.OpenAsync();

			var resetstate = await ResetOutstandingAsync();
			var rowcount = 0;
			var sql = @"INSERT INTO outstanding(name) VALUES(@name)";
			foreach (var item in outstanding)
			{
				if (item != null)
				{
					var cmd = new NpgsqlCommand(sql, con);
					cmd.Parameters.AddWithValue("name", item);
					await cmd.PrepareAsync();
					rowcount += await cmd.ExecuteNonQueryAsync();
				}
				else
				{
					var cmd = new NpgsqlCommand(sql, con);
					cmd.Parameters.AddWithValue("name", "");
					await cmd.PrepareAsync();
					rowcount += await cmd.ExecuteNonQueryAsync();
				}
			}
			var duplicatestate = await RemoveDuplicatesAsync(con);
			await con.CloseAsync();
			con.Dispose();
			if (rowcount != -1)
			{
				return true;
			}
			else
			{
				return false;
			}

		}

		public static async Task<bool> PushPrioritisedAsync(List<string> prioritised)
		{
			if (prioritised == null)
			{
				Console.WriteLine("prioritised is null");
				return false;
			}
			using var con = EstablishDBConnection();
			await con.OpenAsync();

			await ResetPrioritisedAsync();

			var sql = @"INSERT INTO prioritised(link) VALUES(@link)";
			foreach (var item in prioritised)
			{
				if (item != null)
				{
					var command = new NpgsqlCommand(sql, con);
					command.Parameters.AddWithValue(@"link", item);
					await command.PrepareAsync();
					await command.ExecuteNonQueryAsync();
				}
				else
				{
					var cmd = new NpgsqlCommand(sql, con);
					cmd.Parameters.AddWithValue(@"link", "");
					await cmd.PrepareAsync();
					await cmd.ExecuteNonQueryAsync();
				}
			}
			var duplicatestate = await RemoveDuplicatesPriorityAsync(con);

			await con.CloseAsync();
			con.Dispose();
			return true;
		}





		

		private static async Task<bool> RemoveDuplicatesAsync(NpgsqlConnection con)
		{
			using var cmd = new NpgsqlCommand(removeduplicate, con);
			var x = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
			if (x != -1)
			{
				Console.WriteLine("Removed {0} duplicates in outstanding", x);
				return true;
			}
			else
			{
				Console.WriteLine("Something went wrong when removing duplicates");
				return false;
			}
		}

		private static async Task<bool> RemoveDuplicatesPriorityAsync(NpgsqlConnection con)
		{
			var sql = "DELETE FROM prioritised WHERE ID IN (SELECT ID FROM (SELECT id, ROW_NUMBER() OVER (partition BY link ORDER BY ID) AS RowNumber FROM prioritised) AS T WHERE T.RowNumber > 1);";
			using var cmd = new NpgsqlCommand(sql, con);
			var x = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
			if (x != -1)
			{
				Console.WriteLine("Removed {0} duplicates in prioritised", x);
				return true;
			}
			else
			{
				return false;
			}
		}

		public static async Task<IEnumerable<string>> GetOutstandingAsync(int i)
		{
			List<string> outstanding = new List<string>();
			using var con = EstablishDBConnection();
			await con.OpenAsync();
			StringBuilder sb = new StringBuilder("SELECT * FROM outstanding ORDER BY id DESC LIMIT ");
			sb.Append(i);
			sb.Append(";");
			string sql = sb.ToString();

			using var cmd = new NpgsqlCommand(sql, con);

			using NpgsqlDataReader rdr = cmd.ExecuteReader();
			while (rdr.Read())
			{
				outstanding.Add(rdr.GetString(1));
			}
			con.Close();

			return outstanding;
		}

		public static async Task<IEnumerable<string>> GetPrioritisedAsync()
		{
			List<string> prioritised = new List<string>();
			using var con = EstablishDBConnection();
			await con.OpenAsync();
			string sql = "SELECT * FROM prioritised ORDER BY id DESC LIMIT 20000";
			using var cmd = new NpgsqlCommand(sql, con);

			using NpgsqlDataReader rdr = cmd.ExecuteReader();

			while (rdr.Read())
			{
				prioritised.Add(rdr.GetString(1));
			}
			con.Close();
			con.Dispose();
			return prioritised;
		}
		*/
	}
}