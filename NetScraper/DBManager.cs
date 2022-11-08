using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace NetScraper
{
	internal class DBManager
	{
		public static string connectionString = "";
		public static void PushToDB()
		{
			MongoClient dbClient = new MongoClient(connectionString);

			var database = dbClient.GetDatabase("sample_training");
			var collection = database.GetCollection<BsonDocument>("grades");
		}
	}
}
