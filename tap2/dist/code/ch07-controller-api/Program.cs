using ApiKhachHang.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();                                      // bat controller (model binding, filters, ...)
builder.Services.AddSingleton<ICustomerService, InMemoryCustomerService>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

app.MapControllers();                                                   // gan cac [Route] cua controller vao pipeline

app.Run();
