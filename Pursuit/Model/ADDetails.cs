using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using Pursuit.Context;

namespace Pursuit.Model
{
    [BsonIgnoreExtraElements]
    public class ADDetails 
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        [BsonElement("Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = null!;

        [BsonElement("Phone")]
        public string? Phone { get; set; } = null!;

        [BsonElement("Firstname")]
        public string? FirstName { get; set; } = null!;

        [BsonElement("Lastname")]
        public string? LastName { get; set; } = null!;

        [BsonElement("MemberOf")]
        public string? MemberOf { get; set; } = null!;

        [BsonElement("Department")]
        public string? Department { get; set; } = null!;

        [BsonElement("Company")]
        public string? Company { get; set; } = null!;


    }
}
