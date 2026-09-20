using System.Collections.Concurrent;
using Kho.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Kho.Application.Messaging;

// ================= Hop dong =================
public interface IRequest<TResponse> where TResponse : IKetQua<TResponse>;

// Lenh: THAY DOI trang thai. Truy van: chi DOC, khong tac dung phu (CQRS - Chuong 5)
public interface ICommand<TResponse> : IRequest<TResponse> where TResponse : IKetQua<TResponse>;
public interface IQuery<TResponse> : IRequest<TResponse> where TResponse : IKetQua<TResponse>;

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : IKetQua<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

// "Ong dan" (pipeline): boc quanh handler, giong middleware cua ASP.NET Core (Tap 2, Chuong 3)
public interface IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : IKetQua<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}

public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) where TResponse : IKetQua<TResponse>;
}

// ================= Cai dat mediator toi gian (~40 dong) =================
internal sealed class Sender(IServiceProvider sp) : ISender
{
    // Cache "may goi" cho tung kieu request de khong phai reflection moi lan
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) where TResponse : IKetQua<TResponse>
    {
        var wrapper = (Boc<TResponse>)Wrappers.GetOrAdd(request.GetType(),
            t => Activator.CreateInstance(typeof(Boc<,>).MakeGenericType(t, typeof(TResponse)))!);
        return wrapper.Chay(request, sp, ct);
    }

    private abstract class Boc<TResponse> where TResponse : IKetQua<TResponse>
    {
        public abstract Task<TResponse> Chay(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class Boc<TRequest, TResponse> : Boc<TResponse>
        where TRequest : IRequest<TResponse> where TResponse : IKetQua<TResponse>
    {
        public override Task<TResponse> Chay(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct)
        {
            var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            RequestHandlerDelegate<TResponse> chuoi = () => handler.Handle((TRequest)request, ct);

            // Behavior dang ky SAU nam NGOAI CUNG: duyet nguoc de dang ky dau tien bao ngoai cung
            foreach (var b in sp.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
            {
                var ke = chuoi;
                chuoi = () => b.Handle((TRequest)request, ke, ct);
            }
            return chuoi();
        }
    }
}

public static class MediatorExtensions
{
    // Quet assembly: dang ky moi IRequestHandler<,> va (tuy chon) behavior
    public static IServiceCollection AddMediator(this IServiceCollection services, System.Reflection.Assembly assembly, params Type[] behaviorMoRong)
    {
        services.AddScoped<ISender, Sender>();

        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
            foreach (var i in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                services.AddScoped(i, type);

        foreach (var b in behaviorMoRong)                      // thu tu truyen vao = thu tu tu NGOAI vao TRONG
            services.AddScoped(typeof(IPipelineBehavior<,>), b);

        return services;
    }
}
