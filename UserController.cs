[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpPost]
    public IActionResult Create(User user)
    {
        _userRepository.Add(user);
        _userRepository.SaveChanges();

        return Ok();
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var user = _userRepository.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
