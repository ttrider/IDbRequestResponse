using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TTRider.Data.RequestResponse;
using Xunit;

namespace Tests
{
    public class Tests
    {
        [Fact]
        public async Task NoDataBack()
        {
            var connection = new SqlConnection("Data Source=.; Integrated Security=true;");
            connection.Open();
            var command = new SqlCommand("PRINT 'no data returned'", connection);
            var request = DbRequest.Create(command);
            var response = await request.GetResponseAsync();

            Assert.NotNull(response);
            Assert.NotNull(response.FieldNames);
            Assert.Equal(0, response.FieldNames.Count);
            Assert.Null(response.Records);
            Assert.Null(response.ReturnCode);
            Assert.Equal(0, response.Output.Count);


            response.Dispose();
            Assert.Equal(ConnectionState.Closed, response.Request.Command.Connection.State);
        }


        [Theory]
        [InlineData(
            "SET @p2=N'v2v';" +
            "SET @p3=@p1;"
            , false)]
        [InlineData(
            "SET @p2=N'v2v';" +
            "SET @p3=@p1;"
            , true)]
        [InlineData(
            "SET @p2=N'v2v';" +
            "SELECT * FROM sys.objects;" +
            "SET @p3=@p1;" +
            "SELECT * FROM sys.objects;"
            , false)]

        public async Task OutputValues(string query, bool pullRecordsets)
        {
            var connection = new SqlConnection("Data Source=.; Integrated Security=true;");
            connection.Open();
            var command = new SqlCommand(query, connection);
            var p = command.Parameters.AddWithValue("@p1", "v1");
            p.Direction = ParameterDirection.Input;
            p.SqlDbType = SqlDbType.NVarChar;
            p.Size = -1;

            p = command.Parameters.AddWithValue("@p2", "");
            p.Direction = ParameterDirection.Output;
            p.SqlDbType = SqlDbType.NVarChar;
            p.Size = -1;

            p = command.Parameters.AddWithValue("@p3", "");
            p.Direction = ParameterDirection.Output;
            p.SqlDbType = SqlDbType.NVarChar;
            p.Size = -1;

            var request = DbRequest.Create(command);
            var response = await request.GetResponseAsync();

            Assert.NotNull(response);

            if (pullRecordsets)
            {
                Assert.NotNull(response.FieldNames);
                Assert.Equal(0, response.FieldNames.Count);
                Assert.Null(response.Records);
                Assert.Null(response.ReturnCode);
            }

            Assert.Equal(2, response.Output.Count);
            Assert.Equal("v1", response.Output["@p3"]);
            Assert.Equal("v2v", response.Output["@p2"]);


            response.Dispose();
            Assert.Equal(ConnectionState.Closed, response.Request.Command.Connection.State);
        }


        [Theory]
        //[InlineData(
        //   "SET @p2=N'v2v';" +
        //   "SET @p3=@p1;"
        //   , false)]
        //[InlineData(
        //   "SET @p2=N'v2v';" +
        //   "SET @p3=@p1;"
        //   , true)]
        [InlineData(
           "SELECT name, schema_id " +
           "FROM sys.schemas " +
           "WHERE schema_id <=3 " +
           "ORDER BY schema_id;"
           , false)]

        public async Task RecordsetValues(string query, bool pullRecordsets)
        {
            var connection = new SqlConnection("Data Source=.; Integrated Security=true;");
            connection.Open();
            var command = new SqlCommand(query, connection);
            var request = DbRequest.Create(command);
            var response = await request.GetResponseAsync();

            Assert.NotNull(response);

            Assert.NotNull(response.FieldNames);
            Assert.Equal(2, response.FieldNames.Count);
            Assert.Equal("name", response.FieldNames[0]);
            Assert.Equal("schema_id", response.FieldNames[1]);

            var records = response.Records;
            Assert.NotNull(records);

            var recordList = records.ToList();
            Assert.Equal(3, recordList.Count);
            Assert.Equal("dbo", recordList[0][0]);

            Assert.Null(response.Records);

            Assert.Equal(0, response.Output.Count);

            response.Dispose();
            Assert.Equal(ConnectionState.Closed, response.Request.Command.Connection.State);
        }

        [Fact]
        public async Task SingleRecordsetBack()
        {
            var connection = new SqlConnection("Data Source=.; Integrated Security=true;");
            connection.Open();

            var command = new SqlCommand("SELECT top 10 * from sys.objects", connection);

            var request = DbRequest.Create(command);

            var response = await request.GetResponseAsync();

            response.Dispose();


        }

        [Fact]
        public async Task MultipleRecordsetBack()
        {
            var connection = new SqlConnection("Data Source=.; Integrated Security=true;");
            connection.Open();

            var command = new SqlCommand("SELECT top 10 * from sys.tables;SELECT top 10 * from sys.objects", connection);

            var request = DbRequest.Create(command);

            var response = await request.GetResponseAsync();

            response.Dispose();


        }
    }
}
