using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTRider.Data.RequestResponse;
using TTRider.Test;

namespace Tests
{
    [TestClass]
    public class End2End
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testcontext)
        {
        }


        void Initializer(SqlConnection connection)
        {
            var command = new SqlCommand("CREATE TABLE Person (Id int PRIMARY KEY NOT NULL, Name nvarchar(256) NULL); Insert Into Person (Id, Name) VALUES (1, 'James'), (2, 'Spoke'), (3, 'Bones'), (4, 'Uhura'), (5, 'Chekov'),(6, 'Sulu');", connection);
            command.ExecuteNonQuery();
            command = new SqlCommand("CREATE TABLE Person_prereq01 (Id int PRIMARY KEY NOT NULL, Name nvarchar(256) NULL); Insert Into Person_Prereq01 (Id, Name) VALUES (1, 'James'), (2, 'Spoke'), (3, 'Bones'), (4, 'Uhura'), (5, 'Chekov'),(6, 'Sulu');", connection);
            command.ExecuteNonQuery();
        }



        [TestMethod]
        public void SelectDataNoBufferReuseMemory()
        {
            using (var db = TestDatabaseContext.Create(Initializer))
            using (var connection = db.CreateConnection())
            {
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
        }

        [TestMethod]
        public async Task SelectDataAsync()
        {
            using (var db = TestDatabaseContext.Create(Initializer))
            using (var connection = db.CreateConnection())
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name FROM Person_prereq01 Order By Id;";

                var prereqcommand = connection.CreateCommand();
                prereqcommand.CommandText = "Insert Into Person_prereq01 (Id, Name) VALUES (7, 'Scotty');";

                var request = DbRequest.Create(command, null, new IDbCommand[] { prereqcommand });
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
        }

        [TestMethod]
        public void SelectDataNoBuffer()
        {
            using (var db = TestDatabaseContext.Create(Initializer))
            using (var connection = db.CreateConnection())
            {
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
        }

        [TestMethod]
        public void SelectDataBuffer()
        {
            using (var db = TestDatabaseContext.Create(Initializer))
            using (var connection = db.CreateConnection())
            {
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
        }

        [TestMethod]
        public void OutputParameters()
        {
            using (var db = TestDatabaseContext.Create(Initializer))
            using (var connection = db.CreateConnection())
            {
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
        }

        [TestMethod]
        public async Task VerifyConnectionLeak()
        {
            using (var db = TestDatabaseContext.Create(Initializer))
            {
                using (var connection = db.CreateConnection())
                {
                    await connection.OpenAsync();
                    // simulate aborted read
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM sys.objects;";
                    var request = DbRequest.Create(command, DbRequestMode.NoBuffer);
                    var response = request.GetResponse();
                    response.Dispose();
                }

                using (var connection = db.CreateConnection())
                {
                    await connection.OpenAsync();
                    // simulate aborted read after one row
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM sys.objects;";
                    var request = DbRequest.Create(command, DbRequestMode.NoBuffer);
                    var response = request.GetResponse();
                    var first = response.Records.First();
                    response.Dispose();
                }
            }
        }
    }
}
