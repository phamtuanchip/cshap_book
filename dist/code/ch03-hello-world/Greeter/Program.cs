// Nhan doi so dong lenh qua bien "args" (co san trong top-level statements).
string ten = args.Length > 0 ? args[0] : "hoc vien";

Console.WriteLine($"Xin chao, {ten}!");
Console.WriteLine($"Day la doi so dong lenh nhan duoc: {args.Length} doi so.");
