using System.Xml.Linq;
using Xunit;

namespace Kho.Tests;

public class ArchitectureTests
{
    private static readonly string Goc = TimGoc();

    private static string TimGoc()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kho.slnx"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Khong tim thay Kho.slnx");
    }

    private static List<string> ThamChieu(string duAn, string node)
        => XDocument.Load(Path.Combine(Goc, duAn, duAn + ".csproj")).Descendants(node)
            .Select(e => Path.GetFileNameWithoutExtension((string)e.Attribute("Include")!)).ToList();

    [Fact]
    public void Domain_KhongPhuThuocGi()
    {
        Assert.Empty(ThamChieu("Kho.Domain", "ProjectReference"));
        Assert.Empty(ThamChieu("Kho.Domain", "PackageReference"));
    }

    [Fact]
    public void Application_ChiPhuThuocDomain_VaKhongCoEfCoreHayAspNetCore()
    {
        Assert.Equal(["Kho.Domain"], ThamChieu("Kho.Application", "ProjectReference"));
        Assert.DoesNotContain(ThamChieu("Kho.Application", "PackageReference"), p => p.Contains("EntityFramework") || p.Contains("AspNetCore"));
    }

    [Fact]
    public void Infrastructure_PhuThuocApplication_KhongPhuThuocWeb()
    {
        var r = ThamChieu("Kho.Infrastructure", "ProjectReference");
        Assert.Contains("Kho.Application", r);
        Assert.DoesNotContain("Kho.Web", r);
    }

    [Fact]
    public void Aggregate_KhongCoSetterCongKhai()
    {
        var props = typeof(Kho.Domain.SanPhams.SanPham).GetProperties().Where(p => p.SetMethod is { IsPublic: true });
        Assert.Empty(props);                                // trang thai chi doi qua phuong thuc nghiep vu
    }

    [Fact]
    public void MoiHandlerLaSealed()
    {
        var handlers = typeof(Kho.Application.DependencyInjection).Assembly.GetTypes().Where(t => t.Name.EndsWith("Handler"));
        Assert.NotEmpty(handlers);
        Assert.All(handlers, t => Assert.True(t.IsSealed, $"{t.Name} nen la sealed"));
    }
}
