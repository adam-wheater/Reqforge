using Microsoft.OpenApi.Models;
using RocketBoy.Components.Pages.Models;

public class OpenApiService
{
    public string GenerateOpenAPISpec(IEnumerable<RequestObject> requests)
    {
        var openApiDocument = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Version = "1.0.0",
                Title = "Generated API",
                Description = "Automatically generated OpenAPI Specification"
            },
            Paths = new OpenApiPaths()
        };

        foreach (var request in requests)
        {
            if (!openApiDocument.Paths.ContainsKey(request.Url))
                openApiDocument.Paths.Add(request.Url, new OpenApiPathItem());

            var operation = new OpenApiOperation
            {
                Tags = request.Tags.Select(tag => new OpenApiTag { Name = tag }).ToList(),
                Summary = request.Summary,
                Description = request.Description,
                OperationId = request.Name.Replace(" ", "_"),
                Responses = new OpenApiResponses
                {
                    { "200", new OpenApiResponse { Description = "Success" } }
                }
            };

            // Optionally add request body schema if available
            if (!string.IsNullOrEmpty(request.Body))
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = {
                        { "application/json", new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    // Properly instantiate OpenApiString
                                    Example = new Microsoft.OpenApi.Any.OpenApiString(request.Body)
                                }
                            }
                        }
                    }
                };
            }

            openApiDocument.Paths[request.Url].Operations.Add(Enum.Parse<OperationType>(request.MethodType, true), operation);
        }

        return SerializeOpenApiDocument(openApiDocument);
    }

    private string SerializeOpenApiDocument(OpenApiDocument document)
    {
        using var stringWriter = new StringWriter();
        var jsonWriter = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(stringWriter);
        document.SerializeAsV2(jsonWriter);
        return stringWriter.ToString();
    }
}
