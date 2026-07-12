// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Callbacks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the serialization callback contract: <see cref="IOnSerializing" /> and <see cref="IOnSerialized" /> fire
/// around writing, <see cref="IOnDeserializing" /> and <see cref="IOnDeserialized" /> fire around reading, and the
/// callbacks observe the documented state — a write callback can influence the emitted text, the deserializing
/// callback runs after construction but before settable members are assigned, and the deserialized callback observes
/// the fully materialized instance.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that serializing a value invokes <see cref="IOnSerializing.OnSerializing" /> before
    /// <see cref="IOnSerialized.OnSerialized" />, in that order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCallbacksImplemented_ShouldFireSerializingThenSerialized()
    {
        var log = new List<string>();
        var model = new SerializeCallbackModel(log) { Value = 1 };

        _ = YamlSerializer.Serialize(model);

        CollectionAssert.AreEqual(new[] { "OnSerializing", "OnSerialized" }, log);
    }

    /// <summary>
    /// Verifies that a mutation performed in <see cref="IOnSerializing.OnSerializing" /> is reflected in the emitted
    /// text, since the callback runs before the members are written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOnSerializingMutatesState_ShouldReflectMutationInOutput()
    {
        var model = new MutatingSerializeModel { Value = 1 };

        string text = YamlSerializer.Serialize(model);

        // OnSerializing sets Value to 42 before the mapping is written.
        Assert.AreEqual("Value: 42\n", text);
    }

    /// <summary>
    /// Verifies that deserializing a value invokes <see cref="IOnDeserializing.OnDeserializing" /> before
    /// <see cref="IOnDeserialized.OnDeserialized" />, in that order.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCallbacksImplemented_ShouldFireDeserializingThenDeserialized()
    {
        DeserializeCallbackModel model = YamlSerializer.Deserialize<DeserializeCallbackModel>("Value: 1\n")!;

        CollectionAssert.AreEqual(new[] { "OnDeserializing", "OnDeserialized" }, model.Log);
    }

    /// <summary>
    /// Verifies that <see cref="IOnDeserializing.OnDeserializing" /> runs before settable members are assigned, so it
    /// observes the member at its constructed default rather than the value read from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOnDeserializing_ShouldRunBeforeSettableMembersAssigned()
    {
        DeserializingObservesDefaultModel model = YamlSerializer.Deserialize<DeserializingObservesDefaultModel>("Value: 7\n")!;

        Assert.AreEqual(0, model.ValueAtDeserializing);
        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that <see cref="IOnDeserialized.OnDeserialized" /> runs after every settable member is assigned, so it
    /// observes the value read from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOnDeserialized_ShouldRunAfterSettableMembersAssigned()
    {
        DeserializedObservesValueModel model = YamlSerializer.Deserialize<DeserializedObservesValueModel>("Value: 7\n")!;

        Assert.AreEqual(7, model.ValueAtDeserialized);
        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that a value implementing all four callbacks fires no read callbacks on serialize and no write
    /// callbacks on deserialize, so the two lifecycles stay separate.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAllCallbacks_ShouldFireOnlyRelevantLifecycle()
    {
        var serializeLog = new List<string>();
        var model = new AllCallbacksModel(serializeLog) { Value = 1 };

        string text = YamlSerializer.Serialize(model);
        CollectionAssert.AreEqual(new[] { "OnSerializing", "OnSerialized" }, serializeLog);

        AllCallbacksModel roundTripped = YamlSerializer.Deserialize<AllCallbacksModel>(text)!;
        CollectionAssert.AreEqual(new[] { "OnDeserializing", "OnDeserialized" }, roundTripped.Log);
    }

    /// <summary>
    /// A model that records its serialize callbacks into an externally supplied log.
    /// </summary>
    private sealed class SerializeCallbackModel
        : IOnSerializing, IOnSerialized
    {
        /// <summary>The log that records the callback order.</summary>
        private readonly List<string> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializeCallbackModel" /> class.
        /// </summary>
        /// <param name="log">The log that records the callback order.</param>
        public SerializeCallbackModel(List<string> log)
        {
            _log = log;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnSerializing() => _log.Add("OnSerializing");

        /// <inheritdoc />
        public void OnSerialized() => _log.Add("OnSerialized");
    }

    /// <summary>
    /// A model whose serializing callback mutates a member before it is written.
    /// </summary>
    private sealed class MutatingSerializeModel
        : IOnSerializing
    {
        /// <summary>
        /// Gets or sets the value, overwritten by <see cref="OnSerializing" /> before the member is written.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnSerializing() => Value = 42;
    }

    /// <summary>
    /// A model that records its deserialize callbacks into an internal log surfaced for assertion.
    /// </summary>
    private sealed class DeserializeCallbackModel
        : IOnDeserializing, IOnDeserialized
    {
        /// <summary>
        /// Gets the log that records the callback order.
        /// </summary>
        /// <value>The callback log.</value>
        public List<string> Log { get; } = new();

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnDeserializing() => Log.Add("OnDeserializing");

        /// <inheritdoc />
        public void OnDeserialized() => Log.Add("OnDeserialized");
    }

    /// <summary>
    /// A model whose deserializing callback captures the member value before any settable member is assigned.
    /// </summary>
    private sealed class DeserializingObservesDefaultModel
        : IOnDeserializing
    {
        /// <summary>
        /// Gets or sets the value read from the input.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <summary>
        /// Gets the value observed when <see cref="OnDeserializing" /> ran.
        /// </summary>
        /// <value>The observed value.</value>
        public int ValueAtDeserializing { get; private set; }

        /// <inheritdoc />
        public void OnDeserializing() => ValueAtDeserializing = Value;
    }

    /// <summary>
    /// A model whose deserialized callback captures the member value after every settable member is assigned.
    /// </summary>
    private sealed class DeserializedObservesValueModel
        : IOnDeserialized
    {
        /// <summary>
        /// Gets or sets the value read from the input.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <summary>
        /// Gets the value observed when <see cref="OnDeserialized" /> ran.
        /// </summary>
        /// <value>The observed value.</value>
        public int ValueAtDeserialized { get; private set; }

        /// <inheritdoc />
        public void OnDeserialized() => ValueAtDeserialized = Value;
    }

    /// <summary>
    /// A model implementing all four callbacks, used to confirm the read and write lifecycles stay separate.
    /// </summary>
    private sealed class AllCallbacksModel
        : IOnSerializing, IOnSerialized, IOnDeserializing, IOnDeserialized
    {
        /// <summary>The log that records the serialize callbacks; the deserialize log is exposed through <see cref="Log" />.</summary>
        private readonly List<string> _serializeLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllCallbacksModel" /> class for serialization.
        /// </summary>
        /// <param name="serializeLog">The log that records the serialize callbacks.</param>
        public AllCallbacksModel(List<string> serializeLog)
        {
            _serializeLog = serializeLog;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllCallbacksModel" /> class for deserialization.
        /// </summary>
        public AllCallbacksModel()
        {
            _serializeLog = new List<string>();
        }

        /// <summary>
        /// Gets the log that records the deserialize callbacks.
        /// </summary>
        /// <value>The deserialize callback log.</value>
        public List<string> Log { get; } = new();

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnSerializing() => _serializeLog.Add("OnSerializing");

        /// <inheritdoc />
        public void OnSerialized() => _serializeLog.Add("OnSerialized");

        /// <inheritdoc />
        public void OnDeserializing() => Log.Add("OnDeserializing");

        /// <inheritdoc />
        public void OnDeserialized() => Log.Add("OnDeserialized");
    }
}
