using Eventuous.Connector.Base.Grpc;
using Eventuous.Producers.Diagnostics;
using Microsoft.Extensions.Logging;
using Nest;

// using static Eventuous.Connector.EsdbElastic.ProjectionResult;

namespace Eventuous.Connector.EsdbElastic;

public class ElasticJsonProjector : GrpcProjectingProducer<ElasticJsonProjector, ElasticJsonProjectOptions> {
    readonly IElasticClient                _elasticClient;
    readonly ILogger<ElasticJsonProjector> _log;

    public ElasticJsonProjector(IElasticClient elasticClient, ILogger<ElasticJsonProjector> logger)
        : base(false, TracingOptions) {
        _elasticClient = elasticClient;
        _log           = logger;

        On<Index>((message,  token) => Execute(message, token, IndexOne));
        On<Update>((message, token) => Execute(message, token, UpdateOne));

        async Task Execute<T>(
            ProjectedMessage<T>                                              msg,
            CancellationToken                                                cancellationToken,
            Func<T, IndexName, CancellationToken, Task<ElasticCallResponse>> execute
        ) {
            var result = await execute(msg.Message, msg.Stream.ToString(), cancellationToken);

            if (!result.IsValid) {
                throw new ApplicationException(result.DebugInformation, result.Exception);
            }
        }
    }

    static readonly ProducerTracingOptions TracingOptions = new() {
        MessagingSystem  = "elasticsearch",
        DestinationKind  = "index",
        ProduceOperation = "project"
    };

    async Task<ElasticCallResponse> IndexOne(
        Index             operation,
        IndexName         indexName,
        CancellationToken cancellationToken
    ) {
        _log.LogTrace("Indexing document with id {id} to {index}", operation.Id, indexName);
        var response = await _elasticClient.IndexAsync(
            new IndexRequest<object>(operation.Document.ToString(), indexName, operation.Id),
            cancellationToken
        );

        return new ElasticCallResponse(response.IsValid, response.DebugInformation, response.OriginalException);
    }

    async Task<ElasticCallResponse> UpdateOne(
        Update            operation,
        IndexName         indexName,
        CancellationToken cancellationToken
    ) {
        _log.LogTrace("Updating document with id {id} to {index}", operation.Id, indexName);
        var response = await _elasticClient.UpdateAsync(
            new UpdateRequest<object, object>(indexName, operation.Id) {
                Doc = operation.Document.ToString()
            },
            cancellationToken
        );

        return new ElasticCallResponse(response.IsValid, response.DebugInformation, response.OriginalException);
    }

    record ElasticCallResponse(bool IsValid, string DebugInformation, Exception? Exception);
}

public record ElasticJsonProjectOptions;
