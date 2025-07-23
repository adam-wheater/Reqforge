using RocketBoy.Models.Interfaces;
using System.Net;
using System.Runtime.Serialization;

namespace RocketBoy.Components.Pages.Models
{
    public class RequestObject : IEventable
    {
        public string Name { get; set; } = "New Request";
        public string MethodType { get; set; } = "GET";
        public string? Body { get; set; }
        public List<HeaderObject> Headers { get; set; } = new();
        public string? Url { get; set; }
        public HttpResponseMessage? Response { get; set; }
        public LoadTestParameters? LoadTestParameters { get; set; }
        public string? PostRequestTestJS { get; set; }
        public string? PreRequestTestJS { get; set; }

        // Test results
        public string? PreTestResults { get; set; }
        public string? PostTestResults { get; set; }

        // OpenAPI metadata
        public string Summary { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Tags { get; set; } = new();

        // HTTP response status & timing
        public HttpStatusCode? StatusCode { get; set; }
        public TimeSpan? ResponseTime { get; set; }

        [IgnoreDataMember]
        public bool Saved { get; set; }
        public bool ChangedWithoutSave { get; set; }
    }
}
