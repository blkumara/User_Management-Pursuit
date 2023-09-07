using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Pursuit.Model
{
    public class LoginModel
    {
        [BsonElement("Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string? Email { get; set; } = null!;

        [BsonElement("Password")]
        public string? Password { get; set; } = null!;

        [BsonElement("IsVerified")]
        public Boolean? IsVerified { get; set; } = null!;


    }
}
