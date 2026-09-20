using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ApiSanPham;
using ApiSanPham.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Tuy chinh dinh dang loi validation (mac dinh da la ValidationProblemDetails, day chi them "traceId")
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var problem = new ValidationProblemDetails(ctx.ModelState)
            {
                Title = "Du lieu gui len khong hop le",
                Status = StatusCodes.Status400BadRequest,
                Instance = ctx.HttpContext.Request.Path,
            };
            problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        };
    });
builder.Services.AddSingleton<KhoSanPham>();

var app = builder.Build();
app.MapControllers();

// Minimal API khong tu validate DataAnnotations -> dung endpoint filter chung
app.MapPost("/minimal/san-pham", (TaoSanPhamRequest req) => Results.Ok(new { NhanDuoc = req.Ten, Gia = req.Gia }))
   .AddEndpointFilter(async (ctx, next) =>
   {
       foreach (var arg in ctx.Arguments)
       {
           if (arg is null) continue;
           var ketQua = new List<ValidationResult>();
           if (!Validator.TryValidateObject(arg, new ValidationContext(arg), ketQua, validateAllProperties: true))
           {
               var loi = ketQua
                   .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [""]).Select(m => (m, r.ErrorMessage ?? "Khong hop le")))
                   .GroupBy(x => x.m)
                   .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToArray());
               return Results.ValidationProblem(loi);
           }
       }
       return await next(ctx);
   });

app.Run();
