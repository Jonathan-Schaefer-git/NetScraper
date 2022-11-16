using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
		static MongoClient dbClient = new MongoClient(connectionString);
		static IMongoDatabase database = dbClient.GetDatabase("netscraper");
		static IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("core_data");
		static IMongoCollection<BsonDocument>? sdatacollection = database.GetCollection<BsonDocument>("corner_data");
		public static string connectionString = "http://localhost:27017";

		public static void PushDataToDB(Document doc)
		{
			PushHTMLToDB(doc);
			PushSDataToDB(doc);
		}


		private static void PushHTMLToDB(Document doc)
		{
			if(doc.absoluteurl != null && collection != null)
			{
				var document = new BsonDocument { { "website_id", doc.ID }, { "conte", doc.ContentString } };
					
				collection.InsertOne(document);
			}
		}
		private static void PushSDataToDB(Document doc)
		{
			if (doc.absoluteurl != null && sdatacollection != null)
			{
				var document = new BsonDocument { 
													{ "website_id", doc.ID }, 
													{ "responsetime", doc.ResponseTime }, 
													{ "url", doc.absoluteurl.ToString() }, 
													{ "datetime", doc.DateTime },
													{ "links", new BsonArray{	 } },
													{ "imagelinks", new BsonArray{  } }
													};
				sdatacollection.InsertOne(document);
			}
		}
		
	}
}
