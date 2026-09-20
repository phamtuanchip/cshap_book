// Doi tuong bat bien (immutable): tao xong khong doi duoc.
public class DiemDat
{
    public double X { get; init; }   // init: chi gan duoc luc khoi tao
    public double Y { get; init; }

    // tao ban sao voi gia tri moi thay vi sua tai cho
    public DiemDat DichChuyen(double dx, double dy) => new() { X = X + dx, Y = Y + dy };

    public override string ToString() => $"({X}, {Y})";
}
