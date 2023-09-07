using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using Pursuit.Context;
/* =========================================================
    Item Name: Connection_Settings model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{

    [BsonCollection("ConnectionMgt")]
    public class Connection_Setting : IDocument
    {
        public Connection_Setting()
        {
          //  Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
      
        public string? Connection_Type { get; set; }

        public ICollection<Connection> Connections { get; set; } = null!;

        [BsonElement("DateCreated")]
        public DateTime DateCreated => DateTime.Now;



    }
}
