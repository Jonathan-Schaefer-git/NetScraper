﻿using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;

namespace NetScraper
{
	internal class DBManager
	{
		private static MongoClient dbClient = new MongoClient(connectionString);
		private static IMongoDatabase database = dbClient.GetDatabase("netscraper");
		private static IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("core_data");
		private static IMongoCollection<BsonDocument>? sdatacollection = database.GetCollection<BsonDocument>("corner_data");
		public static string connectionString = "http://localhost:27017";

		public static void PushDataToDB(Document doc)
		{
			PushHTMLToDB(doc);
			PushSDataToDB(doc);
		}

		private static void PushHTMLToDB(Document doc)
		{
			if (doc.Absoluteurl != null && collection != null)
			{
				var document = new BsonDocument { { "content", doc.ContentString } };

				collection.InsertOne(document);
			}
		}

		public static List<string>? GetOutstandingLinks()
		{

			return null;
		}
		private static void PushSDataToDB(Document doc)
		{
			if (doc.Absoluteurl != null && sdatacollection != null)
			{
				var document = new BsonDocument {
													{ "ResponseTime", doc.ResponseTime },
													{ "Url", doc.Absoluteurl.ToString() },
													{ "DateTime", doc.DateTime },
													{ "Links", new BsonArray{} },
													{ "ImageAlt", new BsonArray{  } },
													{ "Sources", new BsonDocument{ } }
													};
				sdatacollection.InsertOne(document);
			}
		}
	}
}