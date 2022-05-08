namespace Eventuous.Connector.Base.Serialization;

public class RawDataDeserializer : IEventSerializer {
    public DeserializationResult DeserializeEvent(ReadOnlySpan<byte> data, string eventType, string contentType)
        => new SuccessfullyDeserialized(data.ToArray());

    public SerializationResult SerializeEvent(object evt) => DefaultEventSerializer.Instance.SerializeEvent(evt);
}
