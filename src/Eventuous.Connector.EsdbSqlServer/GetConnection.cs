// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Eventuous.Connector.EsdbSqlServer;

public delegate Task<DbConnection> GetConnection(CancellationToken cancellationToken);

public static class ConnectionFactory {
    public static GetConnection GetConnectionFactory(string connectionString) {
        return GetConnection;

        async Task <DbConnection> GetConnection(CancellationToken cancellationToken) {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
