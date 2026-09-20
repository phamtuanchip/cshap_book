using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace KienTruc.Tests;

// "Kiem thu kien truc": bien quy tac phu thuoc thanh TEST TU DONG. Ai lo them tham chieu sai se lam CI do.
public class ArchitectureTests
{
    private static readonly string GocSolution = TimGoc();

    private static string TimGoc()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KienTruc.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Khong tim thay KienTruc.slnx");
    }

    private static IReadOnlyList<string> ThamChieuDuAn(string tenDuAn)
        => XDocument.Load(Path.Combine(GocSolution, tenDuAn, tenDuAn + ".csproj"))
            .Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension((string)e.Attribute("Include")!))
            .ToList();

    private static IReadOnlyList<string> GoiNuGet(string tenDuAn)
        => XDocument.Load(Path.Combine(GocSolution, tenDuAn, tenDuAn + ".csproj"))
            .Descendants("PackageReference").Select(e => (string)e.Attribute("Include")!).ToList();

    [Fact]
    public void Domain_KhongPhuThuocBatKyThuGiGi()
    {
        Assert.Empty(ThamChieuDuAn("KienTruc.Domain"));
        Assert.Empty(GoiNuGet("KienTruc.Domain"));
    }

    [Fact]
    public void Application_ChiPhuThuocDomain()
        => Assert.Equal(["KienTruc.Domain"], ThamChieuDuAn("KienTruc.Application"));

    [Fact]
    public void Infrastructure_PhuThuocApplication_KhongPhuThuocWeb()
    {
        var refs = ThamChieuDuAn("KienTruc.Infrastructure");
        Assert.Contains("KienTruc.Application", refs);
        Assert.DoesNotContain("KienTruc.Web", refs);
    }

    [Fact]
    public void Domain_KhongThamChieuAssemblyHaTang()
    {
        var cam = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "System.Data", "Newtonsoft" };
        var duocGoi = typeof(KienTruc.Domain.SanPham).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain(duocGoi, ten => cam.Any(c => ten.StartsWith(c)));
    }

    [Fact]
    public void MoiInterfaceRepositoryNamTrongApplication()
    {
        var repo = typeof(KienTruc.Application.ISanPhamRepository).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository"));
        Assert.NotEmpty(repo);
        Assert.All(repo, t => Assert.StartsWith("KienTruc.Application", t.Namespace));
    }

    [Fact]
    public void LopCaiDatRepository_NamTrongInfrastructure()
    {
        var caiDat = typeof(KienTruc.Infrastructure.InMemorySanPhamRepository).Assembly.GetTypes()
            .Where(t => t.IsClass && typeof(KienTruc.Application.ISanPhamRepository).IsAssignableFrom(t));
        Assert.All(caiDat, t => Assert.StartsWith("KienTruc.Infrastructure", t.Namespace));
    }
}
