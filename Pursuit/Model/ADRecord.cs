using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Pursuit.Context;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Text.Json.Serialization;
/* =========================================================
    Item Name: ADRecord model
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{

    [BsonIgnoreExtraElements]
    //[BsonCollection("MS_AD")]
    public class ADRecord : IAdDoc
    {

        public ADRecord()
        {
         //   Id = ObjectId.GenerateNewId();
        }

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

        [BsonElement("user_origin_code")]
        public string? user_origin_code { get; set; }

        [BsonElement("Role")]
        public virtual Role? Role { get; set; } = null!;

        [BsonElement("description")]
        public string? description { get; set; }

        [BsonElement("useraccountcontrol")]
        public string? useraccountcontrol { get; set; }

        [BsonElement("countrycode")]
        public string? countrycode { get; set; }

        [BsonElement("accountexpires")]
        public string? accountexpires { get; set; }

        [BsonElement("employeeid")]
        public string? employeeid { get; set; }

        [BsonElement("company")]
        public string? company { get; set; }


        [BsonElement("samaccountname")]
        public string? samaccountname { get; set; }

        [BsonElement("instancetype")]
        public string? instancetype { get; set; }

        [BsonElement("l")]
        public string? l { get; set; }

        [BsonElement("cn")]
        public string? cn { get; set; }

        [BsonElement("codepage")]
        public string? codepage { get; set; }

        [BsonElement("manager")]
        public string? manager { get; set; }

        [BsonElement("lastlogoff")]
        public string? lastlogoff { get; set; }

        [BsonElement("department")]
        public string? department { get; set; }

        [BsonElement("initials")]
        public string? initials { get; set; }

        [BsonElement("objectguid")]
        public string? objectguid { get; set; }

        [BsonElement("logoncount")]
        public string? logoncount { get; set; }

        [BsonElement("c")]
        public string? c { get; set; }


        [BsonElement("badpwdcount")]
        public string? badpwdcount { get; set; }

        [BsonElement("pwdlastset")]
        public string? pwdlastset { get; set; }

        [BsonElement("memberof")]
        public string? memberof { get; set; }

        [BsonElement("badpasswordtime")]
        public string? badpasswordtime { get; set; }

        [BsonElement("objectsid")]
        public string? objectsid { get; set; }

        [BsonElement("usnchanged")]
        public string? usnchanged { get; set; }

        [BsonElement("st")]
        public string? st { get; set; }

        [BsonElement("primarygroupid")]
        public string? primarygroupid { get; set; }

        [BsonElement("objectcategory")]
        public string? objectcategory { get; set; }

        [BsonElement("userprincipalname")]
        public string? userprincipalname { get; set; }

        [BsonElement("streetaddress")]
        public string? streetaddress { get; set; }

        [BsonElement("samaccounttype")]
        public string? samaccounttype { get; set; }


        [BsonElement("lastlogon")]
        public string? lastlogon { get; set; }

        [BsonElement("whencreated")]
        public string? whencreated { get; set; }

        [BsonElement("distinguishedname")]
        public string? distinguishedname { get; set; }

        [BsonElement("dscorepropagationdata")]
        public string? dscorepropagationdata { get; set; }

        [BsonElement("displayname")]
        public string? displayname { get; set; }

        [BsonElement("sn")]
        public string? sn { get; set; }

        [BsonElement("usncreated")]
        public string? usncreated { get; set; }

        [BsonElement("postalcode")]
        public string? postalcode { get; set; }

        [BsonElement("objectclass")]
        public string[]? objectclass { get; set; }

        [BsonElement("AzureId")]
        public string? AzureId { get; set; }


        [JsonIgnore]
        [BsonElement("UserDocument")]
        [BsonIgnoreIfDefault]

        public ExpandoObject UserDocument { get; set; } = null!;
    }
}
