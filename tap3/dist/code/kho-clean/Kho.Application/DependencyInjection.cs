using FluentValidation;
using Kho.Application.Behaviors;
using Kho.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Kho.Application;

public static class DependencyInjection
{
    // Application tu dang ky nhung gi cua no: Web chi can goi services.AddApplication()
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Thu tu behavior: tu NGOAI vao TRONG  =>  logging > validation > unit of work > handler
        services.AddMediator(assembly, typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        // FluentValidation: dang ky moi validator (IValidator<T>) trong assembly
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
            foreach (var i in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
                services.AddScoped(i, type);

        return services;
    }
}
