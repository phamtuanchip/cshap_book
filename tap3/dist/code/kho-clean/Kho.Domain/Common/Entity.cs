namespace Kho.Domain.Common;

// Su kien mien: "dieu gi DA xay ra" trong nghiep vu, dat ten thi qua khu.
public interface IDomainEvent
{
    DateTimeOffset XayRaLuc { get; }
}

public abstract class Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _suKien = [];

    public TId Id { get; protected set; } = default!;
    public IReadOnlyList<IDomainEvent> SuKienMien => _suKien;

    protected void PhatSuKien(IDomainEvent e) => _suKien.Add(e);
    public void XoaSuKien() => _suKien.Clear();
}
