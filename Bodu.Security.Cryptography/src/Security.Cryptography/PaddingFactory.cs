using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public static class PaddingFactory
    {
        public static IPaddingStrategy Create(PaddingMode mode) => mode switch
        {
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroPadding(),
            PaddingMode.None => new NoPadding(),
            _ => throw new CryptographicException($"Unsupported this.padding this.mode: {mode}")
        };
    }
}