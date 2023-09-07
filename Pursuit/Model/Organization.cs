using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

using Pursuit.Context;
using System.Text.Json.Serialization;
/* =========================================================
Item Name: Organization model
Author: Ortusolis for EvolveAccess Team
Version: 1.0
Copyright 2022 - 2023 - Evolve Access
============================================================ */
namespace Pursuit.Model
{

    public class Organization
    {
        public Organization()
        {
            // Id = ObjectId.GenerateNewId();

            AppUsers = new List<User>();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        [BsonElement("OrganizationName")]
        public string? OrganizationName { get; set; }

        [BsonElement("OrganizationLogo")]
        public string? OrganizationLogo { get; set; }

        [BsonElement("OrgPhone")]
        public string? OrgPhone { get; set; }

        [BsonElement("OrgEmail")]
        public string? OrgEmail { get; set; }

        [BsonElement("OrgAddress")]
        public string? OrgAddress { get; set; }

        [BsonElement("OrgAddress2")]
        public string? OrgAddress2 { get; set; }

        [BsonElement("PinCode")]
        public string? PinCode { get; set; }

        [BsonElement("State")]
        public string? State { get; set; }

        [BsonElement("City")]
        public string? City { get; set; }

        [BsonElement("Country")]
        public string? Country { get; set; }

        [BsonElement("OrgPassword")]
        public string? OrgPassword { get; set; }

        [JsonIgnore]

        [BsonElement("Salt")]
        public byte[] Salt { get; set; } = new byte[0];

        [BsonElement("LicenceKey")]
        public string? LicenceKey { get; set; } = null!;

        [JsonIgnore]

        [BsonElement("LicenceSalt")]
        public byte[] LicenceSalt { get; set; } = new byte[0];

        public Boolean? FirstLogin { get; set; } = false;

        [BsonElement("AppUsers")]
        public IList<User> AppUsers { get; set; } = null!;

        [BsonElement("DateCreated")]
        public DateTime DateCreated => DateTime.Now;

    }
}