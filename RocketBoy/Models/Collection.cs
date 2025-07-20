using RocketBoy.Components.Pages.Models;
using RocketBoy.Models.Interfaces;

namespace RocketBoy.Models;

public class Collection : IEventable
{
    public string Name { get; set; } = "New Collection";
    public List<RequestObject> Requests { get; set; } = [];
    public List<FolderObject> Folders { get; set; } = [];

    public string? PostRequestTestJS { get; set; }
    public string? PreRequestTestJS { get; set; }
}
