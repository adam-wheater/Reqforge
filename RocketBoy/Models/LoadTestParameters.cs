namespace RocketBoy.Components.Pages.Models;

public class LoadTestParameters
{
    /// <summary>
    /// Number of Virtual Users to simulate.
    /// </summary>
    public int VirtualUsers { get; set; } = 10;

    /// <summary>
    /// Duration of the load test (e.g., "30s", "1m").
    /// </summary>
    public string Duration { get; set; } = "30s";
}