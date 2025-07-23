using Jint;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RocketBoy.Components.Pages.Models;
using RocketBoy.Models;
using RocketBoy.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RocketBoy.Components.Pages.Request
{
    public partial class Home : ComponentBase, IDisposable
    {
        [Inject] private RequestStore RequestStore { get; set; } = null!;
        [Inject] private CollectionService CollectionService { get; set; } = null!;
        [Inject] private SettingsService SettingsService { get; set; } = null!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private KeystoreService KeystoreService { get; set; } = null!;

        public List<RequestObject> OpenedTabs { get; set; } = new();
        public RequestObject SelectedTab { get; set; } = new();
        public List<Collection> AllCollections { get; set; } = new();
        public List<HeaderObject> DefaultHeaders { get; set; } = new();
        public Dictionary<RequestObject, string> ResponseBodies { get; set; } = new();

        private Settings Settings { get; set; } = new();
        private bool ShowDefaultHeaders { get; set; }
        private bool isValidJson { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            Settings = await SettingsService.LoadSettingsAsync();
            AllCollections = await CollectionService.LoadAllAsync();
            DefaultHeaders = GetHttpClientDefaultHeaders();
            NewTab();
            await KeystoreService.LoadKeys();

            RequestStore.OnChange += PopulateImportedRequests;
            PopulateImportedRequests();
        }

        private void PopulateImportedRequests()
        {
            foreach (var req in RequestStore.Requests)
                if (!OpenedTabs.Contains(req))
                    OpenedTabs.Add(req);

            if (OpenedTabs.Any() && SelectedTab == null)
                SelectedTab = OpenedTabs.First();

            StateHasChanged();
        }

        public void Dispose()
            => RequestStore.OnChange -= PopulateImportedRequests;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await JSRuntime.InvokeVoidAsync("initializeCodeEditors");
        }

        private string TagsAsString
        {
            get => string.Join(", ", SelectedTab.Tags);
            set => SelectedTab.Tags = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
        }

        #region Collections/Tabs

        public async Task ShowSaveCollectionDialog()
        {
            var name = await JSRuntime.InvokeAsync<string>("prompt", "Collection name:");
            if (string.IsNullOrWhiteSpace(name)) return;

            var col = new Collection { Name = name, Requests = OpenedTabs.ToList() };
            await CollectionService.SaveAsync(col);
            AllCollections = await CollectionService.LoadAllAsync();
        }

        public void SelectTab(RequestObject tab) => SelectedTab = tab;

        public void NewTab()
        {
            var req = new RequestObject();
            OpenedTabs.Add(req);
            SelectedTab = req;
        }

        public async Task CloseTab(RequestObject tab)
        {
            if (!tab.Saved && tab.ChangedWithoutSave)
            {
                bool save = await JSRuntime.InvokeAsync<bool>(
                    "confirm", "Unsaved changes—save before closing?");
                if (save) await SaveRequest();
            }

            OpenedTabs.Remove(tab);
            if (SelectedTab == tab)
                SelectedTab = OpenedTabs.FirstOrDefault() ?? new RequestObject();
        }

        #endregion Collections/Tabs

        #region Headers/JSON

        public void AddHeader() => SelectedTab.Headers.Add(new HeaderObject());

        public void RemoveHeader(HeaderObject h) => SelectedTab.Headers.Remove(h);

        public void ToggleDefaultHeaders() => ShowDefaultHeaders = !ShowDefaultHeaders;

        private void ValidateJson(ChangeEventArgs e)
        {
            var txt = e.Value?.ToString() ?? "";
            try
            {
                JsonDocument.Parse(txt);
                isValidJson = true;
                SelectedTab.Body = txt;
            }
            catch
            {
                isValidJson = false;
            }
        }

        #endregion Headers/JSON

        #region Send + Tests

        public async Task SendRequest()
        {
            await RunPreTests();

            if (string.IsNullOrWhiteSpace(SelectedTab.Url)) return;

            var sw = Stopwatch.StartNew();
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };

            var req = new HttpRequestMessage(
                new HttpMethod(SelectedTab.MethodType),
                SelectedTab.Url);

            if (!string.IsNullOrEmpty(SelectedTab.Body)
                && new[] { "POST", "PUT", "PATCH" }.Contains(SelectedTab.MethodType))
            {
                req.Content = new StringContent(
                    SelectedTab.Body, Encoding.UTF8, "application/json");
            }

            foreach (var h in SelectedTab.Headers.Where(x => !string.IsNullOrEmpty(x.Name)))
            {
                req.Headers.Remove(h.Name);
                req.Headers.Add(h.Name, h.Value);
            }

            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(req);
            }
            catch (Exception ex)
            {
                ResponseBodies[SelectedTab] = $"Error: {ex.Message}";
                SelectedTab.StatusCode = null;
                SelectedTab.ResponseTime = sw.Elapsed;
                return;
            }
            sw.Stop();

            SelectedTab.Response = resp;
            SelectedTab.StatusCode = resp.StatusCode;
            SelectedTab.ResponseTime = sw.Elapsed;
            ResponseBodies[SelectedTab] = await resp.Content.ReadAsStringAsync();

            await RunPostTests();
        }

        private async Task RunPreTests()
        {
            var logs = new List<string>();
            var engine = new Engine()
                .SetValue("console", new { log = new Action<object?>(x => logs.Add(x?.ToString() ?? "null")) })
                .SetValue("request", SelectedTab);

            try
            {
                engine.Execute(SelectedTab.PreRequestTestJS ?? "");
                SelectedTab.PreTestResults = logs.Count > 0
                    ? string.Join("\n", logs)
                    : "[no output]";
            }
            catch (Exception ex)
            {
                SelectedTab.PreTestResults = $"Error: {ex.Message}";
            }
        }

        private async Task RunPostTests()
        {
            var logs = new List<string>();
            var body = SelectedTab.Response is null
                ? ""
                : await SelectedTab.Response.Content.ReadAsStringAsync();

            var engine = new Engine()
                .SetValue("console", new { log = new Action<object?>(x => logs.Add(x?.ToString() ?? "null")) })
                .SetValue("responseBody", body)
                .SetValue("response", SelectedTab.Response);

            try
            {
                engine.Execute(SelectedTab.PostRequestTestJS ?? "");
                SelectedTab.PostTestResults = logs.Count > 0
                    ? string.Join("\n", logs)
                    : "[no output]";
            }
            catch (Exception ex)
            {
                SelectedTab.PostTestResults = $"Error: {ex.Message}";
            }
        }

        #endregion Send + Tests

        #region Save

        public async Task SaveRequest()
        {
            var dir = Settings.RequestsSaveLocation;
            Directory.CreateDirectory(dir);
            var safe = string.Join("_", SelectedTab.Name.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(dir, $"{safe}.json");
            var opts = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(SelectedTab, opts));
            SelectedTab.Saved = true;
            await JSRuntime.InvokeVoidAsync("alert", $"Saved to {path}");
        }

        #endregion Save

        #region Helpers

        private static List<HeaderObject> GetHttpClientDefaultHeaders()
        {
            var list = new List<HeaderObject>
            {
                new() { Name="Content-Type", Value="application/json" },
                new() { Name="Accept",       Value="application/json" },
                new() { Name="User-Agent",   Value="RocketBoy/1.0" }
            };
            using var client = new HttpClient();
            foreach (var h in client.DefaultRequestHeaders)
                foreach (var v in h.Value)
                    list.Add(new HeaderObject { Name = h.Key, Value = v });
            return list;
        }

        private string GetStatusBadge()
            => SelectedTab.StatusCode switch
            {
                var c when ((int?)c >= 200 && (int?)c < 300) => "bg-success",
                var c when ((int?)c >= 300 && (int?)c < 400) => "bg-warning",
                _ => "bg-danger"
            };

        #endregion Helpers
    }
}