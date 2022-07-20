using Eventuous.Connector.Base.Grpc;
using Eventuous.Producers.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Eventuous.Connector.EsdbSqlServer;

public class SqlProjector : GrpcProjectingProducer<SqlProjector, SqlServerProjectOptions> {
    readonly GetConnection                    _getConnection;
    readonly ILogger<SqlServerProjectOptions> _log;

    public SqlProjector(GetConnection getConnection, ILogger<SqlServerProjectOptions> logger) : base(TracingOptions) {
        _getConnection = getConnection;
        _log           = logger;

        On<Execute>((message, token) => ExecuteSql(message.Message, token));
    }

    static readonly ProducerTracingOptions TracingOptions = new() {
        MessagingSystem  = "sqlserver",
        DestinationKind  = "table",
        ProduceOperation = "project"
    };

    async Task ExecuteSql(Execute execute, CancellationToken cancellationToken) {
        _log.LogDebug("Executing SQL {sql}", execute.Sql);
        await _getConnection.ExecuteNonQuery(execute.Sql, _ => { }, cancellationToken);
    }
}

public record SqlServerProjectOptions;
