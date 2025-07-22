using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RocketBoy.Components.Pages.Models;
using RocketBoy.Models;
using RocketBoy.Services;
using System.Text;
using System.Text.Json;

namespace RocketBoy.Components.Pages.Request
{
    public partial class Home : ComponentBase
    {
        // Dependencies and services
        private DotNetObjectReference<Home>? objRef;

        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] public ZapService ZapService { get; set; } = null!;
        [Inject] public KeystoreService KeystoreService { get; set; } = null!;
        [Inject] public SettingsService SettingsService { get; set; } = null!;

        private Settings Settings { get; set; } = new Settings();
        public List<RequestObject> OpenedTabs { get; set; } = new();
        public RequestObject? SelectedTab { get; set; } = null;
        public bool ShowDefaultHeaders { get; set; } = false;
        public List<HeaderObject> DefaultHeaders { get; set; } = GetHttpClientDefaultHeaders();
        private bool isValidJson = true;
        private string ZapApiKey { get; set; }
        private string ZapBaseUrl { get; set; }
        public bool HasGeneratedOpenApiSpec { get; set; } = false;
        public bool HasRunSecurityTest { get; set; } = false;

        private bool IsK6Available()
        {
            try
            {
                var k6Path = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator)
                    .Select(p => Path.Combine(p, "k6.exe")).FirstOrDefault(File.Exists);
                return !string.IsNullOrEmpty(k6Path);
            }
            catch
            {
                return false;
            }
        }

        private bool IsZapAvailable()
        {
            if (string.IsNullOrWhiteSpace(ZapApiKey) || string.IsNullOrWhiteSpace(ZapBaseUrl))
            {
                return false;
            }

            try
            {
                HttpClient httpClient = new();
                return ZapService.GetVersion(httpClient).Result;
            }
            catch
            {
                return false;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await KeystoreService.LoadKeys();
            ZapApiKey = KeystoreService.GetKey("ZapApiKey") ?? string.Empty;
            ZapBaseUrl = KeystoreService.GetKey("ZapBaseUrl") ?? string.Empty;

            Settings = await SettingsService.LoadSettingsAsync();
            ShowDefaultHeaders = Settings.ShowDefaultHeaders;

            if (OpenedTabs == null || OpenedTabs.Count == 0) { NewTab(); }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                objRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("setupKeyHandler", objRef);
            }
        }

        public static List<HeaderObject> GetHttpClientDefaultHeaders()
        {
            var headerItems = new List<HeaderObject>
            {
                new HeaderObject { Name = "Content-Type", Value = "application/json" },
                new HeaderObject { Name = "Accept", Value = "application/json" },
                new HeaderObject { Name = "User-Agent", Value = "RocketBoy/1.0" }
            };

            HttpClient httpClient = new();
            foreach (var header in httpClient.DefaultRequestHeaders)
            {
                if (!string.IsNullOrEmpty(header.Key) && header.Value != null && header.Value.Any())
                {
                    foreach (var value in header.Value)
                    {
                        headerItems.Add(new HeaderObject { Name = header.Key, Value = value });
                    }
                }
            }

            return headerItems;
        }

        public async Task SendRequest()
        {
            if (SelectedTab == null || string.IsNullOrEmpty(SelectedTab.Url))
            {
                Console.WriteLine("Request cannot be sent: Invalid input.");
                return;
            }

            HttpClientHandler handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = false
            };

            HttpClient httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(100)  // Increase the timeout if needed
            };

            HttpRequestMessage request = new()
            {
                Method = ConvertToHttpMethod(SelectedTab.MethodType),
                RequestUri = new Uri(SelectedTab.Url)
            };

            if ((SelectedTab.MethodType == "POST" || SelectedTab.MethodType == "PUT" || SelectedTab.MethodType == "PATCH")
                && !string.IsNullOrEmpty(SelectedTab.Body))
            {
                request.Content = new StringContent(SelectedTab.Body, Encoding.UTF8, "application/json");
            }

            foreach (var header in SelectedTab.Headers)
            {
                if (!string.IsNullOrEmpty(header.Name) && !string.IsNullOrEmpty(header.Value))
                {
                    request.Headers.Add(header.Name, header.Value);
                }
            }

            try
            {
                SelectedTab.Response = await httpClient.SendAsync(request);
                Console.WriteLine($"Request sent successfully: {SelectedTab.Response.StatusCode}");
            }
            catch (HttpRequestException httpRequestException)
            {
                Console.WriteLine($"An HTTP error occurred while sending the request: {httpRequestException.Message}");
            }
            catch (TaskCanceledException taskCanceledException)
            {
                if (!taskCanceledException.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("The request timed out.");
                }
                else
                {
                    Console.WriteLine($"The request was canceled: {taskCanceledException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending the request: {ex.Message}");
            }
        }

        private void ValidateJson(ChangeEventArgs e)
        {
            var jsonText = e.Value?.ToString();
            isValidJson = IsValidJson(jsonText ?? "");
            SelectedTab.Body = jsonText ?? "";
        }

        private bool IsValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            try
            {
                using (var doc = JsonDocument.Parse(input))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public HttpMethod ConvertToHttpMethod(string type) => type switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            "OPTIONS" => HttpMethod.Options,
            _ => HttpMethod.Get
        };

        public void SelectTab(RequestObject requestObject)
        {
            SelectedTab = requestObject;
            HasGeneratedOpenApiSpec = false;
            HasRunSecurityTest = false;
        }

        public void NewTab()
        {
            RequestObject requestObject = new();
            OpenedTabs.Add(requestObject);
            SelectedTab = requestObject;
            HasGeneratedOpenApiSpec = false;
            HasRunSecurityTest = false;
        }

        [JSInvokable]
        public async Task HandleCtrlS()
        {
            Console.WriteLine("Ctrl + S was pressed in the context of the component.");
            //save
        }

        public async Task CloseTab(RequestObject requestObject)
        {
            if (!requestObject.Saved || requestObject.ChangedWithoutSave)
            {
                var confirmed = await JSRuntime.InvokeAsync<bool>("confirmCloseTab", "You have unsaved changes. Do you want to save them before closing?");
                if (confirmed)
                {
                    //save
                }
            }

            OpenedTabs.Remove(requestObject);

            if (requestObject == SelectedTab)
            {
                SelectedTab = null;
            }

            StateHasChanged();
        }

        public void AddHeader()
        {
            if (SelectedTab != null)
            {
                SelectedTab.Headers.Add(new HeaderObject());
            }
        }

        public void RemoveHeader(HeaderObject header)
        {
            if (SelectedTab != null)
            {
                SelectedTab.Headers.Remove(header);
            }
        }

        public void ToggleDefaultHeaders()
        {
            ShowDefaultHeaders = !ShowDefaultHeaders;
        }

        private string TagsAsString
        {
            get => string.Join(", ", SelectedTab.Tags);
            set
            {
                SelectedTab.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(tag => tag.Trim())
                                        .ToList();
            }
        }
    }
}