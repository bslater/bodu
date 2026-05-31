// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginException.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Base exception for failures raised by <see cref="ExternalPluginLoader" /> while loading or validating an external
/// notable- date plugin assembly.
/// </summary>
/// <remarks>
/// <para>
/// Derived exception types surface the specific failure mode: <see cref="PluginNotTrustedException" /> when the trust
/// policy rejects the candidate, <see cref="PluginMissingAttributeException" /> when the assembly does not carry a
/// valid <see cref="NotableDatePluginAttribute" />, and <see cref="PluginActivationException" /> when the declared
/// plugin type fails to instantiate.
/// </para>
/// <para>
/// Catch the base type when a host wants to treat all plugin-load failures uniformly; catch one of the derived types
/// when the host needs to surface different diagnostics (for example, prompting the operator to update the trust policy
/// in the <see cref="PluginNotTrustedException" /> case).
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var loader = new ExternalPluginLoader(trustPolicy);
///
/// try
/// {
///     INotableDatePlugin plugin = loader.Load(assemblyPath);
///     // ... register the plugin with the service ...
/// }
/// catch (PluginNotTrustedException ex)
/// {
///     // Trust policy rejected the assembly — log the reason for the operator.
///     Console.Error.WriteLine(ex.Message);
/// }
/// catch (NotableDatePluginException ex)
/// {
///     // Catch-all for missing attribute, activation failure, etc.
///     Console.Error.WriteLine($"Plugin load failed: {ex.Message}");
/// }
///]]>
/// </example>
public class NotableDatePluginException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NotableDatePluginException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public NotableDatePluginException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class.
    /// </summary>
    public NotableDatePluginException()
        : base()
    { }
}
