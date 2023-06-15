// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector.Base.Grpc;
using Eventuous.Producers.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Eventuous.Connector.EsdbSqlServer;

public class SqlServerProjector : GrpcProjectingProducer<SqlServerProjector, SqlServerProjectOptions> {
    readonly GetConnection               _getConnection;
    readonly ILogger<SqlServerProjector> _log;

    public SqlServerProjector(GetConnection getConnection, ILogger<SqlServerProjector> logger) : base(TracingOptions) {
        _getConnection = getConnection;
        _log = logger;

        On<Execute>((message, token) => ExecuteSql(message.Message, token));
    }

    static readonly ProducerTracingOptions TracingOptions = new() {
        MessagingSystem = "sqlserver",
        DestinationKind = "table",
        ProduceOperation = "project"
    };

    async Task ExecuteSql(Execute execute, CancellationToken cancellationToken) {
        _log.LogDebug("Executing SQL {Sql}", execute.Sql);
        await _getConnection.ExecuteNonQuery(execute.Sql, _ => { }, cancellationToken);
    }
}

public record SqlServerProjectOptions;
