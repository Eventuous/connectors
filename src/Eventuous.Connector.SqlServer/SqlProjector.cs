using Eventuous.Producers;
using Microsoft.Extensions.Logging;
using static Eventuous.Connector.SqlServer.ProjectionResult;

namespace Eventuous.Connector.SqlServer;

public class SqlProjector : BaseProducer<SqlServerProjectOptions> {
    readonly GetConnection                    _getConnection;
    readonly ILogger<SqlServerProjectOptions> _log;

    public SqlProjector(GetConnection getConnection, ILogger<SqlServerProjectOptions> logger) {
        _getConnection = getConnection;
        _log           = logger;
        ReadyNow();
    }

    protected override async Task ProduceMessages(
        StreamName                   stream,
        IEnumerable<ProducedMessage> messages,
        SqlServerProjectOptions?     options,
        CancellationToken            cancellationToken = default
    ) {
        // One-by-one, we'll process each message. In reality, the projector will always handle just a single message
        foreach (var message in messages) {
            await ProduceLocal(message);
        }

        async Task ProduceLocal(ProducedMessage message) {
            if (message.Message is not ProjectionResult projectionResult
             || projectionResult.OperationCase == OperationOneofCase.Ignore) {
                return;
            }

            try {
                _log.LogTrace("Executing SQL {sql}", projectionResult.Execute.Sql);
                await _getConnection.ExecuteNonQuery(projectionResult.Execute.Sql, _ => { }, cancellationToken);
            }
            catch (Exception e) {
                _log.LogError(
                    "Failed to project event {id} because {reason}",
                    projectionResult.EventId,
                    e.Message
                );

                await message.Nack("Failed to project event", e);
            }
        }
    }
}

public record SqlServerProjectOptions;
