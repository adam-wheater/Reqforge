using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using RocketBoy.Components.Pages.Models;
using System.Text.Json;

namespace RocketBoy.Services
{
    public class OpenApiImportService
    {
        public async Task<List<RequestObject>> ImportAsync(Stream openApiStream)
        {
            // Instantiate the reader from the OpenApi.Readers package
            var reader = new OpenApiStreamReader();

            // Read the document and capture any diagnostics
            OpenApiDiagnostic diagnostic;
            OpenApiDocument doc = reader.Read(openApiStream, out diagnostic);

            var requests = new List<RequestObject>();

            // Loop through each path in the spec
            foreach (KeyValuePair<string, OpenApiPathItem> pathEntry in doc.Paths)
            {
                string path = pathEntry.Key;
                OpenApiPathItem pathItem = pathEntry.Value;

                // Loop through each operation under this path (GET, POST, etc.)
                foreach (KeyValuePair<OperationType, OpenApiOperation> opEntry in pathItem.Operations)
                {
                    OperationType method = opEntry.Key;
                    OpenApiOperation operation = opEntry.Value;

                    var req = new RequestObject
                    {
                        Name = operation.OperationId ?? $"{method}_{path}",
                        Url = path,
                        MethodType = method.ToString().ToUpper(),
                        Summary = operation.Summary ?? "",
                        Description = operation.Description ?? "",
                        Tags = operation.Tags.Select(t => t.Name).ToList()
                    };

                    // If there's an application/json requestBody schema with properties…
                    if (operation.RequestBody?.Content.TryGetValue("application/json", out var media) == true
                        && media.Schema?.Properties != null)
                    {
                        var exampleObj = new Dictionary<string, object?>();

                        // media.Schema.Properties is IDictionary<string, OpenApiSchema>
                        foreach (KeyValuePair<string, OpenApiSchema> prop in media.Schema.Properties)
                        {
                            string propName = prop.Key;
                            OpenApiSchema schema = prop.Value;

                            // Basic placeholder by schema.Type string
                            object? placeholder = schema.Type switch
                            {
                                "string" => "",
                                "integer" => 0,
                                "boolean" => false,
                                _ => null
                            };

                            exampleObj[propName] = placeholder;
                        }

                        // Serialize the placeholder object as pretty JSON
                        req.Body = JsonSerializer.Serialize(
                            exampleObj,
                            new JsonSerializerOptions { WriteIndented = true });
                    }

                    // Always include a JSON content-type header
                    req.Headers.Add(new HeaderObject
                    {
                        Name = "Content-Type",
                        Value = "application/json"
                    });

                    requests.Add(req);
                }
            }

            // Return synchronously wrapped in Task
            return await Task.FromResult(requests);
        }
    }
}