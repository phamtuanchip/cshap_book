using ApiKhachHang.Models;

namespace ApiKhachHang.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(string? search, CancellationToken ct);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct);
    Task<Customer> CreateAsync(Customer customer, CancellationToken ct);
    Task<bool> UpdateAsync(int id, Customer customer, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}

// Thay the doan "Get customers from the database" bang dich vu that (trong bo nho).
// Chuong 12 se thay bang EF Core ma controller khong phai sua gi.
public class InMemoryCustomerService : ICustomerService
{
    private readonly Lock _khoa = new();
    private readonly List<Customer> _ds =
    [
        new() { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
        new() { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" },
        new() { Id = 3, Name = "Bob Johnson", Email = "bob.johnson@example.com" },
    ];
    private int _idKeTiep = 4;

    public Task<IReadOnlyList<Customer>> GetAllAsync(string? search, CancellationToken ct)
    {
        lock (_khoa)
        {
            IEnumerable<Customer> q = _ds;
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<Customer>>(q.Select(Sao).ToList());
        }
    }

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
    {
        lock (_khoa) return Task.FromResult(_ds.FirstOrDefault(c => c.Id == id) is { } c ? Sao(c) : null);
    }

    public Task<Customer> CreateAsync(Customer customer, CancellationToken ct)
    {
        lock (_khoa)
        {
            var moi = new Customer { Id = _idKeTiep++, Name = customer.Name, Email = customer.Email };
            _ds.Add(moi);
            return Task.FromResult(Sao(moi));
        }
    }

    public Task<bool> UpdateAsync(int id, Customer customer, CancellationToken ct)
    {
        lock (_khoa)
        {
            var c = _ds.FirstOrDefault(x => x.Id == id);
            if (c is null) return Task.FromResult(false);
            c.Name = customer.Name;
            c.Email = customer.Email;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (_khoa) return Task.FromResult(_ds.RemoveAll(c => c.Id == id) > 0);
    }

    private static Customer Sao(Customer c) => new() { Id = c.Id, Name = c.Name, Email = c.Email };
}

public interface IUserRepository
{
    User? GetById(int id);
    void Add(User user);
    void SaveChanges();
}

public class InMemoryUserRepository : IUserRepository
{
    private readonly Lock _khoa = new();
    private readonly List<User> _ds = [new() { Id = 1, UserName = "admin" }];
    private int _idKeTiep = 2;

    public User? GetById(int id) { lock (_khoa) return _ds.FirstOrDefault(u => u.Id == id); }

    public void Add(User user)
    {
        lock (_khoa)
        {
            user.Id = _idKeTiep++;
            _ds.Add(user);
        }
    }

    public void SaveChanges() { /* Trong bo nho: khong can. Voi EF Core, day la _db.SaveChanges(). */ }
}
