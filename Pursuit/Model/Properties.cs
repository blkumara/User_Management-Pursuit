using MongoDB.Bson.Serialization.Attributes;

namespace Pursuit.Model
{
    public class Properties
    {
        [BsonElement("Protocol")]
        public string? Protocol { get; set; }

        [BsonElement("FullName")]
        public string? FullName { get; set; }


        [BsonElement("Method")]
        public string? Method { get; set; }

        [BsonElement("ContentType")]
        public string? ContentType { get; set; }

        [BsonElement("ContentLength")]
        public int? ContentLength { get; set; }

        [BsonElement("Scheme")]
        public string? Scheme { get; set; }

        [BsonElement("Host")]
        public string? Host { get; set; }

        [BsonElement("PathBase")]
        public string? PathBase { get; set; }

        [BsonElement("Path")]
        public string? Path { get; set; }

        [BsonElement("QueryString")]
        public string? QueryString { get; set; }

        [BsonElement("HostingRequestStartingLog")]
        public string? HostingRequestStartingLog { get; set; }

        [BsonElement("EventId")]
        public Eventid? EventId { get; set; }

        [BsonElement("SourceContext")]
        public string? SourceContext { get; set; }

        [BsonElement("RequestId")]
        public string? RequestId { get; set; }

        [BsonElement("RequestPath")]
        public string? RequestPath { get; set; }

        [BsonElement("ConnectionId")]
        public string? ConnectionId { get; set; }

        public double? ElapsedMilliseconds { get; set; }

        public string? RouteData { get; set; }
        public int? StatusCode { get; set; }
        public string? MethodInfo { get; set; }
        public string? Controller { get; set; }
        public string? EndpointName { get; set; }
        public string? AssemblyName { get; set; }

        public string? ActionId { get; set; }
        public string? ActionName { get; set; }

        public string? ObjectResultType { get; set; }
        public string? Type { get; set; }
        public string? AuthenticationScheme { get; set; }
        public string? FailureMessage { get; set; }
        public string? HostingRequestFinishedLog { get; set; }
        

    }
}
