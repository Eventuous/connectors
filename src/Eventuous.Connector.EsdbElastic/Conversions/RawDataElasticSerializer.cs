// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Elasticsearch.Net;

namespace Eventuous.Connector.EsdbElastic.Conversions;

public class RawDataElasticSerializer : IElasticsearchSerializer {
    readonly IElasticsearchSerializer _builtIn;

    public RawDataElasticSerializer(IElasticsearchSerializer builtIn) => _builtIn = builtIn;
    public object Deserialize(Type type, Stream stream) => _builtIn.Deserialize(type, stream);

    public T Deserialize<T>(Stream stream) => _builtIn.Deserialize<T>(stream);

    public Task<object> DeserializeAsync(
        Type type, Stream stream, CancellationToken cancellationToken = default
    )
        => _builtIn.DeserializeAsync(type, stream, cancellationToken);

    public Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        => _builtIn.DeserializeAsync<T>(stream, cancellationToken);

    public void Serialize<T>(
        T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None
    ) {
        if (data is not string dataString) {
            _builtIn.Serialize(data, stream, formatting);
            return;
        }

        using var writer = new StreamWriter(stream);
        writer.Write(dataString);
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
