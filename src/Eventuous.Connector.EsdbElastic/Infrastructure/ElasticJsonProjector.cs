using System.Diagnostics.CodeAnalysis;
using Eventuous.Producers;
using Microsoft.Extensions.Logging;
using Nest;

namespace Eventuous.Connector.EsdbElastic.Infrastructure;

public class ElasticJsonProjector : BaseProducer<ElasticJsonProjectOptions> {
    readonly IElasticClient                _elasticClient;
    readonly ILogger<ElasticJsonProjector> _log;

    public ElasticJsonProjector(IElasticClient elasticClient, ILogger<ElasticJsonProjector> logger) {
        _elasticClient = elasticClient;
        _log           = logger;
        ReadyNow();
    }

    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly")]
    protected override async Task ProduceMessages(
        StreamName                   stream,
        IEnumerable<ProducedMessage> messages,
        ElasticJsonProjectOptions?   options,
        CancellationToken            cancellationToken = default
    ) {
        string    index     = stream;
        IndexName indexName = index;

        foreach (var message in messages) {
            if (message.Message is not ProjectionResult projectionResult) {
                continue;
            }

            switch (projectionResult.OperationCase) {
                case ProjectionResult.OperationOneofCase.None:   break;
                case ProjectionResult.OperationOneofCase.Ignore: break;
                case ProjectionResult.OperationOneofCase.Index:
                    _log.LogDebug("Indexing document with id {id} to {index}", projectionResult.Index.Id, index);

                    await _elasticClient.IndexAsync(
                        new IndexRequest<object>(projectionResult.Index.Document, indexName, projectionResult.Index.Id),
                        cancellationToken
                    );

                    break;
                case ProjectionResult.OperationOneofCase.Update:
                    _log.LogDebug("Updating document with id {id} to {index}", projectionResult.Update.Id, index);
                    var response = await _elasticClient.UpdateAsync(
                        new UpdateRequest<object, object>(indexName, projectionResult.Update.Id) {
                            Doc = projectionResult.Update.Document
                        },
                        cancellationToken
                    );

                    if (!response.IsValid) {
                        _log.LogError("Failed to update document {id} in {index} because {reason}", projectionResult.Update.Id, index, response.DebugInformation);
                    }
                    break;
                case ProjectionResult.OperationOneofCase.UpdateScript: break;
                case ProjectionResult.OperationOneofCase.Delete: break;
                default: throw new ArgumentOutOfRangeException(nameof(projectionResult.OperationCase));
            }
        }
    }
}

public record ElasticJsonProjectOptions;
