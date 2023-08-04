// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector.Base.Grpc;
using Eventuous.Connector.Filters.Grpc;
using Eventuous.Producers.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Eventuous.Connector.EsdbMongo;

public class MongoJsonProjector : GrpcProjectingProducer<MongoJsonProjector, MongoJsonProjectOptions> {
    readonly IMongoDatabase              _database;
    readonly ILogger<MongoJsonProjector> _log;

    public MongoJsonProjector(IMongoDatabase database, ILogger<MongoJsonProjector> log)
        : base(TracingOptions) {
        _database = database;
        _log      = log;

        On<InsertOne>((message,  token) => InsertOne(message.Stream, message.Message, token));
        On<InsertMany>((message, token) => InsertMany(message.Stream, message.Message, token));
        On<UpdateOne>((message,  token) => UpdateOne(message.Stream, message.Message, token));
        On<UpdateMany>((message, token) => UpdateMany(message.Stream, message.Message, token));
        On<DeleteOne>((message,  token) => DeleteOne(message.Stream, message.Message, token));
        On<DeleteMany>((message, token) => DeleteMany(message.Stream, message.Message, token));
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

    Task InsertMany(string collection, InsertMany insertMany, CancellationToken cancellationToken) {
        _log.LogTrace("Inserting {@Documents}", insertMany);

        return _database
            .GetCollection<BsonDocument>(collection)
            .InsertManyAsync(insertMany.Documents.Select(d => BsonDocument.Parse(d.ToString())), cancellationToken: cancellationToken);
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

    Task UpdateMany(string collection, UpdateMany updateMany, CancellationToken cancellationToken) {
        _log.LogTrace("Updating with {@Update}", updateMany);

        return _database.GetCollection<BsonDocument>(collection)
            .UpdateManyAsync(
                new JsonFilterDefinition<BsonDocument>(updateMany.Filter.ToString()),
                new JsonUpdateDefinition<BsonDocument>(updateMany.Update.ToString()),
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

    Task DeleteMany(string collection, DeleteMany deleteMany, CancellationToken cancellationToken) {
        _log.LogTrace("Deleting {@Delete}", deleteMany);

        return _database
            .GetCollection<BsonDocument>(collection)
            .DeleteManyAsync(new JsonFilterDefinition<BsonDocument>(deleteMany.Filter.ToString()), cancellationToken);
    }
}

public record MongoJsonProjectOptions;
