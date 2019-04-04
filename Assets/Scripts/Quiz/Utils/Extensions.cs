using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Quiz
{
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

        public static void SetStatus(this CanvasGroup canvasGroup, bool status)
        {
            canvasGroup.alpha = status ? 1 : 0;
            canvasGroup.interactable = status;
            canvasGroup.blocksRaycasts = status;
        }

        public static T PickRandom<T>(this IEnumerable<T> enumerable)
        {
            var array = enumerable as T[] ?? enumerable.ToArray();
            var index = Random.Range(0, array.Length);

            return array.ElementAt(index);
        }
    }
}