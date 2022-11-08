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
		static MongoClient dbClient = new MongoClient(connectionString);

		static IMongoDatabase database = dbClient.GetDatabase("netscraper");
		static IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("core_data");
		public static string connectionString = "http://localhost:27017";
		public static void PushToDB(Document doc)
		{
			
			var document = new BsonDocument { { "student_id", 10000 }, {
				"scores",
				new BsonArray {
				new BsonDocument { { "type", "ID" }, { "score", doc.ID } },
				new BsonDocument { { "type", "quiz" }, { "score", 74.92381029342834 } },
				new BsonDocument { { "type", "homework" }, { "score", 89.97929384290324 } },
				new BsonDocument { { "type", "homework" }, { "score", 82.12931030513218 } }
				}
				}, 
				{ "class_id", 480 }

		};
			MongoClient dbClient = new MongoClient(connectionString);
			if(collection != null)
			{
				collection.InsertOne(document);
			}
		}
	}
}
