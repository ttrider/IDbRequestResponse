using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace Tests
{
    public class TestDatabaseContext : IDisposable
    {
        public static TestDatabaseContext Create(Action<SqlConnection> initialier = null)
        {
            return new TestDatabaseContext("(LocalDB)\\v11.0", "TestDatabaseContext", null, initialier);
        }

        public static TestDatabaseContext Create(string key, Action<SqlConnection> initialier = null)
        {
            return new TestDatabaseContext("(LocalDB)\\v11.0", key, ".\\",initialier);
        }

        public static TestDatabaseContext Create(string server, string key, string path = null, Action<SqlConnection> initialier = null)
        {
            return new TestDatabaseContext(server, key, path, initialier);
        }

        private readonly string name;

        private readonly string cs;
        private readonly List<SqlConnection> connections = new List<SqlConnection>();



        TestDatabaseContext(string datasource, string id, string path=".\\", Action<SqlConnection> initialier = null)
        {
            name = $"DB{id}_{Guid.NewGuid().ToString("N")}";

            var dataFile = Path.GetFullPath($"{path}{name}_data.mdf");
            var logFile = Path.GetFullPath($"{path}{name}_log.ldf");

            var connection = new SqlConnection($"server={datasource}");
            using (connection)
            {
                connection.Open();

                string sql = string.Format(@"
                        CREATE DATABASE
                            [{0}]
                        ON PRIMARY (
                           NAME={0}_data,
                           FILENAME = '{1}'
                        )
                        LOG ON (
                            NAME={0}_log,
                            FILENAME = '{2}'
                        )",
                    name, dataFile, logFile);

                var command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
                this.cs = $"Data Source={datasource};Integrated Security=True;Connect Timeout=30;Initial Catalog={name};";
                connection.Close();
            }

            if (initialier != null)
            {
                this.Initialize(initialier);
            }
        }

        public void Dispose()
        {
            foreach (var sqlConnection in this.connections)
            {
                sqlConnection.Dispose();
            }
            SqlConnection.ClearAllPools();
            var connection = new SqlConnection(@"server=(LocalDB)\v11.0");
            connection.Open();
            connection.ChangeDatabase("master");


            string sql = $"DROP DATABASE [{this.name}];";
            var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
            connection.Dispose();
            SqlConnection.ClearAllPools();
        }

        public SqlConnection CreateConnection()
        {
            var c = new SqlConnection(this.cs);
            this.connections.Add(c);
            return c;
        }

        public void Initialize(Action<SqlConnection> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            using (var connection = CreateConnection())
            {
                connection.Open();
                handler(connection);
            }
        }

        public async Task InitializeAsync(Func<SqlConnection, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();
                await handler(connection);
            }
        }
    }
}