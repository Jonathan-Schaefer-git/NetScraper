using Npgsql;
using System.Text;

namespace NetScraper
{
	internal class PostgreSQL
	{
		private static string removeduplicate = "DELETE FROM outstanding WHERE ID IN (SELECT ID FROM (SELECT id, ROW_NUMBER() OVER (partition BY name ORDER BY ID) AS RowNumber FROM outstanding) AS T WHERE T.RowNumber > 1);";
		private static string cs = "Host=192.168.2.220;Username=postgres;Password=1598;Database=Netscraper";

		public static void ResetOutstanding()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS outstanding";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"CREATE TABLE outstanding(id SERIAL PRIMARY KEY,name TEXT)";
			Console.WriteLine("Resetted outstanding");
			cmd.ExecuteNonQuery();
			con.Close();
		}

		public static void ResetPrioritised()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS prioritised";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"CREATE TABLE prioritised(id SERIAL PRIMARY KEY,link TEXT)";
			Console.WriteLine("Resetted prioritised");
			cmd.ExecuteNonQuery();
			con.Close();
		}

		public static void PushOutstanding(List<string> outstanding)
		{
			if (outstanding == null)
			{
				Console.WriteLine("outstanding is null");
				return;
			}
			using var con = new NpgsqlConnection(cs);
			con.Open();

			ResetOutstanding();

			var sql = @"INSERT INTO outstanding(name) VALUES(@name)";
			foreach (var item in outstanding)
			{
				if (item != null)
				{
					var cmd = new NpgsqlCommand(sql, con);
					cmd.Parameters.AddWithValue("name", item);
					cmd.ExecuteNonQuery();
				}
				else
				{
					var cmd = new NpgsqlCommand(sql, con);
					cmd.Parameters.AddWithValue("name", "");
					cmd.ExecuteNonQuery();
				}
			}
			
			RemoveDuplicates();

			con.Close();
		}

		public static void PushPrioritised(List<string> prioritised)
		{
			if (prioritised == null)
			{
				Console.WriteLine("prioritised is null");
				return;
			}
			using var con = new NpgsqlConnection(cs);
			con.Open();

			ResetPrioritised();

			var sql = @"INSERT INTO prioritised(link) VALUES(@link)";
			foreach (var item in prioritised)
			{
				if (item != null)
				{
					var command = new NpgsqlCommand(sql, con);
					command.Parameters.AddWithValue(@"link", item);
					command.ExecuteNonQuery();
				}
				else
				{
					var cmd = new NpgsqlCommand(sql, con);
					cmd.Parameters.AddWithValue(@"link", "");
					cmd.ExecuteNonQuery();
				}
			}
			RemoveDuplicatesPriority(con);

			con.Close();
		}

		public static long GetScrapingCount()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			var sql = "SELECT * FROM maindata WHERE ID = (SELECT MAX(id) FROM maindata)";
			var cmd = new NpgsqlCommand(sql, con);
			using NpgsqlDataReader rdr = cmd.ExecuteReader();

			rdr.Read();
			var x = rdr.GetInt64(0);
			
			con.Close();
			return x;
		}

		public static void PushDocument(Document doc)
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			var sql = @"INSERT INTO maindata(status, url, datetime, emails, csscount, jscount, approximatesize, links, contentstring, imagedescriptions, imagelinks, imagerelativeposition) VALUES(@status, @url, @datetime, @emails, @csscount, @jscount, @approximatesize, @links, @contentstring, @imagedescriptions ,@imagelinks, @imagerelativeposition)";

			var cmd = new NpgsqlCommand(sql, con);
			var imagealts = new List<string>();
			var imagelinks = new List<string>();
			var imagepositions = new List<string>();
			if (doc.ContentString == null || doc.ImageData == null)
			{
				return;
			}
			foreach (var item in doc.ImageData)
			{
				if (item.Alt != null)
				{
					imagealts.Add(item.Alt);
				}
				else
				{
					imagealts.Add("");
				}
				if (item.Link != null)
				{
					imagelinks.Add(item.Link);
				}
				else
				{
					imagelinks.Add("");
				}
				if (item.Relativelocation != null)
				{
					imagepositions.Add(item.Relativelocation);
				}
				else
				{
					imagepositions.Add("");
				}
			}
			cmd.Parameters.AddWithValue(@"status", doc.Status);
			//This Null Reference check is useless but was added as a safety precussion
			if(doc.Absoluteurl != null)
			{
				cmd.Parameters.AddWithValue(@"url", doc.Absoluteurl.ToString());
			}
			else
			{
				cmd.Parameters.AddWithValue(@"url", "");
				throw new Exception("Website was added without Value");
			}
			cmd.Parameters.AddWithValue(@"datetime", doc.DateTime.ToString());
			if(doc.Emails == null)
			{
				cmd.Parameters.AddWithValue(@"emails", "");
			}
			else
			{
				cmd.Parameters.AddWithValue(@"emails", doc.Emails);
			}
			cmd.Parameters.AddWithValue(@"csscount", doc.CSSCount);
			cmd.Parameters.AddWithValue(@"jscount", doc.JSCount);
			cmd.Parameters.AddWithValue(@"approximatesize", doc.ApproxByteSize);

			if (doc.Links != null)
			{
				cmd.Parameters.AddWithValue(@"links", doc.Links);
			}
			else
			{
				cmd.Parameters.AddWithValue(@"links", new List<string>());
			}
			cmd.Parameters.AddWithValue(@"contentstring", doc.ContentString);
			cmd.Parameters.AddWithValue(@"imagedescriptions", imagealts);
			cmd.Parameters.AddWithValue(@"imagelinks", imagelinks);
			cmd.Parameters.AddWithValue(@"imagerelativeposition", imagepositions);
			cmd.Prepare();
			cmd.ExecuteNonQuery();

			con.Close();
		}

		public static void ResetMainData()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS maindata cascade";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"CREATE TABLE maindata(id SERIAL PRIMARY KEY, status BOOLEAN, url TEXT, datetime TEXT, emails TEXT[], csscount INTEGER, jscount INTEGER, approximatesize INTEGER, links TEXT[], contentstring TEXT, imagedescriptions TEXT[], imagelinks TEXT[], imagerelativeposition TEXT[])";
			Console.WriteLine("Resetted maindata");
			cmd.ExecuteNonQuery();
			con.Close();
		}

		private static void RemoveDuplicates()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			using var cmd = new NpgsqlCommand(removeduplicate, con);
			var x = cmd.ExecuteNonQuery();
			Console.WriteLine("Removed {0} duplicates in outstanding", x);
			con.Close();
		}

		private static void RemoveDuplicatesPriority(NpgsqlConnection con)
		{
			var sql = "DELETE FROM prioritised WHERE ID IN (SELECT ID FROM (SELECT id, ROW_NUMBER() OVER (partition BY link ORDER BY ID) AS RowNumber FROM prioritised) AS T WHERE T.RowNumber > 1);";
			using var cmd = new NpgsqlCommand(sql, con);
			var x = cmd.ExecuteNonQuery();
			Console.WriteLine("Removed {0} duplicates in prioritised", x);
		}

		public static IEnumerable<string> GetOutstanding(int i)
		{
			List<string> outstanding = new List<string>();
			using var con = new NpgsqlConnection(cs);
			con.Open();
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

		public static IEnumerable<string> GetPrioritised()
		{
			List<string> prioritised = new List<string>();
			using var con = new NpgsqlConnection(cs);
			con.Open();
			string sql = "SELECT * FROM prioritised ORDER BY id DESC LIMIT 20000";
			using var cmd = new NpgsqlCommand(sql, con);

			using NpgsqlDataReader rdr = cmd.ExecuteReader();

			while (rdr.Read())
			{
				prioritised.Add(rdr.GetString(1));
			}
			con.Close();
			return prioritised;
		}
	}
}