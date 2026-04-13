namespace Bodu.Security.Cryptography
{
    using System.Security.Cryptography;

    /// <summary>
    /// Defines a factory that produces configured instances of a specific <see cref="HashAlgorithm" /> implementation.
    /// </summary>
    /// <typeparam name="T">The concrete type of <see cref="HashAlgorithm" /> this factory creates. Must inherit from <see cref="HashAlgorithm" />.</typeparam>
    /// <remarks>
    /// This interface is used to decouple the creation and configuration of hash algorithm instances from the logic that consumes them. It
    /// enables reusable, testable, and extensible one-shot hash operations via the <see cref="HashAlgorithmHelper" /> utility class. ///
    /// Implementations can return newly constructed or pooled algorithm instances, depending on lifecycle management requirements.
    /// </remarks>
    public interface IHashAlgorithmFactory<out T>
        where T : System.Security.Cryptography.HashAlgorithm
    {
        /// <summary>
        /// Creates and returns a new instance of the hash algorithm with any necessary configuration applied.
        /// </summary>
        /// <returns>A fully initialized <typeparamref name="T" /> instance, ready for data input.</returns>
        T Create();
    }
}