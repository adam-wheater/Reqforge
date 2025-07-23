using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RocketBoy.Components.Pages.Models;
using RocketBoy.Models;
using RocketBoy.Services;
using System.Text.Json;

namespace RocketBoy.Components.Pages.Request
{
    public partial class Home : ComponentBase
    {
        [Inject] private OpenApiImportService OpenApiImport { get; set; } = null!;
        [Inject] private CollectionService CollectionService { get; set; } = null!;
        [Inject] private SettingsService SettingsService { get; set; } = null!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private ZapService ZapService { get; set; } = null!;
        [Inject] private KeystoreService KeystoreService { get; set; } = null!;

        public List<RequestObject> OpenedTabs { get; set; } = new();
        public RequestObject SelectedTab { get; set; } = new();
        public List<Collection> AllCollections { get; set; } = new();
        public List<HeaderObject> DefaultHeaders { get; set; } = new();
        public Dictionary<RequestObject, string> ResponseBodies { get; set; } = new();

        private Settings Settings { get; set; } = new();
        private bool ShowDefaultHeaders { get; set; }
        private bool isValidJson = true;

        private string TagsAsString
        {
            get => string.Join(", ", SelectedTab.Tags);
            set => SelectedTab.Tags =
                value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(t => t.Trim()).ToList();
        }

        protected override async Task OnInitializedAsync()
        {
            Settings = await SettingsService.LoadSettingsAsync();
            AllCollections = await CollectionService.LoadAllAsync();
            DefaultHeaders = GetHttpClientDefaultHeaders();
            NewTab();
            await KeystoreService.LoadKeys();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Initialize CodeMirror on each .code-editor textarea
                await JSRuntime.InvokeVoidAsync("initializeCodeEditors");
            }
        }

        public async Task ImportOpenApiFile(InputFileChangeEventArgs e)
        {
            using var stream = e.File.OpenReadStream();
            var imported = await OpenApiImport.ImportAsync(stream);
            OpenedTabs.AddRange(imported);
            SelectedTab = OpenedTabs.Last();
            StateHasChanged();
        }

        public void OnCollectionSelected(ChangeEventArgs e)
        {
            var name = e.Value?.ToString();
            var col = AllCollections.FirstOrDefault(c => c.Name == name);
            if (col != null)
            {
                OpenedTabs = col.Requests.ToList();
                SelectedTab = OpenedTabs.First();
            }
        }

        public async Task ShowSaveCollectionDialog()
        {
            var name = await JSRuntime.InvokeAsync<string>("prompt", "Collection name:");
            if (string.IsNullOrWhiteSpace(name)) return;

            var col = new Collection
            {
                Name = name,
                Requests = OpenedTabs.ToList()
            };
            await CollectionService.SaveAsync(col);
            AllCollections = await CollectionService.LoadAllAsync();
        }

        public void SelectTab(RequestObject tab)
        {
            SelectedTab = tab;
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

        public void AddHeader()
            => SelectedTab.Headers.Add(new HeaderObject());

        public void RemoveHeader(HeaderObject h)
            => SelectedTab.Headers.Remove(h);

        public void ToggleDefaultHeaders()
            => ShowDefaultHeaders = !ShowDefaultHeaders;

        private void ValidateJson(ChangeEventArgs e)
        {
            var txt = e.Value?.ToString() ?? "";
            isValidJson = !string.IsNullOrWhiteSpace(txt)
                          && JsonDocument.Parse(txt) is not null;
            if (isValidJson) SelectedTab.Body = txt;
        }

        // **Automatically** run pre-tests, then send, then post-tests
        public async Task SendRequest()
        {
            // 1) run pre-request script
            await RunPreTests();

            if (string.IsNullOrWhiteSpace(SelectedTab.Url))
                return;

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = false
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(100)
            };

            var httpReq = new HttpRequestMessage
            {
                Method = ConvertToHttpMethod(SelectedTab.MethodType),
                RequestUri = new Uri(SelectedTab.Url)
            };

            if (!string.IsNullOrEmpty(SelectedTab.Body)
                && new[] { "POST", "PUT", "PATCH" }.Contains(SelectedTab.MethodType))
            {
                httpReq.Content = new StringContent(
                    SelectedTab.Body,
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            foreach (var hdr in SelectedTab.Headers)
            {
                if (!string.IsNullOrEmpty(hdr.Name)
                    && !string.IsNullOrEmpty(hdr.Value))
                {
                    httpReq.Headers.Remove(hdr.Name);
                    httpReq.Headers.Add(hdr.Name, hdr.Value);
                }
            }

            try
            {
                var resp = await client.SendAsync(httpReq);
                SelectedTab.Response = resp;
                var text = await resp.Content.ReadAsStringAsync();
                ResponseBodies[SelectedTab] = text;
            }
            catch (Exception ex)
            {
                ResponseBodies[SelectedTab] = $"Error: {ex.Message}";
            }

            // 2) run post-request script
            await RunPostTests();
        }

        private async Task RunPreTests()
        {
            var logs = new List<string>();

            var engine = new Jint.Engine()
                .SetValue("console", new
                {
                    log = new Action<object?>(arg =>
                    {
                        logs.Add(arg?.ToString() ?? "null");
                    })
                })
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

            var engine = new Jint.Engine()
                .SetValue("console", new
                {
                    log = new Action<object?>(arg =>
                    {
                        logs.Add(arg?.ToString() ?? "null");
                    })
                })
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

        public async Task SaveRequest()
        {
            var dir = Settings.RequestsSaveLocation;
            Directory.CreateDirectory(dir);

            var safeName = string.Join("_", SelectedTab.Name
                .Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(dir, $"{safeName}.json");

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(SelectedTab, opts);
            await File.WriteAllTextAsync(path, json);

            SelectedTab.Saved = true;
            await JSRuntime.InvokeVoidAsync("alert", $"Saved to {path}");
        }

        public void NewTab()
        {
            var req = new RequestObject();
            OpenedTabs.Add(req);
            SelectedTab = req;
        }

        private static HttpMethod ConvertToHttpMethod(string m) => m switch
        {
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "PATCH" => HttpMethod.Patch,
            "DELETE" => HttpMethod.Delete,
            "OPTIONS" => HttpMethod.Options,
            _ => HttpMethod.Get
        };

        private static List<HeaderObject> GetHttpClientDefaultHeaders()
        {
            var list = new List<HeaderObject>
            {
                new() { Name="Content-Type", Value="application/json" },
                new() { Name="Accept",       Value="application/json" },
                new() { Name="User-Agent",   Value="RocketBoy/1.0" }
            };

            var client = new HttpClient();
            foreach (var h in client.DefaultRequestHeaders)
                foreach (var v in h.Value)
                    list.Add(new HeaderObject { Name = h.Key, Value = v });
            return list;
        }
    }
}