using System.Net;
using System.Net.Sockets;
using System.Text;

// Muc tieu: NHIN THAY mot cuoc "tro chuyen" HTTP that su o dang van ban.
// Ta tu dung mot may chu HTTP thu cong (TcpListener) tren cong ngau nhien, roi goi no bang HttpClient.

var listener = new TcpListener(IPAddress.Loopback, 0);   // cong 0 = he dieu hanh chon cong con trong
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;

var may_chu = Task.Run(async () =>
{
    using TcpClient khach = await listener.AcceptTcpClientAsync();
    using NetworkStream stream = khach.GetStream();

    var dem = new byte[4096];
    int n = await stream.ReadAsync(dem);
    string request = Encoding.UTF8.GetString(dem, 0, n);

    Console.WriteLine("-------- REQUEST MAY CHU NHAN DUOC --------");
    Console.WriteLine(request.TrimEnd());

    string body = """{"loiChao":"Xin chao tu may chu tu viet","thanhCong":true}""";
    string response =
        "HTTP/1.1 200 OK\r\n" +
        "Content-Type: application/json; charset=utf-8\r\n" +
        $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
        "X-May-Chu: demo\r\n" +
        "Connection: close\r\n" +
        "\r\n" +
        body;
    await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
});

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("Accept", "application/json");
http.DefaultRequestHeaders.Add("X-Yeu-Cau", "thu-nghiem");

HttpResponseMessage kq = await http.GetAsync($"http://127.0.0.1:{port}/api/chao?ten=An&lang=vi");
await may_chu;
listener.Stop();

Console.WriteLine("\n-------- RESPONSE CLIENT NHAN DUOC --------");
Console.WriteLine($"Status: {(int)kq.StatusCode} {kq.StatusCode}");
foreach (var (ten, giaTri) in kq.Headers.Concat(kq.Content.Headers))
    Console.WriteLine($"{ten}: {string.Join(", ", giaTri)}");
Console.WriteLine();
Console.WriteLine(await kq.Content.ReadAsStringAsync());

// Phan tich URL
Console.WriteLine("\n-------- PHAN TICH URL --------");
var url = new Uri("https://api.cuahang.vn:8443/api/san-pham/42?nhom=phu-kien&sapXep=gia#chi-tiet");
Console.WriteLine($"Scheme={url.Scheme}, Host={url.Host}, Port={url.Port}");
Console.WriteLine($"Path={url.AbsolutePath}, Query={url.Query}, Fragment={url.Fragment}");

// Cac ma trang thai thuong gap
Console.WriteLine("\n-------- MA TRANG THAI --------");
foreach (var ma in new[] { HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent,
                           HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden,
                           HttpStatusCode.NotFound, HttpStatusCode.Conflict, HttpStatusCode.InternalServerError })
    Console.WriteLine($"{(int)ma} {ma}");
