// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigJsonValueKind.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Identifies the kind of a <see cref="ConfigJsonValue" /> parsed from a configuration document.
/// </summary>
internal enum ConfigJsonValueKind
{
    /// <summary>A JSON object with named members.</summary>
    Object = 0,

    /// <summary>A JSON array of values.</summary>
    Array = 1,

    /// <summary>A JSON string.</summary>
    String = 2,

    /// <summary>A JSON number.</summary>
    Number = 3,

    /// <summary>A JSON boolean (<c>true</c> or <c>false</c>).</summary>
    Boolean = 4,

    /// <summary>A JSON <c>null</c> literal.</summary>
    Null = 5,
}
