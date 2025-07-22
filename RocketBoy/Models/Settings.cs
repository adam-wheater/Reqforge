namespace RocketBoy.Models
{
    public class Settings
    {
        public string RequestsSaveLocation { get; set; }
            = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RocketBoy",
                "Requests");

        public string CollectionsSaveLocation { get; set; }
            = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RocketBoy",
                "Collections");

        // Home
        public bool ShowDefaultHeaders { get; set; } = false;

        public bool ShowLoadTestDialog { get; set; } = false;

        // Next
        public int DefaultVirtualUsers { get; set; } = 10;

        public string DefaultLoadTestDuration { get; set; } = "30s";
    }
}