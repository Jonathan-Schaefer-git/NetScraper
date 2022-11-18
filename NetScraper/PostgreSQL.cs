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

		public static List<string>? GetOutstanding()
		{
			using var con = new NpgsqlConnection(cs);
			con.Open();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS outstanding";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"CREATE TABLE cars(id SERIAL PRIMARY KEY,name VARCHAR(255), price INT)";
			cmd.ExecuteNonQuery();

			return null;
		}
	}
}
