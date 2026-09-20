namespace Kho.Domain.Common;

public enum LoaiLoi { DuLieuKhongHopLe, KhongTimThay, XungDot, NghiepVu }

// Loi la GIA TRI (khong phai exception): mo ta "chuyen gi da sai" theo ngon ngu nghiep vu, khong biet gi ve HTTP.
public sealed record Loi(string Ma, string MoTa, LoaiLoi Loai)
{
    public static Loi DuLieuKhongHopLe(string ma, string moTa) => new(ma, moTa, LoaiLoi.DuLieuKhongHopLe);
    public static Loi KhongTimThay(string ma, string moTa) => new(ma, moTa, LoaiLoi.KhongTimThay);
    public static Loi XungDot(string ma, string moTa) => new(ma, moTa, LoaiLoi.XungDot);
    public static Loi NghiepVu(string ma, string moTa) => new(ma, moTa, LoaiLoi.NghiepVu);
}

// static abstract (C# 11): cho phep code GENERIC (pipeline behavior) tao ket qua loi ma khong can reflection
public interface IKetQua<TSelf> where TSelf : IKetQua<TSelf>
{
    static abstract TSelf TuLoi(Loi loi);
    bool ThanhCong { get; }
    Loi? Loi { get; }
}

public class Result : IKetQua<Result>
{
    public bool ThanhCong { get; }
    public Loi? Loi { get; }

    protected Result(bool thanhCong, Loi? loi) => (ThanhCong, Loi) = (thanhCong, loi);

    public static Result Ok() => new(true, null);
    public static Result Fail(Loi loi) => new(false, loi);
    public static Result<T> Ok<T>(T giaTri) => Result<T>.Ok(giaTri);
    public static Result TuLoi(Loi loi) => Fail(loi);
}

public sealed class Result<T> : Result, IKetQua<Result<T>>
{
    private readonly T? _giaTri;

    private Result(T? giaTri, bool thanhCong, Loi? loi) : base(thanhCong, loi) => _giaTri = giaTri;

    // Truy cap GiaTri khi that bai la BUG cua nguoi goi -> nem exception (khong phai loi nghiep vu)
    public T GiaTri => ThanhCong ? _giaTri! : throw new InvalidOperationException("Ket qua that bai khong co gia tri: " + Loi);

    public static Result<T> Ok(T giaTri) => new(giaTri, true, null);
    public static new Result<T> Fail(Loi loi) => new(default, false, loi);
    public static new Result<T> TuLoi(Loi loi) => Fail(loi);

    public static implicit operator Result<T>(T giaTri) => Ok(giaTri);

    // Ghep chuoi: chi chay tiep khi thanh cong (railway-oriented)
    public Result<TOut> Map<TOut>(Func<T, TOut> f) => ThanhCong ? Result<TOut>.Ok(f(GiaTri)) : Result<TOut>.Fail(Loi!);
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> f) => ThanhCong ? f(GiaTri) : Result<TOut>.Fail(Loi!);
}
