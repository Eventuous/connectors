using System.Diagnostics.CodeAnalysis;
using Eventuous.Producers;
using Eventuous.Producers.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using static Eventuous.Connector.Mongo.ProjectionResult;

namespace Eventuous.Connector.Mongo;

public class MongoJsonProjector : BaseProducer<MongoJsonProjectOptions> {
    readonly IMongoDatabase              _database;
    readonly ILogger<MongoJsonProjector> _log;

    public MongoJsonProjector(IMongoDatabase database, ILogger<MongoJsonProjector> log)
        : base(false, TracingOptions) {
        _database = database;
        _log      = log;
    }

    static readonly ProducerTracingOptions TracingOptions = new() {
        MessagingSystem  = "mongodb",
        DestinationKind  = "collection",
        ProduceOperation = "project"
    };

    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly")]
    protected override async Task ProduceMessages(
        StreamName                   stream,
        IEnumerable<ProducedMessage> messages,
        MongoJsonProjectOptions?     options,
        CancellationToken            cancellationToken = default
    ) {
        var collection = _database.GetCollection<BsonDocument>(stream);

        foreach (var message in messages) {
            await ProduceLocal(message);
        }

        async Task ProduceLocal(ProducedMessage message) {
            if (message.Message is not ProjectionResult projectionResult
             || projectionResult.OperationCase == OperationOneofCase.Ignore) {
                return;
            }

            try {
                var resp = projectionResult.OperationCase switch {
                    OperationOneofCase.InsertOne => InsertOne(
                        projectionResult.InsertOne.Document,
                        collection,
                        cancellationToken
                    ),
                    OperationOneofCase.UpdateOne => UpdateOne(
                        projectionResult.UpdateOne.Filter,
                        projectionResult.UpdateOne.Update,
                        collection,
                        cancellationToken
                    ),
                    OperationOneofCase.DeleteOne => DeleteOne(
                        projectionResult.DeleteOne.Filter,
                        collection,
                        cancellationToken
                    ),
                    _ => default
                };

                if (resp is { IsCompleted: false }) {
                    await resp.NoContext();
                }

                await message.Ack<MongoJsonProjector>().NoContext();
            }
            catch (Exception ex) {
                await message.Nack<MongoJsonProjector>(ex.Message, ex).NoContext();
            }
        }
    }

    Task InsertOne(string document, IMongoCollection<BsonDocument> collection, CancellationToken cancellationToken) {
        _log.LogTrace("Inserting {Document}", document);
        return collection.InsertOneAsync(BsonDocument.Parse(document), cancellationToken: cancellationToken);
    }

    Task UpdateOne(
        string                         filter,
        string                         update,
        IMongoCollection<BsonDocument> collection,
        CancellationToken              cancellationToken
    ) {
        _log.LogTrace("Updating {Filter} with {Update}", filter, update);

        return collection.UpdateOneAsync(
            new JsonFilterDefinition<BsonDocument>(filter),
            new JsonUpdateDefinition<BsonDocument>(update),
            new UpdateOptions { IsUpsert = true },
            cancellationToken
        );
    }

    Task DeleteOne(string filter, IMongoCollection<BsonDocument> collection, CancellationToken cancellationToken) {
        _log.LogTrace("Deleting {Filter}", filter);
        return collection.DeleteOneAsync(new JsonFilterDefinition<BsonDocument>(filter), cancellationToken);
    }
}

public record MongoJsonProjectOptions;
