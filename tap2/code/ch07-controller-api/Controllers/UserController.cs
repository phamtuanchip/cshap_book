using ApiKhachHang.Models;
using ApiKhachHang.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiKhachHang.Controllers;

// Ban viet lai cua UserController goc: van DI qua constructor, nhung dung primary constructor
// va tra 201 Created (chu khong phai 200 OK) khi tao moi.
[ApiController]
[Route("[controller]")]     // -> /user
public class UserController(IUserRepository userRepository) : ControllerBase
{
    [HttpPost]
    public IActionResult Create(User user)
    {
        userRepository.Add(user);
        userRepository.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var user = userRepository.GetById(id);
        return user is null ? NotFound() : Ok(user);
    }
}
