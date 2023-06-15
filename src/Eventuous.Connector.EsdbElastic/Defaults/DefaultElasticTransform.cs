// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector.EsdbElastic.Conversions;
using Eventuous.ElasticSearch.Producers;
using Eventuous.ElasticSearch.Store;
using Eventuous.Gateway;
using Eventuous.Subscriptions.Context;

namespace Eventuous.Connector.EsdbElastic.Defaults;

public class DefaultElasticTransform : IGatewayTransform<ElasticProduceOptions> {
    readonly string _indexName;

    static readonly ElasticProduceOptions Options = new() { ProduceMode = ProduceMode.Create };

    public DefaultElasticTransform(string indexName) => _indexName = indexName;

    public ValueTask<GatewayMessage<ElasticProduceOptions>[]> RouteAndTransform(
        IMessageConsumeContext context
    ) {
        var gatewayMessage = new GatewayMessage<ElasticProduceOptions>(
            new StreamName(_indexName),
            FromContext(context),
            null,
            Options
        );

        return new ValueTask<GatewayMessage<ElasticProduceOptions>[]>(new[] { gatewayMessage });
    }

    static PersistedEvent FromContext(IMessageConsumeContext ctx)
        => new(
            ctx.MessageId,
            ctx.MessageType,
            (long)ctx.StreamPosition,
            ctx.ContentType,
            ctx.Stream,
            ctx.GlobalPosition,
            ctx.Message,
            ElasticMetadata.FromMetadata(ctx.Metadata),
            ctx.Created
        );
}
