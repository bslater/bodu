using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class HashAlgorithmExtensions
    {
        public static void AppendData(this HashAlgorithm algorithm, ReadOnlySpan<byte> data)
        {
            ThrowHelper.ThrowIfNull(algorithm);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            data.CopyTo(buffer);
            algorithm.TransformBlock(buffer, 0, data.Length, null, 0);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}