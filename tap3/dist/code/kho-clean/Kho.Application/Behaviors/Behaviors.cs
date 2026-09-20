using System.Diagnostics;
using FluentValidation;
using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Kho.Application.Behaviors;

// 1) LOGGING: bao quanh MOI request (ngoai cung)
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> where TResponse : IKetQua<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        string ten = typeof(TRequest).Name;
        var dong = Stopwatch.StartNew();
        var kq = await next();
        if (kq.ThanhCong) log.LogInformation("{Request} thanh cong sau {Ms} ms", ten, dong.ElapsedMilliseconds);
        else log.LogWarning("{Request} that bai ({Ma}) sau {Ms} ms", ten, kq.Loi!.Ma, dong.ElapsedMilliseconds);
        return kq;
    }
}

// 2) VALIDATION: kiem tra dau vao TRUOC khi vao handler; loi -> tra Result that bai (khong nem exception)
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> where TResponse : IKetQua<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var ctx = new ValidationContext<TRequest>(request);
        var loi = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(ctx, ct))))
            .SelectMany(r => r.Errors).Where(e => e is not null).ToList();

        if (loi.Count == 0) return await next();

        // Gop loi thanh mot Loi duy nhat: "truong: thong bao; truong: thong bao"
        string moTa = string.Join("; ", loi.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
        return TResponse.TuLoi(Loi.DuLieuKhongHopLe("Validation", moTa));
    }
}

// 3) UNIT OF WORK: sau khi LENH thanh cong thi luu MOT lan (cung mot giao dich). Truy van khong di qua behavior nay.
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork uow) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse> where TResponse : IKetQua<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var kq = await next();
        if (!kq.ThanhCong) return kq;                       // handler that bai: khong luu gi

        try
        {
            await uow.LuuAsync(ct);
        }
        catch (ConcurrencyException)
        {
            return TResponse.TuLoi(Loi.XungDot("Kho.XungDotDongThoi", "Du lieu vua bi nguoi khac thay doi, vui long thu lai"));
        }
        catch (TrungLapException e)
        {
            return TResponse.TuLoi(Loi.XungDot("Kho.TrungLap", e.Message));
        }
        return kq;
    }
}

// Loi "ha tang" duoc dich sang ngon ngu Application (Infrastructure nem, Application hieu)
public class ConcurrencyException(string message = "Xung dot dong thoi") : Exception(message);
public class TrungLapException(string message) : Exception(message);
