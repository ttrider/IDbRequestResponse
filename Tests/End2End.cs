using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTRider.Data.RequestResponse;

namespace Tests
{
    [TestClass]
    public class End2End
    {
        private static string connectionString;
        [ClassInitialize()]
        public static void ClassInit(TestContext testcontext)
        {
            connectionString = CreateTestDatabase("persons");
        }

        [TestMethod]
        public void SelectDataNoBufferReuseMemory()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Person Order By Id;";

            var request = DbRequest.Create(command, DbRequestMode.NoBufferReuseMemory);
            Assert.IsNotNull(request);

            using (var response = request.GetResponse())
            {
                Assert.IsNotNull(response);

                var records = response.Records.GetEnumerator();
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(1, records.Current[0]);
                Assert.AreEqual("James", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(2, records.Current[0]);
                Assert.AreEqual("Spoke", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(3, records.Current[0]);
                Assert.AreEqual("Bones", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(4, records.Current[0]);
                Assert.AreEqual("Uhura", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(5, records.Current[0]);
                Assert.AreEqual("Chekov", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(6, records.Current[0]);
                Assert.AreEqual("Sulu", records.Current[1]);
                Assert.IsFalse(records.MoveNext());
                records.Dispose();
            }
        }

        [TestMethod]
        public async Task SelectDataAsync()
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Person Order By Id;";

            var prereqcommand = connection.CreateCommand();
            prereqcommand.CommandText = "Insert Into Person (Id, Name) VALUES (7, 'Scotty');";

            var request = DbRequest.Create(command, null, new IDbCommand[] {prereqcommand});
            Assert.IsNotNull(request);

            using (var response = await request.GetResponseAsync())
            {
                Assert.IsNotNull(response);

                var records = response.Records.GetEnumerator();
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(1, records.Current[0]);
                Assert.AreEqual("James", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(2, records.Current[0]);
                Assert.AreEqual("Spoke", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(3, records.Current[0]);
                Assert.AreEqual("Bones", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(4, records.Current[0]);
                Assert.AreEqual("Uhura", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(5, records.Current[0]);
                Assert.AreEqual("Chekov", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(6, records.Current[0]);
                Assert.AreEqual("Sulu", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(7, records.Current[0]);
                Assert.AreEqual("Scotty", records.Current[1]);
                Assert.IsFalse(records.MoveNext());
                records.Dispose();
            }
        }

        [TestMethod]
        public void SelectDataNoBuffer()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Person Order By Id;";

            var request = DbRequest.Create(command, DbRequestMode.NoBuffer);
            Assert.IsNotNull(request);

            using (var response = request.GetResponse())
            {
                Assert.IsNotNull(response);

                var records = response.Records.GetEnumerator();
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(1, records.Current[0]);
                Assert.AreEqual("James", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(2, records.Current[0]);
                Assert.AreEqual("Spoke", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(3, records.Current[0]);
                Assert.AreEqual("Bones", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(4, records.Current[0]);
                Assert.AreEqual("Uhura", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(5, records.Current[0]);
                Assert.AreEqual("Chekov", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(6, records.Current[0]);
                Assert.AreEqual("Sulu", records.Current[1]);
                Assert.IsFalse(records.MoveNext());
                records.Dispose();
            }
        }

        [TestMethod]
        public void SelectDataBuffer()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Person Order By Id;";

            var request = DbRequest.Create(command, DbRequestMode.Buffer);
            Assert.IsNotNull(request);

            using (var response = request.GetResponse())
            {
                Assert.IsNotNull(response);

                var records = response.Records.GetEnumerator();
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(1, records.Current[0]);
                Assert.AreEqual("James", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(2, records.Current[0]);
                Assert.AreEqual("Spoke", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(3, records.Current[0]);
                Assert.AreEqual("Bones", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(4, records.Current[0]);
                Assert.AreEqual("Uhura", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(5, records.Current[0]);
                Assert.AreEqual("Chekov", records.Current[1]);
                Assert.IsTrue(records.MoveNext());
                Assert.AreEqual(6, records.Current[0]);
                Assert.AreEqual("Sulu", records.Current[1]);
                Assert.IsFalse(records.MoveNext());
                records.Dispose();
            }
        }

        [TestMethod]
        public void OutputParameters()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT @Name = Name FROM Person WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", 2);
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 256).Direction = ParameterDirection.Output;

            var request = DbRequest.Create(command, DbRequestMode.Buffer);
            Assert.IsNotNull(request);

            using (var response = request.GetResponse())
            {
                Assert.IsNotNull(response);
                Assert.AreEqual("Spoke", response.Output["@Name"]);
            }
        }

        public static string CreateTestDatabase(string id)
        {
            var name = "DB" + Guid.NewGuid().ToString("N") + id;

            var target = Path.GetFullPath(@".\" + name + ".mdf");

            //var cs = @"Data Source=(LocalDB)\v11.0;Integrated Security=True;Connect Timeout=30;AttachDbFilename=";
            //cs += target;
            var cs = @"Data Source=(LocalDB)\v11.0;Integrated Security=True;Connect Timeout=30;Initial Catalog=";
            cs += name;

            if (!File.Exists(target))
            {
                SqlConnection connection = new SqlConnection(@"server=(LocalDB)\v11.0");
                using (connection)
                {
                    connection.Open();

                    string sql = string.Format(@"
                        CREATE DATABASE
                            [{1}]
                        ON PRIMARY (
                           NAME={1}_data,
                           FILENAME = '{0}{1}_data.mdf'
                        )
                        LOG ON (
                            NAME=Test_log,
                            FILENAME = '{0}{1}_log.ldf'
                        )",
                        Path.GetFullPath(@".\"), name);

                    var command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();

                    // populate database
                    command = new SqlCommand("USE [" + name + "];", connection);
                    command.ExecuteNonQuery();

                    command = new SqlCommand("CREATE TABLE Person (Id int PRIMARY KEY NOT NULL, Name nvarchar(256) NULL); Insert Into Person (Id, Name) VALUES (1, 'James'), (2, 'Spoke'), (3, 'Bones'), (4, 'Uhura'), (5, 'Chekov'),(6, 'Sulu');", connection);
                    command.ExecuteNonQuery();

                    connection.Close();
                }
            }

            return cs;
        }
    }
}
