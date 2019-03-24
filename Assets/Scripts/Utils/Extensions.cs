using System.IO;
using System.Text;
using System.Threading.Tasks;

public static class Extensions
{
    public static async Task<string> ReadString(this Stream stream)
    {
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    public static async Task WriteString(this Stream stream, string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);

        await stream.WriteAsync(bytes, 0, bytes.Length);
    }
}