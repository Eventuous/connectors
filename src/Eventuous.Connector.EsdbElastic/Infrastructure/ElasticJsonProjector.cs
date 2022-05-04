using System.Diagnostics.CodeAnalysis;
using Eventuous.Producers;
using Microsoft.Extensions.Logging;
using Nest;
using static Eventuous.Connector.EsdbElastic.ProjectionResult;

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

            var resp = projectionResult.OperationCase switch {
                OperationOneofCase.Index  => await Index(projectionResult.Index, indexName, cancellationToken),
                OperationOneofCase.Update => await Update(projectionResult.Update, indexName, cancellationToken),
                _ => new ElasticCallResponse(true, "")
            };

            if (!resp.IsValid) {
                _log.LogError(
                    "Failed to update document {id} in {index} because {reason}",
                    projectionResult.Update.Id,
                    index,
                    resp.DebugInformation
                );

                throw new InvalidOperationException("Failed to execute call to Elasticsearch");
            }
        }
    }

    async Task<ElasticCallResponse> Index(
        Index             operation,
        IndexName         indexName,
        CancellationToken cancellationToken
    ) {
        _log.LogDebug("Indexing document with id {id} to {index}", operation.Id, indexName);

        var response = await _elasticClient.IndexAsync(
            new IndexRequest<object>(operation.Document, indexName, operation.Id),
            cancellationToken
        );

        return new ElasticCallResponse(response.IsValid, response.DebugInformation);
    }

    async Task<ElasticCallResponse> Update(
        Update            operation,
        IndexName         indexName,
        CancellationToken cancellationToken
    ) {
        _log.LogDebug("Updating document with id {id} to {index}", operation.Id, indexName);

        var response = await _elasticClient.UpdateAsync(
            new UpdateRequest<object, object>(indexName, operation.Id) {
                Doc = operation.Document
            },
            cancellationToken
        );

        return new ElasticCallResponse(response.IsValid, response.DebugInformation);
    }

    record ElasticCallResponse(bool IsValid, string DebugInformation);
}

public record ElasticJsonProjectOptions;
