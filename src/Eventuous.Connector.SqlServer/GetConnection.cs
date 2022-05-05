using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Eventuous.Connector.SqlServer;

public delegate Task<DbConnection> GetConnection(CancellationToken cancellationToken);

public static class ConnectionFactory {
    public static GetConnection GetConnectionFactory(string connectionString) {
        async Task <DbConnection> GetConnection(CancellationToken cancellationToken) {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        
        return GetConnection;
    }
}
