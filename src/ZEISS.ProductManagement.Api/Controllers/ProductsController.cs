using Microsoft.AspNetCore.Mvc;
using ZEISS.ProductManagement.Application.Products;

namespace ZEISS.ProductManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List products (not implemented)")]
    [ProducesResponseType<ProductResponse[]>(StatusCodes.Status200OK)]
    public IActionResult GetAll(CancellationToken cancellationToken) => NotImplemented();

    [HttpPost]
    [EndpointSummary("Create a product with initial stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken) =>
        NotImplemented();

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a product by ID (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public IActionResult GetById(int id, CancellationToken cancellationToken) => NotImplemented();

    [HttpPut("{id:int}")]
    [EndpointSummary("Update name, description and price; never stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken) =>
        NotImplemented();

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a product (not implemented)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Delete(int id, CancellationToken cancellationToken) => NotImplemented();

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [EndpointSummary("Withdraw stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public IActionResult DecrementStock(int id, int quantity, CancellationToken cancellationToken) =>
        NotImplemented();

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [EndpointSummary("Add stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public IActionResult AddStock(int id, int quantity, CancellationToken cancellationToken) =>
        NotImplemented();

    [HttpGet("search")]
    [EndpointSummary("Search products by partial name (not implemented)")]
    [ProducesResponseType<ProductResponse[]>(StatusCodes.Status200OK)]
    public IActionResult Search([FromQuery] string? name, CancellationToken cancellationToken) =>
        NotImplemented();

    [HttpGet("stock-level")]
    [EndpointSummary("Filter products by stock range (not implemented)")]
    [ProducesResponseType<ProductResponse[]>(StatusCodes.Status200OK)]
    public IActionResult GetByStockLevel([FromQuery] int? min, [FromQuery] int? max, CancellationToken cancellationToken) =>
        NotImplemented();

    private ObjectResult NotImplemented() =>
        Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Not implemented",
            detail: "This endpoint is reserved. Product use cases and persistence are not implemented yet.");
}
