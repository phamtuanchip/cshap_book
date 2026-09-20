using ApiKhachHang.Models;
using ApiKhachHang.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiKhachHang.Controllers;

// [ApiController]: bat cac hanh vi danh cho API (tu dong 400 khi model sai, suy ra nguon binding, ProblemDetails...)
// [Route("api/[controller]")]: [controller] duoc thay bang ten lop bo hau to "Controller" -> /api/customers
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController(ICustomerService customers) : ControllerBase   // DI qua primary constructor
{
    // GET /api/customers?search=jo
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers([FromQuery] string? search, CancellationToken ct)
        => Ok(await customers.GetAllAsync(search, ct));

    // GET /api/customers/2
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetCustomerById(int id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    // POST /api/customers   (body JSON) -> 201 Created + Location
    [HttpPost]
    [ProducesResponseType<Customer>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer, CancellationToken ct)
    {
        var created = await customers.CreateAsync(customer, ct);
        return CreatedAtAction(nameof(GetCustomerById), new { id = created.Id }, created);
    }

    // PUT /api/customers/2 -> 204 hoac 404
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, Customer customer, CancellationToken ct)
        => await customers.UpdateAsync(id, customer, ct) ? NoContent() : NotFound();

    // DELETE /api/customers/2 -> 204 hoac 404
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
        => await customers.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
