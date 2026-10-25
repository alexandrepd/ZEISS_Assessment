using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ZEISS.ProductManagement.IntegrationTests;

public sealed class ProductsApiTests
{
    [Theory]
    [InlineData("DELETE", "/api/products/100005")]
    [InlineData("POST", "/api/products/100005/decrement-stock/4")]
    [InlineData("POST", "/api/products/100005/add-to-stock/2")]
    [InlineData("GET", "/api/products/search?name=lens")]
    [InlineData("GET", "/api/products/stock-level?min=0&max=10")]
    public async Task ReservedRoute_ReturnsNotImplementedProblem(string method, string path)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(501, problem.GetProperty("status").GetInt32());
        Assert.Equal("Not implemented", problem.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("POST", "/api/products", "{}", "Name")]
    [InlineData("POST", "/api/products", """{"name":"Lens","price":1}""", "Stock")]
    [InlineData("POST", "/api/products", """{"name":"Lens","price":1.001,"stock":0}""", "Price")]
    [InlineData("POST", "/api/products", """{"name":"Lens","price":1,"stock":-1}""", "Stock")]
    [InlineData("PUT", "/api/products/100005", """{"name":" ","price":1}""", "Name")]
    [InlineData("PUT", "/api/products/100005", """{"name":"Lens","price":0}""", "Price")]
    public async Task InvalidRequest_ReturnsValidationProblem(string method, string path, string json, string field)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Development_OpenApiDescribesNineOperationsAndProductContracts()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var paths = document.GetProperty("paths");
        Assert.Equal(9, paths.EnumerateObject().Sum(path => path.Value.EnumerateObject().Count()));
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var isImplementedGet = operation.Name == "get" &&
                    (path.Name == "/api/products" || path.Name == "/api/products/{id}");
                var isCreate = operation.Name == "post" && path.Name == "/api/products";
                var isUpdate = operation.Name == "put" && path.Name == "/api/products/{id}";
                var responses = operation.Value.GetProperty("responses");
                Assert.Equal(!isImplementedGet && !isCreate && !isUpdate, responses.TryGetProperty("501", out _));
                if (isImplementedGet || isUpdate)
                {
                    Assert.True(responses.TryGetProperty("200", out _));
                }
                if (isCreate)
                {
                    Assert.True(responses.TryGetProperty("201", out _));
                }
            }
        }

        Assert.True(paths.GetProperty("/api/products/{id}").GetProperty("get")
            .GetProperty("responses").TryGetProperty("404", out _));

        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("ProductResponse").GetProperty("properties").TryGetProperty("stock", out _));
        Assert.True(schemas.GetProperty("CreateProductRequest").GetProperty("properties").TryGetProperty("stock", out _));
        Assert.False(schemas.GetProperty("UpdateProductRequest").GetProperty("properties").TryGetProperty("stock", out _));

        var swagger = await client.GetStringAsync("/swagger");
        Assert.Contains("SwaggerUIBundle", swagger);
        Assert.Contains("/openapi/v1.json", swagger);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    public async Task Production_DoesNotExposeDocumentation(string path)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
