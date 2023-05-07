using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CustomerService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Customer>> GetCustomers()
        {
            // Get customers from the database
            var customers = new List<Customer>
            {
                new Customer { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
                new Customer { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" },
                new Customer { Id = 3, Name = "Bob Johnson", Email = "bob.johnson@example.com" }
            };

            return Ok(customers);
        }

        [HttpGet("{id}")]
        public ActionResult<Customer> GetCustomerById(int id)
        {
            // Get customer by id from the database
            var customer = new Customer { Id = id, Name = "John Doe", Email = "john.doe@example.com" };

            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }

        [HttpPost]
        public ActionResult<Customer> CreateCustomer(Customer customer)
        {
            // Save customer to the database
            // ...

            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public ActionResult<Customer> UpdateCustomer(int id, Customer customer)
        {
            // Update customer in the database
            // ...

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteCustomer(int id)
        {
            // Delete customer from the database
            // ...

            return NoContent();
        }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
