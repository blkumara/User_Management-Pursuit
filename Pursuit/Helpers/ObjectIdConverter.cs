using MongoDB.Bson;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
/* =========================================================
    Item Name: Object Id to String conversion - JsonObjectIdConverter
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Helpers
{
    //Used to convert the objec id of mongo db to the string
    public class JsonObjectIdConverter : JsonConverter<ObjectId>
    {
        
        public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new ObjectId(JsonSerializer.Deserialize<string>(ref reader, options));

        public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}