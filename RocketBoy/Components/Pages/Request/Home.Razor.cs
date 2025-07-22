using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RocketBoy.Components.Pages.Models;
using RocketBoy.Models;
using RocketBoy.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RocketBoy.Components.Pages.Request
{
    public partial class Home : ComponentBase
    {
        // Dependencies and services
        private DotNetObjectReference<Home>? objRef;

        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] public OpenApiService OpenApiService { get; set; } = null!;
        [Inject] public ZapSettings ZapSettings { get; set; } = null!;
        [Inject] public ZapService ZapService { get; set; } = null!;
        [Inject] public KeystoreService KeystoreService { get; set; } = null!;
        [Inject] public NavigationManager Navigation { get; set; } = null!;
        [Inject] public SettingsService SettingsService { get; set; } = null!;

        private Settings Settings { get; set; } = new Settings();

        // State properties
        public List<RequestObject> OpenedTabs { get; set; } = new();

        public List<Collection> Collections { get; set; } = new();
        public RequestObject? SelectedTab { get; set; } = null;
        public LoadTestParameters LoadTestFormModel { get; set; } = new();
        public bool ShowLoadTestDialog { get; set; } = false;
        public bool ShowDefaultHeaders { get; set; } = false;
        public List<HeaderObject> DefaultHeaders { get; set; } = GetHttpClientDefaultHeaders();
        public bool LoadTestInProgress { get; set; } = false;
        public string LoadTestProgressOutput { get; set; } = string.Empty;
        public string? LoadTestOutput { get; set; } = null;
        private string openApiSpecJson;
        private bool isValidJson = true;
        public bool SecurityTestInProgress { get; set; } = false;
        public string SecurityTestProgressOutput { get; set; } = string.Empty;
        public string? SecurityTestOutput { get; set; } = null;
        public List<string> SecurityTestDetails { get; set; } = new();

        private string ZapApiKey { get; set; }
        private string ZapBaseUrl { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await KeystoreService.LoadKeys();
            ZapApiKey = KeystoreService.GetKey("ZapApiKey") ?? string.Empty;
            ZapBaseUrl = KeystoreService.GetKey("ZapBaseUrl") ?? string.Empty;

            Settings = await SettingsService.LoadSettingsAsync();
            ShowDefaultHeaders = Settings.ShowDefaultHeaders;
            ShowLoadTestDialog = Settings.ShowLoadTestDialog;

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

        public async Task SubmitLoadTestForm()
        {
            if (SelectedTab == null || LoadTestFormModel.VirtualUsers < 1 || !Regex.IsMatch(LoadTestFormModel.Duration, @"^\d+(s|m|h)$")) return;

            SelectedTab.LoadTestParameters = LoadTestFormModel;
            ShowLoadTestDialog = false;
            LoadTestInProgress = true;
            LoadTestProgressOutput = "Load test is starting...\n";
            LoadTestOutput = null;
            string accumulatedLogs = LoadTestProgressOutput;
            StateHasChanged();

            try
            {
                string scriptPath = GenerateTestScript(
                    LoadTestFormModel.VirtualUsers,
                    LoadTestFormModel.Duration,
                    SelectedTab.Url,
                    SelectedTab.MethodType,
                    JsonSerializer.Serialize(SelectedTab.Headers),
                    SelectedTab.Body ?? string.Empty
                );

                string k6Command = $"k6 run {scriptPath}";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/C {k6Command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var process = new Process { StartInfo = processInfo };
                process.Start();

                while (!process.HasExited)
                {
                    string progressLine = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrEmpty(progressLine))
                    {
                        LoadTestProgressOutput += progressLine + "\n";
                        accumulatedLogs += progressLine + "\n";
                        StateHasChanged();
                    }
                }

                string remainingOutput = await process.StandardOutput.ReadToEndAsync();
                string errorOutput = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                LoadTestInProgress = false;

                accumulatedLogs += remainingOutput + "\n";
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    accumulatedLogs += $"Errors occurred:\n{errorOutput}";
                }

                LoadTestOutput = accumulatedLogs;
            }
            catch (Exception ex)
            {
                LoadTestOutput = $"An error occurred: {ex.Message}";
            }

            StateHasChanged();
        }

        public static string GenerateTestScript(
            int vus, string duration, string url, string method,
            string headersJson, string body)
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, "wwwroot/js/k6_test_template.js");
            string template = File.ReadAllText(templatePath);
            string bodyValue = string.IsNullOrEmpty(body) ? "null" : $"'{body}'";
            string script = template
                .Replace("__VUS__", vus.ToString())
                .Replace("__DURATION__", duration)
                .Replace("__URL__", url)
                .Replace("__METHOD__", method)
                .Replace("__HEADERS__", headersJson)
                .Replace("__BODY__", bodyValue);
            string outputScriptPath = Path.Combine(FileSystem.AppDataDirectory, "k6_test_script.js");
            File.WriteAllText(outputScriptPath, script);

            return outputScriptPath;
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
            LoadTestFormModel = requestObject.LoadTestParameters ?? new LoadTestParameters();
        }

        public void NewTab()
        {
            RequestObject requestObject = new();
            OpenedTabs.Add(requestObject);
            SelectedTab = requestObject;
        }

        [JSInvokable]
        public async Task HandleCtrlS()
        {
            Console.WriteLine("Ctrl + S was pressed in the context of the component.");
            //save
        }

        public void Dispose()
        {
            objRef?.Dispose();
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

        public async Task GenerateOpenAPISpecification()
        {
            openApiSpecJson = OpenApiService.GenerateOpenAPISpec(OpenedTabs);
            StateHasChanged();
        }

        public void DownloadOpenAPISpec()
        {
            var bytes = Encoding.UTF8.GetBytes(openApiSpecJson);
            var fileName = "openapi_spec.json";
            var mimeType = "application/json";
            JSRuntime.InvokeVoidAsync("saveAsFile", fileName, Convert.ToBase64String(bytes), mimeType);
        }

        public async Task StartSecurityTest()
        {
            if (SelectedTab == null || string.IsNullOrEmpty(SelectedTab.Url))
            {
                Console.WriteLine("Security test cannot be started: Invalid input.");
                return;
            }

            SecurityTestInProgress = true;
            SecurityTestProgressOutput = "Security test is starting...\n";
            SecurityTestOutput = null;
            SecurityTestDetails.Clear();
            StateHasChanged();

            var zapApiKey = KeystoreService.GetKey("ZapApiKey");
            var zapBaseUrl = KeystoreService.GetKey("ZapBaseUrl");
            if (string.IsNullOrEmpty(zapApiKey) || string.IsNullOrEmpty(zapBaseUrl))
            {
                SecurityTestOutput = "ZAP settings are not properly configured.";
                SecurityTestInProgress = false;
                StateHasChanged();
                return;
            }

            HttpClient httpClient = new();
            var zapService = new ZapService(new ZapSettings { ApiKey = zapApiKey, BaseUrl = zapBaseUrl });

            try
            {
                var version = await zapService.GetVersion(httpClient);
                Console.WriteLine($"Detected ZAP version {version}");
            }
            catch
            {
                await JSRuntime.InvokeVoidAsync("alert",
                    "Unable to contact OWASP ZAP at " + zapBaseUrl +
                    ".\n\nPlease make sure OWASP ZAP is installed and running (download from https://www.zaproxy.org/download/).");
                SecurityTestInProgress = false;
                return;
            }

            try
            {
                // Add URL to context
                var contextName = "Default Context";
                var addUrlResponse = await zapService.AddUrlToContext(httpClient, contextName, SelectedTab.Url);
                SecurityTestProgressOutput += $"Added URL to context: {addUrlResponse}\n";
                StateHasChanged();

                // Run Spider scan
                var spiderScanId = await zapService.RunSpiderScan(httpClient, SelectedTab.Url);
                SecurityTestProgressOutput += $"Spider scan started: {spiderScanId}\n";
                StateHasChanged();

                bool isSpiderCompleted = false;
                while (!isSpiderCompleted)
                {
                    var progress = await zapService.GetSpiderStatus(httpClient, spiderScanId);
                    SecurityTestProgressOutput += $"Spider scan progress: {progress}\n";
                    StateHasChanged();

                    if (progress == "100") { isSpiderCompleted = true; }
                    else { await Task.Delay(5000); } // Wait for 5 seconds before polling again
                }

                // Run Active scan
                var activeScanId = await zapService.StartActiveScan(httpClient, SelectedTab.Url);
                SecurityTestProgressOutput += $"Active scan started: {activeScanId}\n";
                StateHasChanged();

                bool isActiveScanCompleted = false;
                while (!isActiveScanCompleted)
                {
                    var progress = await zapService.GetActiveScanStatus(httpClient, activeScanId);
                    SecurityTestProgressOutput += $"Active scan progress: {progress}\n";
                    StateHasChanged();

                    if (progress == "100") { isActiveScanCompleted = true; }
                    else { await Task.Delay(5000); } // Wait for 5 seconds before polling again
                }

                var alerts = await zapService.GetAlerts(httpClient, SelectedTab.Url);
                SecurityTestOutput = $"Security test completed. Alerts: {alerts}";
            }
            catch (HttpRequestException httpRequestException)
            {
                SecurityTestOutput = $"An error occurred during the security test: {httpRequestException.Message}";
            }
            catch (Exception ex)
            {
                SecurityTestOutput = $"An unexpected error occurred during the security test: {ex.Message}";
            }

            SecurityTestInProgress = false;
            StateHasChanged();
        }

        private void ParseScanResults(string progress)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(progress))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("scanProgress", out JsonElement scanProgress))
                    {
                        JsonElement hostProcess = scanProgress[1].GetProperty("HostProcess");

                        foreach (JsonElement plugin in hostProcess.EnumerateArray())
                        {
                            string pluginName = plugin[0].GetString();
                            string pluginId = plugin[1].GetString();
                            string pluginStatus = plugin[3].GetString();
                            string pluginCount = plugin[5].GetString();

                            SecurityTestDetails.Add($"{pluginName} (ID: {pluginId}) - {pluginStatus} - Issues: {pluginCount}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SecurityTestDetails.Add($"Failed to parse scan results: {ex.Message}");
            }
        }

        public void DownloadSecurityTestResults()
        {
            var bytes = Encoding.UTF8.GetBytes(SecurityTestOutput ?? string.Empty);
            var fileName = "security_test_results.txt";
            var mimeType = "text/plain";
            JSRuntime.InvokeVoidAsync("saveAsFile", fileName, Convert.ToBase64String(bytes), mimeType);
        }
    }
}