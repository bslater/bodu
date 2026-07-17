// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SharedSourceAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

// Format aliases consumed by the shared serializer source in Bodu.Text.Serialization/shared/, which is
// compiled into this assembly (see the csproj Compile link). Each Bodu text-format package defines the same
// alias names against its own reader/writer/options/converter/exception types, so the shared files read as
// ordinary C# while binding to this format's ref-struct-bound surface. YAML consumes only the metadata trio
// and the structural converter factories — its scalar converters are format-local to preserve YAML's
// implicit-typing coercions — so only the aliases that subset references are declared here.
global using FormatConverter = Bodu.Text.Yaml.Serialization.YamlConverter;
global using FormatConverterFactory = Bodu.Text.Yaml.Serialization.YamlConverterFactory;
global using FormatOptions = Bodu.Text.Yaml.YamlSerializerOptions;
global using FormatReader = Bodu.Text.Yaml.Reader.Utf8YamlReader;
global using FormatResourceStrings = Bodu.YamlResourceStrings;
global using FormatSerializationException = Bodu.Text.Yaml.YamlSerializationException;
global using FormatWriter = Bodu.Text.Yaml.Writer.Utf8YamlWriter;
