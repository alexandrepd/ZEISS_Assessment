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
    [EndpointSummary("Create a product with initial stock and a generated ID")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        [FromServices] IProductCommands commands,
        CancellationToken cancellationToken)
    {
        var price = request.Price
            ?? throw new InvalidOperationException("The request must be validated before creating a product.");
        var stock = request.Stock
            ?? throw new InvalidOperationException("The request must be validated before creating a product.");

        var product = await commands.CreateAsync(
            request.Name,
            request.Description,
            price,
            stock,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
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
    [EndpointSummary("Update name, description and price; never stock")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id, [FromBody] UpdateProductRequest request,
        [FromServices] IProductCommands commands, CancellationToken cancellationToken)
    {
        var price = request.Price
            ?? throw new InvalidOperationException("The request must be validated before updating a product.");
        var product = await commands.UpdateAsync(
            id, request.Name, request.Description, price, cancellationToken);
        if (product is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Product not found",
                detail: "The requested product does not exist.");
        }

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a product by ID")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        int id, [FromServices] IProductCommands commands, CancellationToken cancellationToken)
    {
        var deleted = await commands.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Product not found",
                detail: "The requested product does not exist.");
        }

        return NoContent();
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
