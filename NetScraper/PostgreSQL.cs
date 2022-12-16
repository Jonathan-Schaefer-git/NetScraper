using Npgsql;
using NpgsqlTypes;
using System.Data.SqlTypes;
using System.Reflection;
using System.Text;

namespace NetScraper
{
	internal class PostgreSQL
	{
		private static string removeduplicate = "DELETE FROM outstanding WHERE ID IN (SELECT ID FROM (SELECT id, ROW_NUMBER() OVER (partition BY name ORDER BY ID) AS RowNumber FROM outstanding) AS T WHERE T.RowNumber > 1);";
		private static string cs = "Host=192.168.2.220;Username=postgres;Password=1598;Database=Netscraper";
		public static async Task<bool> ResetOutstandingAsync()
		{
			var con = EstablishDBConnection();
			await con.OpenAsync();
			using var cmd = new NpgsqlCommand();
			cmd.Connection= con;
			cmd.CommandText = "DROP TABLE IF EXISTS outstanding";
			var dropstate = await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"CREATE TABLE outstanding(id SERIAL PRIMARY KEY,name TEXT)";
			Console.WriteLine("Resetted outstanding");
			var createstate = await cmd.ExecuteNonQueryAsync();
			await con.CloseAsync();
			await con.DisposeAsync();
			if(dropstate != -1 && createstate != -1)
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
			var con = EstablishDBConnection();
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
			var con = EstablishDBConnection();
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
			await con.DisposeAsync();
			if(rowcount != -1)
				return true;
			else
			{
				return false;
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
		public static async Task<bool> PushPrioritisedAsync(List<string> prioritised)
		{
			if (prioritised == null)
			{
				Console.WriteLine("prioritised is null");
				return false;
			}
			var con = EstablishDBConnection();
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
			await con.DisposeAsync();
			return true;
		}

		public static async Task<long> GetScrapingCount()
		{
			using var con = new NpgsqlConnection(cs);
			await con.OpenAsync();
			var sql = "SELECT * FROM maindata WHERE ID = (SELECT MAX(id) FROM maindata)";
			var cmd = new NpgsqlCommand(sql, con);
			using NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync();

			await rdr.ReadAsync();
			var x = rdr.GetInt64(0);
			
			await con.CloseAsync();
			await con.DisposeAsync();
			return x;
		}
		public static async Task<bool> PushDocumentListAsync(List<Document> documents)
		{
			var con = EstablishDBConnection();
			await con.OpenAsync();
			var sql = @"INSERT INTO maindata(status, url, datetime, emails, csscount, jscount, approximatesize, links, contentstring, imagedescriptions, imagelinks, imagerelativeposition) VALUES(@status, @url, @datetime, @emails, @csscount, @jscount, @approximatesize, @links, @contentstring, @imagedescriptions ,@imagelinks, @imagerelativeposition)";

			var cmd = new NpgsqlCommand(sql, con);
			foreach (var doc in documents)
			{
				var imagealts = new List<string>();
				var imagelinks = new List<string>();
				var imagepositions = new List<string>();
				if (doc.ContentString == null || doc.ImageData == null)
				{
					return false;
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
				cmd.Parameters.Clear();
				cmd.Parameters.AddWithValue(@"status", doc.Status);
				//This Null Reference check is useless but was added as a safety precussion
				if (doc.Absoluteurl != null)
				{
					cmd.Parameters.AddWithValue(@"url", doc.Absoluteurl.ToString());
				}
				else
				{
					cmd.Parameters.AddWithValue(@"url", "");
					throw new Exception("Website was added without Value");
				}
				cmd.Parameters.AddWithValue(@"datetime", doc.DateTime.ToString());
				if (doc.Emails == null)
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
				await cmd.PrepareAsync();
				var state = await cmd.ExecuteNonQueryAsync();
				await con.CloseAsync();
				await con.DisposeAsync();

				return true;
			}
			return false;
		}
		public static async Task<bool> PushDocumentAsync(Document doc)
		{
			var con = EstablishDBConnection();
			await con.OpenAsync();
			var sql = @"INSERT INTO maindata(status, url, datetime, emails, csscount, jscount, approximatesize, links, contentstring, imagedescriptions, imagelinks, imagerelativeposition) VALUES(@status, @url, @datetime, @emails, @csscount, @jscount, @approximatesize, @links, @contentstring, @imagedescriptions ,@imagelinks, @imagerelativeposition)";

			var cmd = new NpgsqlCommand(sql, con);
			var imagealts = new List<string>();
			var imagelinks = new List<string>();
			var imagepositions = new List<string>();
			if (doc.ContentString == null || doc.ImageData == null)
			{
				return false;
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
			await cmd.PrepareAsync();
			var state = await cmd.ExecuteNonQueryAsync();
			await con.CloseAsync();
			await con.DisposeAsync();
			if (state != -1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static async Task<bool> ResetMainDataAsync()
		{
			var con = EstablishDBConnection();
			await con.OpenAsync().ConfigureAwait(false);
			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;

			cmd.CommandText = "DROP TABLE IF EXISTS maindata cascade";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"CREATE TABLE maindata(id SERIAL PRIMARY KEY, status BOOLEAN, url TEXT, datetime TEXT, emails TEXT[], csscount INTEGER, jscount INTEGER, approximatesize INTEGER, links TEXT[], contentstring TEXT, imagedescriptions TEXT[], imagelinks TEXT[], imagerelativeposition TEXT[])";
			Console.WriteLine("Resetted maindata");
			var x = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
			await con.CloseAsync().ConfigureAwait(false);
			await con.DisposeAsync();
			if (x != -1)
			{
				return true;
			}
			else
			{
				return false;
			}
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
			if(x != -1)
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
			var con = EstablishDBConnection();
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

		public static async Task<IEnumerable<string>> GetPrioritisedAsync()
		{
			List<string> prioritised = new List<string>();
			var con = EstablishDBConnection();
			await con.OpenAsync();
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
		/*
		public void WriteToServer<T>(IEnumerable<T> data, NpgsqlConnection conn, string DestinationTableName)
		{
			try
			{
				if (DestinationTableName == null || DestinationTableName == "")
				{
					throw new ArgumentOutOfRangeException("DestinationTableName", "Destination table must be set");
				}
				PropertyInfo[] properties = typeof(T).GetProperties();
				int colCount = properties.Length;

				NpgsqlDbType[] types = new NpgsqlDbType[colCount];
				int[] lengths = new int[colCount];
				string[] fieldNames = new string[colCount];

				using (var cmd = new NpgsqlCommand("SELECT * FROM " + DestinationTableName + " LIMIT 1", conn))
				{
					using (var rdr = cmd.ExecuteReader())
					{
						if (rdr.FieldCount != colCount)
						{
							throw new ArgumentOutOfRangeException("dataTable", "Column count in Destination Table does not match column count in source table.");
						}
						var columns = rdr.GetColumnSchema();
						for (int i = 0; i < colCount; i++)
						{
							types[i] = (NpgsqlDbType)columns[i].NpgsqlDbType;
							lengths[i] = columns[i].ColumnSize == null ? 0 : (int)columns[i].ColumnSize;
							fieldNames[i] = columns[i].ColumnName;
						}
					}

				}
				var sB = new StringBuilder(fieldNames[0]);
				for (int p = 1; p < colCount; p++)
				{
					sB.Append(", " + fieldNames[p]);
				}
				using (var writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + sB.ToString() + ") FROM STDIN (FORMAT BINARY)"))
				{
					foreach (var t in data)
					{
						writer.StartRow();

						for (int i = 0; i < colCount; i++)
						{
							if (properties[i].GetValue(t) == null)
							{
								writer.WriteNull();
							}
							else
							{
								switch (types[i])
								{
									case NpgsqlDbType.Bigint:
										writer.Write((long)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Bit:
										if (lengths[i] > 1)
										{
											writer.Write((byte[])properties[i].GetValue(t), types[i]);
										}
										else
										{
											writer.Write((byte)properties[i].GetValue(t), types[i]);
										}
										break;
									case NpgsqlDbType.Boolean:
										writer.Write((bool)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Bytea:
										writer.Write((byte[])properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Char:
										if (properties[i].GetType() == typeof(string))
										{
											writer.Write((string)properties[i].GetValue(t), types[i]);
										}
										else if (properties[i].GetType() == typeof(Guid))
										{
											var value = properties[i].GetValue(t).ToString();
											writer.Write(value, types[i]);
										}


										else if (lengths[i] > 1)
										{
											writer.Write((char[])properties[i].GetValue(t), types[i]);
										}
										else
										{

											var s = ((string)properties[i].GetValue(t).ToString()).ToCharArray();
											writer.Write(s[0], types[i]);
										}
										break;
									case NpgsqlDbType.Time:
									case NpgsqlDbType.Timestamp:
									case NpgsqlDbType.TimestampTz:
									case NpgsqlDbType.Date:
										writer.Write((DateTime)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Double:
										writer.Write((double)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Integer:
										try
										{
											if (properties[i].GetType() == typeof(int))
											{
												writer.Write((int)properties[i].GetValue(t), types[i]);
												break;
											}
											else if (properties[i].GetType() == typeof(string))
											{
												var swap = Convert.ToInt32(properties[i].GetValue(t));
												writer.Write((int)swap, types[i]);
												break;
											}
										}
										catch (Exception ex)
										{
											string sh = ex.Message;
										}

										writer.Write((object)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Interval:
										writer.Write((TimeSpan)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Numeric:
									case NpgsqlDbType.Money:
										writer.Write((decimal)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Real:
										writer.Write((Single)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Smallint:

										try
										{
											if (properties[i].GetType() == typeof(byte))
											{
												var swap = Convert.ToInt16(properties[i].GetValue(t));
												writer.Write((short)swap, types[i]);
												break;
											}
											writer.Write((short)properties[i].GetValue(t), types[i]);
										}
										catch (Exception ex)
										{
											string ms = ex.Message;
										}

										break;
									case NpgsqlDbType.Varchar:
									case NpgsqlDbType.Text:
										writer.Write((string)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Uuid:
										writer.Write((Guid)properties[i].GetValue(t), types[i]);
										break;
									case NpgsqlDbType.Xml:
										writer.Write((string)properties[i].GetValue(t), types[i]);
										break;
								}
							}
						}
					}
					writer.Complete();
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error executing NpgSqlBulkCopy.WriteToServer().  See inner exception for details", ex);
			}
		}
		*/
	}
}