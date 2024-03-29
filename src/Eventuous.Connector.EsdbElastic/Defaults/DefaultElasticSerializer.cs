// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Elasticsearch.Net;
using Eventuous.ElasticSearch.Store;

namespace Eventuous.Connector.EsdbElastic.Defaults;

public class DefaultElasticSerializer : IElasticsearchSerializer {
    readonly IElasticsearchSerializer _builtIn;

    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public DefaultElasticSerializer(IElasticsearchSerializer builtIn) => _builtIn = builtIn;

    public object Deserialize(Type type, Stream stream) => _builtIn.Deserialize(type, stream);

    public T Deserialize<T>(Stream stream) => _builtIn.Deserialize<T>(stream);

    public Task<object> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
        => _builtIn.DeserializeAsync(type, stream, cancellationToken);

    public Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        => _builtIn.DeserializeAsync<T>(stream, cancellationToken);

    public void Serialize<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None) {
        if (data is not PersistedEvent persistedEvent) {
            _builtIn.Serialize(data, stream, formatting);
            return;
        }

        var payload = persistedEvent.Message as byte[];

        using var doc    = JsonSerializer.SerializeToDocument(persistedEvent with { Message = null }, Options);
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();

        foreach (var jsonElement in doc.RootElement.EnumerateObject()) {
            if (jsonElement.NameEquals("message")) {
                writer.WritePropertyName("message");
                var parsed = JsonDocument.Parse(payload);
                parsed.WriteTo(writer);
            }
            else if (jsonElement.NameEquals("created")) {
                writer.WriteString("@timestamp", persistedEvent.Created);
            }
            else {
                jsonElement.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();
    }

    public Task SerializeAsync<T>(
        T                       data,
        Stream                  stream,
        SerializationFormatting formatting        = SerializationFormatting.None,
        CancellationToken       cancellationToken = default
    ) {
        Serialize(data, stream, formatting);
        return Task.CompletedTask;
    }
}
