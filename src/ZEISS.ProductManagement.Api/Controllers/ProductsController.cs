using Microsoft.AspNetCore.Mvc;
using ZEISS.ProductManagement.Application.Products;

namespace ZEISS.ProductManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List products ordered by ID, including available stock")]
    [ProducesResponseType<ProductResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        [FromServices] IProductQueries queries, CancellationToken cancellationToken)
    {
        return Ok(await queries.ListAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Create a product with initial stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a product by ID, including available stock")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id, [FromServices] IProductQueries queries, CancellationToken cancellationToken)
    {
        var product = await queries.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Product not found",
                detail: "The requested product does not exist.");
        }

        return Ok(product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Update name, description and price; never stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Delete a product (not implemented)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Delete(int id, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Withdraw stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public IActionResult DecrementStock(int id, int quantity, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Add stock (not implemented)")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public IActionResult AddStock(int id, int quantity, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    [HttpGet("search")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Search products by partial name (not implemented)")]
    [ProducesResponseType<ProductResponse[]>(StatusCodes.Status200OK)]
    public IActionResult Search([FromQuery] string? name, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    [HttpGet("stock-level")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    [EndpointSummary("Filter products by stock range (not implemented)")]
    [ProducesResponseType<ProductResponse[]>(StatusCodes.Status200OK)]
    public IActionResult GetByStockLevel([FromQuery] int? min, [FromQuery] int? max, CancellationToken cancellationToken)
    {
        return NotImplemented();
    }

    private ObjectResult NotImplemented()
    {
        return Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Not implemented",
            detail: "This endpoint is reserved. Product use cases and persistence are not implemented yet.");
    }
}
