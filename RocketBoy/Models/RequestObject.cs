using RocketBoy.Models.Interfaces;
using System.Runtime.Serialization;

namespace RocketBoy.Components.Pages.Models;

public class RequestObject : IEventable
{
    public string Name { get; set; } = "New Request";
    public string MethodType { get; set; } = "GET";
    public string? Body { get; set; }
    public List<HeaderObject> Headers { get; set; } = new List<HeaderObject>();
    public string? Url { get; set; }
    public HttpResponseMessage? Response { get; set; }
    public LoadTestParameters? LoadTestParameters { get; set; }
    public string? PostRequestTestJS { get; set; }
    public string? PreRequestTestJS { get; set; }

    // OpenAPI-specific properties
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = new List<string>();

    [IgnoreDataMember]
    public bool Saved { get; set; }
    public bool ChangedWithoutSave { get; set; }
}
