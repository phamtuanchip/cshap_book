using System.Text.Json;
using System.Text.Json.Serialization;
using QuanLyKho.Core.Abstractions;
using QuanLyKho.Core.Models;

namespace QuanLyKho.Core.Storage;

public class JsonKhoRepository(string duongDan) : IKhoRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<DuLieuKho> TaiAsync(CancellationToken ct = default)
    {
        if (!File.Exists(duongDan)) return new DuLieuKho();

        await using var fs = File.OpenRead(duongDan);
        return await JsonSerializer.DeserializeAsync<DuLieuKho>(fs, Options, ct) ?? new DuLieuKho();
    }

    public async Task LuuAsync(DuLieuKho duLieu, CancellationToken ct = default)
    {
        string? thuMuc = Path.GetDirectoryName(Path.GetFullPath(duongDan));
        if (thuMuc is not null) Directory.CreateDirectory(thuMuc);

        // Ghi ra file tam roi doi ten: neu sap giua chung, file cu van nguyen ven
        string tam = duongDan + ".tmp";
        await using (var fs = File.Create(tam))
        {
            await JsonSerializer.SerializeAsync(fs, duLieu, Options, ct);
        }
        File.Move(tam, duongDan, overwrite: true);
    }
}
