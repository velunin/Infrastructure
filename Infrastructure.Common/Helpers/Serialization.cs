using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Infrastructure.Common.Helpers
{
    public class Serialization
    {
        public static byte[] Serialize<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] array)
        {
            using (var stream = new MemoryStream(array))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}