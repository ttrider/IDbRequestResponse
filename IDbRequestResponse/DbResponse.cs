// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace TTRider.Data.RequestResponse
{
    public class DbResponse : DbResponseBase, IDbResponse
    {
        private DataRecordEnumerable currentEnumerable;

        public static IDbResponse GetResponse(IDbRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Command == null) throw new ArgumentException("request.Command");
            if (request.Command.Connection == null) throw new ArgumentException("request.Command.Connection");

            var response = new DbResponse(request);
            response.ProcessRequest();
            return response;
        }

        public static async Task<IDbResponse> GetResponseAsync(IDbRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Command == null) throw new ArgumentException("request.Command");
            if (request.Command.Connection == null) throw new ArgumentException("request.Command.Connection");
            return await DbResponseAsync.GetResponseAsync(request);
        }

        private DbResponse(IDbRequest request)
            : base(request)
        {
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
                LogPrerequisite(prerequisiteCommand.GetCommandSummary());
                prerequisiteCommand.ExecuteNonQuery();
            }

            LogCommand(Command.GetCommandSummary());
            this.Connection = Command.Connection;
            this.Reader = Command.ExecuteReader(CommandBehavior.CloseConnection);
        }


        void OnRecordsetProcessed()
        {
            this.currentEnumerable = null;
            if (!this.Reader.NextResult())
            {
                this.completed = true;
                EnsureOutputValues();
                LogCompleted();
            }
        }

        public override IEnumerable<object[]> Records
        {
            get
            {
                if (this.Request.Mode == DbRequestMode.Buffer)
                {
                    return this.currentEnumerable ??
                           (this.currentEnumerable = new BufferedDataRecordEnumerable(this, this.OnRecordsetProcessed));
                }
                if (completed)
                {
                    return Enumerable.Empty<object[]>();
                }

                return this.currentEnumerable ??
                       (this.currentEnumerable = new DataRecordEnumerable(this, this.OnRecordsetProcessed));
            }
        }

        public IReadOnlyList<string> FieldNames
        {
            get
            {
                if (this.Reader != null && !this.Reader.IsClosed && this.Reader.FieldCount > 0)
                {
                    var fields = new string[this.Reader.FieldCount];
                    for (var i = 0; i < this.Reader.FieldCount; i++)
                    {
                        fields[i] = this.Reader.GetName(i);
                    }
                    return fields;
                }
                return new string[0];
            }
        }

        protected class DataRecordEnumerable : IEnumerable<object[]>
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

        protected class BufferedDataRecordEnumerable : DataRecordEnumerable
        {
            private readonly List<object[]> records;
            public BufferedDataRecordEnumerable(DbResponse response, Action onCompletedAction)
                : base(response, onCompletedAction)
            {
                this.records = new List<object[]>();
            }

            protected override IEnumerator<object[]> DoGetEnumerator()
            {
                using (var enumerator = base.DoGetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        this.records.Add(enumerator.Current);
                    }
                }
                return this.records.GetEnumerator();
            }
        }

        protected class DataRecordEnumerator : IEnumerator<object[]>
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
                if (this.completed || !this.response.Reader.Read())
                {
                    this.completed = true;
                    this.onCompletedAction();
                    return false;
                }

                if (this.buffer == null || this.response.Request.Mode != DbRequestMode.NoBufferReuseMemory)
                {
                    this.buffer = new object[this.response.Reader.FieldCount];
                }
                this.response.Reader.GetValues(this.buffer);
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

            public object[] Current => this.buffer;

            object IEnumerator.Current => Current;
        }
    }
}
