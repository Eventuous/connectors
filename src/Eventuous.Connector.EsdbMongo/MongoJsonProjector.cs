using Eventuous.Connector.Base.Grpc;
using Eventuous.Producers.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Eventuous.Connector.EsdbMongo;

public class MongoJsonProjector : GrpcProjectingProducer<MongoJsonProjector, MongoJsonProjectOptions> {
    readonly IMongoDatabase              _database;
    readonly ILogger<MongoJsonProjector> _log;

    public MongoJsonProjector(IMongoDatabase database, ILogger<MongoJsonProjector> log) : base(TracingOptions) {
        _database = database;
        _log      = log;

        On<InsertOne>((message, token) => InsertOne(message.Stream, message.Message, token));
        On<UpdateOne>((message, token) => UpdateOne(message.Stream, message.Message, token));
        On<DeleteOne>((message, token) => DeleteOne(message.Stream, message.Message, token));
    }

    static readonly ProducerTracingOptions TracingOptions = new() {
        MessagingSystem  = "mongodb",
        DestinationKind  = "collection",
        ProduceOperation = "project"
    };

    Task InsertOne(string collection, InsertOne insertOne, CancellationToken cancellationToken) {
        _log.LogTrace("Inserting {@Document}", insertOne);

        return _database
            .GetCollection<BsonDocument>(collection)
            .InsertOneAsync(BsonDocument.Parse(insertOne.Document.ToString()), cancellationToken: cancellationToken);
    }

    Task UpdateOne(string collection, UpdateOne updateOne, CancellationToken cancellationToken) {
        _log.LogTrace("Updating with {@Update}", updateOne);

        return _database.GetCollection<BsonDocument>(collection)
            .UpdateOneAsync(
                new JsonFilterDefinition<BsonDocument>(updateOne.Filter.ToString()),
                new JsonUpdateDefinition<BsonDocument>(updateOne.Update.ToString()),
                new UpdateOptions { IsUpsert = true },
                cancellationToken
            );
    }

    Task DeleteOne(string collection, DeleteOne deleteOne, CancellationToken cancellationToken) {
        _log.LogTrace("Deleting {@Delete}", deleteOne);

        return _database
            .GetCollection<BsonDocument>(collection)
            .DeleteOneAsync(new JsonFilterDefinition<BsonDocument>(deleteOne.Filter.ToString()), cancellationToken);
    }
}

public record MongoJsonProjectOptions;
