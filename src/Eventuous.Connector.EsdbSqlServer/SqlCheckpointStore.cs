// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Data;
using System.Data.Common;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.Extensions.Logging;

namespace Eventuous.Connector.EsdbSqlServer;

public class SqlCheckpointStore(GetConnection getConnection, ILogger<SqlCheckpointStore> log) : ICheckpointStore {
    const string TableName = "Checkpoints";

    const string CreateTableSql      = $"CREATE TABLE {TableName} (ID VARCHAR(100) NOT NULL PRIMARY KEY, Position BIGINT)";
    const string InsertCheckpointSql = $"INSERT INTO {TableName} (ID, Position) VALUES (@CheckpointId, @Position)";
    const string UpdateCheckpointSql = $"UPDATE {TableName} SET Position = @Position WHERE ID = @CheckpointId";

    public async ValueTask<Checkpoint> GetLastCheckpoint(string checkpointId, CancellationToken cancellationToken) {
        await EnsureTableExists(cancellationToken);

        var checkpoint = await getConnection.ExecuteQuery(
            $"SELECT Position FROM {TableName} WHERE ID = @CheckpointId",
            cmd => cmd.AddParameter("@CheckpointId", checkpointId),
            GetCheckpoint,
            cancellationToken
        );

        log.LogInformation(
            "Loaded checkpoint {CheckpointId} with value {Position}",
            checkpointId,
            checkpoint.Position
        );

        return checkpoint;

        async Task<Checkpoint> GetCheckpoint(DbCommand cmd, CancellationToken ct) {
            var cp = await cmd.ExecuteReaderAsync(ct);

            if (!await cp.ReadAsync(ct)) {
                await AddCheckpoint();

                return new Checkpoint(checkpointId, null);
            }

            var value = cp.GetInt64(0);

            return new Checkpoint(checkpointId, value == -1 ? null : (ulong?)value);
        }

        Task AddCheckpoint()
            => getConnection.ExecuteNonQuery(
                InsertCheckpointSql,
                cmd => {
                    cmd.AddParameter("@CheckpointId", checkpointId);
                    cmd.AddParameter("@Position", -1);
                },
                cancellationToken
            );
    }

    public async ValueTask<Checkpoint> StoreCheckpoint(
        Checkpoint        checkpoint,
        bool              force,
        CancellationToken cancellationToken
    ) {
        await getConnection.ExecuteNonQuery(
            UpdateCheckpointSql,
            cmd => {
                cmd.AddParameter("@CheckpointId", checkpoint.Id);
                cmd.AddParameter("@Position", checkpoint.Position.HasValue ? (long)checkpoint.Position : -1);
            },
            cancellationToken
        );

        log.LogDebug("Stored checkpoint {CheckpointId} with value {Position}", checkpoint.Id, checkpoint.Position);

        return checkpoint;
    }

    async Task EnsureTableExists(CancellationToken cancellationToken) {
        if (await Exists()) return;

        log.LogInformation("Creating the checkpoints table");
        await CreateTable();

        return;

        async Task<bool> Exists() {
            await using var connection = await getConnection(cancellationToken);
            await using var command    = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@table) SELECT 1 ELSE SELECT 0";
            command.AddParameter("@table", TableName);
            var exists = (int)(await command.ExecuteScalarAsync(cancellationToken))!;

            return (exists == 1);
        }

        Task CreateTable() => getConnection.ExecuteNonQuery(CreateTableSql, _ => { }, cancellationToken);
    }
}

public static class DbCommandExtensionMethods {
    public static void AddParameter(this IDbCommand command, string name, object? value) {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value         = value;
        command.Parameters.Add(parameter);
    }

    public static async Task ExecuteNonQuery(
        this GetConnection getConnection,
        string             query,
        Action<DbCommand>  configureCommand,
        CancellationToken  cancellationToken
    ) {
        await using var connection = await getConnection(cancellationToken);
        await using var command    = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = query;
        configureCommand(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<T> ExecuteQuery<T>(
        this GetConnection                          getConnection,
        string                                      query,
        Action<DbCommand>                           configureCommand,
        Func<DbCommand, CancellationToken, Task<T>> action,
        CancellationToken                           cancellationToken
    ) {
        await using var connection = await getConnection(cancellationToken);
        await using var command    = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = query;
        configureCommand(command);

        return await action(command, cancellationToken);
    }
}
