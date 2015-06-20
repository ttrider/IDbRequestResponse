// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2015 All Rights Reserved
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using log4net;


namespace TTRider.Data.RequestResponse
{
    public class DbResponse : IDbResponse, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DbResponse));

        private IDbCommand command;
        private IDbCommand connection;
        private IDataReader reader;
        private IDictionary<string, object> output;
        private int? returnCode;
        private bool completed;
        private bool disposed;
        private DataRecordEnumerable currentEnumerable;

        public static IDbResponse GetResponse(IDbRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.Command == null) throw new ArgumentException("request.Command");
            if (request.Command.Connection == null) throw new ArgumentException("request.Command.Connection");

            var response = new DbResponse(request);
            response.ProcessRequest();
            return response;
        }

        public static async Task<IDbResponse> GetResponseAsync(IDbRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.Command == null) throw new ArgumentException("request.Command");
            if (request.Command.Connection == null) throw new ArgumentException("request.Command.Connection");

            var response = new DbResponse(request);
            await response.ProcessRequestAsync();
            return response;
        }

        private DbResponse(IDbRequest request)
        {
            this.Request = request;
            this.command = request.Command;
            this.sessionHash = (request.Command.CommandText + string.Join("-", request.PrerequisiteCommands) + DateTime.Now.ToString("s")).GetHashCode().ToString("x8");
        }

        ~DbResponse()
        {
            Dispose(false);
        }

        private readonly string sessionHash;

        private async Task ProcessRequestAsync()
        {
            if (this.Request.Command.Connection.State != ConnectionState.Open)
            {
                await this.Request.Command.Connection.OpenAsync();
            }

            // process Prerequisites
            foreach (var prerequisiteCommand in this.Request.PrerequisiteCommands)
            {
                prerequisiteCommand.Connection = this.Request.Command.Connection;
                Log.DebugFormat("{0}-PREREQUISITE_COMMAND: {1}", this.sessionHash, prerequisiteCommand.GetCommandSummary());
                await prerequisiteCommand.ExecuteNonQueryAsync();
            }

            Log.DebugFormat("{0}-COMMAND: {1}", this.sessionHash, command.GetCommandSummary());
            this.reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        private void ProcessRequest()
        {
            if (this.Request.Command.Connection.State != ConnectionState.Open)
            {
                this.Request.Command.Connection.Open();
            }

            // process Prerequisites
            foreach (var prerequisiteCommand in this.Request.PrerequisiteCommands)
            {
                prerequisiteCommand.Connection = this.Request.Command.Connection;
                Log.DebugFormat("{0}-PREREQUISITE_COMMAND: {1}", this.sessionHash, prerequisiteCommand.GetCommandSummary());
                prerequisiteCommand.ExecuteNonQuery();
            }

            Log.DebugFormat("{0}-COMMAND: {1}", this.sessionHash, command.GetCommandSummary());
            this.reader = command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        private void EnsureOutputValues()
        {
            if (this.output == null)
            {
                // we need to fetch output data
                while (!this.completed)
                {
                    foreach (var record in this.Records)
                    {
                        var dummy = record;
                    }
                }

                var outputData = new Dictionary<string, object>();
                foreach (var parameter in command.Parameters.OfType<DbParameter>().Where(p => p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output))
                {
                    var value = parameter.Value;
                    if (value == DBNull.Value)
                    {
                        value = null;
                    }
                    outputData[parameter.ParameterName] = value;
                }

                var retCode =
                            command.Parameters.OfType<DbParameter>()
                                .FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
                if (retCode != null)
                {
                    this.returnCode = (int?)retCode.Value;
                }

                this.output = outputData;
            }
        }

        void OnRecordsetProcessed()
        {
            this.currentEnumerable = null;
            if (!this.reader.NextResult())
            {
                this.completed = true;
                EnsureOutputValues();
                Log.DebugFormat("{0}-COMMAND COMPLETED", this.sessionHash);
                if (Completed != null)
                {
                    Completed(this, EventArgs.Empty);
                }
            }
        }

        public IDbRequest Request { get; private set; }

        public IEnumerable<object[]> Records
        {
            get
            {
                if (this.Request.Mode == DbRequestMode.Buffer)
                {
                    return this.currentEnumerable ??
                           (this.currentEnumerable = new BufferedDataRecordEnumerable(this, this.OnRecordsetProcessed));
                }
                else
                {
                    if (completed)
                    {
                        return Enumerable.Empty<object[]>();
                    }

                    return this.currentEnumerable ??
                           (this.currentEnumerable = new DataRecordEnumerable(this, this.OnRecordsetProcessed));
                }
            }
        }

        public IDictionary<string, object> Output
        {
            get
            {
                this.EnsureOutputValues();
                return this.output;
            }
        }

        public int? ReturnCode
        {
            get
            {
                this.EnsureOutputValues();
                return this.returnCode;
            }
        }

        public bool HasMoreData
        {
            get { return !this.completed; }
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (command != null)
            {
                command.Dispose();
                command = null;
            }

            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            this.disposed = true;
        }

        public event EventHandler Completed;

        class DataRecordEnumerable : IEnumerable<object[]>
        {
            private readonly DbResponse response;
            private readonly Action onCompletedAction;

            public DataRecordEnumerable(DbResponse response, Action onCompletedAction)
            {
                this.response = response;
                this.onCompletedAction = onCompletedAction;
            }


            public IEnumerator<object[]> GetEnumerator()
            {
                return this.DoGetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            protected virtual IEnumerator<object[]> DoGetEnumerator()
            {
                return new DataRecordEnumerator(response, this.onCompletedAction);
            }
        }

        private class BufferedDataRecordEnumerable : DataRecordEnumerable
        {
            private readonly List<object[]> records;
            public BufferedDataRecordEnumerable(DbResponse response, Action onCompletedAction)
                : base(response, onCompletedAction)
            {
                this.records = new List<object[]>();
            }

            protected override IEnumerator<object[]> DoGetEnumerator()
            {
                using(var enumerator = base.DoGetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        this.records.Add(enumerator.Current);
                    }
                }
                return this.records.GetEnumerator();
            }
        }

        class DataRecordEnumerator : IEnumerator<object[]>
        {
            private readonly DbResponse response;
            private readonly Action onCompletedAction;
            object[] buffer;
            private bool completed;

            public DataRecordEnumerator(DbResponse response, Action onCompletedAction)
            {
                this.response = response;
                this.onCompletedAction = onCompletedAction;
            }

            public void Dispose()
            {
                // read till the end;
                while (this.MoveNext()) { }
            }

            public bool MoveNext()
            {
                if (this.completed || !this.response.reader.Read())
                {
                    this.completed = true;
                    this.onCompletedAction();
                    return false;
                }

                if (this.buffer == null || this.response.Request.Mode != DbRequestMode.NoBufferReuseMemory)
                {
                    this.buffer = new object[this.response.reader.FieldCount];
                }
                this.response.reader.GetValues(this.buffer);
                for (int i = 0; i < this.buffer.Length; i++)
                {
                    if (this.buffer[i] is DBNull)
                    {
                        this.buffer[i] = null;
                    }
                }
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public object[] Current
            {
                get { return this.buffer; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
