using Npgsql;

namespace NetScraper
{
	internal class PostgreSQL
	{
		private static string removeduplicate = "DELETE FROM outstanding WHERE ID IN (SELECT ID FROM (SELECT id, ROW_NUMBER() OVER (partition BY name ORDER BY ID) AS RowNumber FROM outstanding) AS T WHERE T.RowNumber > 1);";
		private static string cs = "Host=192.168.2.220;Username=postgres;Password=1598;Database=Netscraper";

		public static void CheckIfExists(NpgsqlConnection con)
		{
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS outstanding";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"CREATE TABLE outstanding(id SERIAL PRIMARY KEY,name TEXT)";
			Console.WriteLine("Created Outstanding");
			cmd.ExecuteNonQuery();
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

			CheckIfExists(con);

			var sql = @"INSERT INTO outstanding(name) VALUES(@name)";
			foreach (var item in outstanding)
			{
				var cmd = new NpgsqlCommand(sql, con);
				cmd.Parameters.AddWithValue("name", item);
				cmd.ExecuteNonQuery();
			}
			/*
			foreach (var link in outstanding)
			{
				cmd.Parameters.AddWithValue('link', link);
				cmd.Prepare();
				cmd.ExecuteNonQuery();
				Console.WriteLine("Added {0} to stack", link);
			}
			*/

			con.Close();
		}

		public static void PushDocuments(List<Document> documents)
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			var sql = "INSERT INTO maindata(status, url, datetime, emails, csscount, jscount, approximatesize, links, contentstring, id, imagedescriptions, imagelinks, imagerelativeposition) VALUES(@status, @url, @datetime, @emails, @csscount, @jscount, @approximatesize, @links, @contentstring, @imagelinks, @imagerelativeposition)";

			foreach (var doc in documents)
			{
				var cmed = new NpgsqlCommand(sql, con);
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
				cmed.Parameters.AddWithValue("status", doc.Status);
				cmed.Parameters.AddWithValue("url", doc.Absoluteurl.ToString());
				cmed.Parameters.AddWithValue("datetime", doc.DateTime.ToString());
				cmed.Parameters.AddWithValue("emails", doc.Emails);
				cmed.Parameters.AddWithValue("csscount", doc.CSSCount);
				cmed.Parameters.AddWithValue("jscount", doc.JSCount);
				cmed.Parameters.AddWithValue("approximatesize", doc.ApproxByteSize);

				if (doc.Links != null)
				{
					cmed.Parameters.AddWithValue("links", doc.Links);
				}
				else
				{
					cmed.Parameters.AddWithValue("links", "");
				}
				cmed.Parameters.AddWithValue("contentstring", doc.ContentString);
				cmed.Parameters.AddWithValue("imagedescriptions", imagealts);
				cmed.Parameters.AddWithValue("imagelinks", imagelinks);
				cmed.Parameters.AddWithValue("imagerelativeposition", imagepositions);
				cmed.Prepare();
				cmed.ExecuteNonQuery();
			}
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
			Console.WriteLine("Created Outstanding");
			cmd.ExecuteNonQuery();
			con.Close();
		}
		private static void RemoveDuplicates()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			using var cmd = new NpgsqlCommand(removeduplicate, con);
			var x = cmd.ExecuteNonQuery();
			Console.WriteLine("Removed {0} duplicates", x);
			con.Close();
		}

		public static IEnumerable<string> GetOutstanding()
		{
			RemoveDuplicates();
			List<string> outstanding = new List<string>();
			using var con = new NpgsqlConnection(cs);
			con.Open();
			string sql = "SELECT * FROM outstanding";
			using var cmd = new NpgsqlCommand(sql, con);

			using NpgsqlDataReader rdr = cmd.ExecuteReader();

			while (rdr.Read())
			{
				outstanding.Add(rdr.GetString(1));
				Console.WriteLine(rdr.GetString(1));
			}
			con.Close();
			return outstanding;
		}
	}
}