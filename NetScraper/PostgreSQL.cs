using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace NetScraper
{
	internal class PostgreSQL
	{
		private static string cs = "Host=192.168.2.220;Username=postgres;Password=1598;Database=Netscraper";
		private static void CheckIfExists()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();

			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS Outstanding";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"CREATE TABLE Outstanding(id SERIAL PRIMARY KEY,link VARCHAR(255))";
			Console.WriteLine("Created Outstanding");
			cmd.ExecuteNonQuery();

			con.Close();
		}
		public static void PushOutstanding(List<string> outstanding)
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();

			CheckIfExists();

			var sql = "INSERT INTO Outstanding(link) VALUES(@link)";

			using var cmed = new NpgsqlCommand(sql, con);
			foreach (var item in outstanding)
			{
				cmed.Parameters.AddWithValue("link", item);
				cmed.Prepare();
				cmed.ExecuteNonQuery();
			}
			con.Close();
		}
		public static IEnumerable<string> GetOutstanding()
		{
			List<string> outstanding = new List<string>();
			using var con = new NpgsqlConnection(cs);
			con.Open();

			string sql = "SELECT * FROM Outstanding";
			using var cmd = new NpgsqlCommand(sql, con);

			using NpgsqlDataReader rdr = cmd.ExecuteReader();

			while (rdr.Read())
			{
				outstanding.Add(rdr.GetString(1));
			}
			return outstanding;
		}
	}
}
