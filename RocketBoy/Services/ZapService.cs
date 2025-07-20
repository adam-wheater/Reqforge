using System.Text.Json;

namespace RocketBoy.Services
{
    public class ZapService
    {
        private readonly ZapSettings _settings;

        public ZapService(ZapSettings settings)
        {
            _settings = settings;
        }
        public async Task<string> GetVersion(HttpClient httpClient)
        {
            var requestUrl = $"{_settings.BaseUrl}/JSON/core/view/version/?apikey={_settings.ApiKey}";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("version").GetString()!;
        }

        public async Task<string> GetSpiderStatus(HttpClient httpClient, int scanId)
        {
            var requestUrl = $"{_settings.BaseUrl}/JSON/spider/view/status/?apikey={_settings.ApiKey}&scanId={scanId}";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseBody);
            var status = jsonDocument.RootElement.GetProperty("status").GetString();
            return status;
        }

        public async Task<string> GetActiveScanStatus(HttpClient httpClient, int scanId)
        {
            var requestUrl = $"{_settings.BaseUrl}/JSON/ascan/view/status/?apikey={_settings.ApiKey}&scanId={scanId}";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseBody);
            var status = jsonDocument.RootElement.GetProperty("status").GetString();
            return status;
        }

        public async Task<string> AddUrlToContext(HttpClient httpClient, string contextName, string targetUrl)
        {
            string encodedUrl = Uri.EscapeDataString(targetUrl);
            string requestUrl = $"{_settings.BaseUrl}/JSON/context/action/includeInContext/?apikey={_settings.ApiKey}&contextName={contextName}&regex={encodedUrl}.*";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<int> RunSpiderScan(HttpClient httpClient, string targetUrl)
        {
            string encodedUrl = Uri.EscapeDataString(targetUrl);
            string requestUrl = $"{_settings.BaseUrl}/JSON/spider/action/scan/?apikey={_settings.ApiKey}&url={encodedUrl}";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Log the response for debugging purposes
            Console.WriteLine($"RunSpiderScan response body: {responseBody}");

            var jsonDocument = JsonDocument.Parse(responseBody);
            var scanIdProperty = jsonDocument.RootElement.GetProperty("scan");

            int scanId;
            if (scanIdProperty.ValueKind == JsonValueKind.String)
            {
                if (!int.TryParse(scanIdProperty.GetString(), out scanId))
                {
                    throw new InvalidOperationException("Scan ID is not a valid number.");
                }
            }
            else
            {
                scanId = scanIdProperty.GetInt32();
            }

            return scanId;
        }

        public async Task<int> StartActiveScan(HttpClient httpClient, string targetUrl)
        {
            string encodedUrl = Uri.EscapeDataString(targetUrl);
            string requestUrl = $"{_settings.BaseUrl}/JSON/ascan/action/scan/?apikey={_settings.ApiKey}&url={encodedUrl}";
            Console.WriteLine($"Sending request to ZAP API: {requestUrl}");

            var response = await httpClient.GetAsync(requestUrl);
            string responseBody = await response.Content.ReadAsStringAsync();

            // Log the response for debugging purposes
            Console.WriteLine($"StartActiveScan response body: {responseBody}");

            response.EnsureSuccessStatusCode();

            var jsonDocument = JsonDocument.Parse(responseBody);
            var scanIdProperty = jsonDocument.RootElement.GetProperty("scan");

            int scanId;
            if (scanIdProperty.ValueKind == JsonValueKind.String)
            {
                if (!int.TryParse(scanIdProperty.GetString(), out scanId))
                {
                    throw new InvalidOperationException("Scan ID is not a valid number.");
                }
            }
            else
            {
                scanId = scanIdProperty.GetInt32();
            }

            return scanId;
        }

        public async Task<string> GetSpiderResults(HttpClient httpClient, int scanId)
        {
            var requestUrl = $"{_settings.BaseUrl}/JSON/spider/view/status/?apikey={_settings.ApiKey}&scanId={scanId}";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetScanResults(HttpClient httpClient, int scanId)
        {
            var requestUrl = $"{_settings.BaseUrl}/JSON/ascan/view/scanProgress/?apikey={_settings.ApiKey}&scanId={scanId}";
            HttpResponseMessage response = null;
            string responseBody = null;
            try
            {
                response = await httpClient.GetAsync(requestUrl);
                responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"GetScanResults response body: {responseBody}");

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex.Message}");
                if (response != null)
                {
                    Console.WriteLine($"Response status code: {response.StatusCode}");
                    Console.WriteLine($"Response body: {responseBody}");
                }
                throw;
            }
            return responseBody;
        }

        public async Task<string> GetAlerts(HttpClient httpClient, string targetUrl)
        {
            var requestUrl = $"{_settings.BaseUrl}/JSON/core/view/alerts/?apikey={_settings.ApiKey}&baseurl={Uri.EscapeDataString(targetUrl)}";
            HttpResponseMessage response = null;
            string responseBody = null;
            try
            {
                response = await httpClient.GetAsync(requestUrl);
                responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"GetAlerts response body: {responseBody}");

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex.Message}");
                if (response != null)
                {
                    Console.WriteLine($"Response status code: {response.StatusCode}");
                    Console.WriteLine($"Response body: {responseBody}");
                }
                throw;
            }
            return responseBody;
        }
    }

    public class ZapSettings
    {
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
