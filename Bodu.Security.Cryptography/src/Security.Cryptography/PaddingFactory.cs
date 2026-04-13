namespace Bodu.Security.Cryptography
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates <see cref="IPaddingStrategy" /> instances for the standard <see cref="PaddingMode" /> values.
    /// </summary>
    public static class PaddingFactory
    {
        /// <summary>
        /// Creates a new <see cref="IPaddingStrategy" /> for the specified padding mode.
        /// </summary>
        /// <param name="mode">The padding scheme to apply. Supported values are <see cref="PaddingMode.PKCS7" />,
        /// <see cref="PaddingMode.Zeros" />, and <see cref="PaddingMode.None" />.</param>
        /// <returns>An <see cref="IPaddingStrategy" /> that implements the requested <paramref name="mode" />.</returns>
        /// <exception cref="CryptographicException">Thrown if <paramref name="mode" /> is not a supported padding scheme.</exception>
        public static IPaddingStrategy Create(PaddingMode mode) => mode switch
        {
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroPadding(),
            PaddingMode.None => new NoPadding(),
            _ => throw new CryptographicException($"Unsupported this.padding this.mode: {mode}")
        };
    }
}