// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;


namespace TTRider.Data.RequestResponse
{
    public class DbResponseAsync : DbResponseBase, IDbResponse
    {
        private BlockingCollection<object[]> currentEnumerable;

        DbResponseAsync(IDbRequest request) :
            base(request)
        { }

        internal static async Task<IDbResponse> GetResponseAsync(IDbRequest request)
        {
            var response = new DbResponseAsync(request);
            await response.ProcessRequestAsync();
            return response;
        }

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
                LogPrerequisite(prerequisiteCommand.GetCommandSummary());
                await prerequisiteCommand.ExecuteNonQueryAsync();
            }

            LogCommand(Command.GetCommandSummary());
            this.Connection = Command.Connection;
            this.Reader = await Command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }


        async Task OnRecordsetProcessed()
        {
            this.currentEnumerable = null;
            if (!await this.Reader.NextResultAsync())
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
                var items = this.currentEnumerable;
                if (items == null)
                {
                    items = new BlockingCollection<object[]>();
                    this.currentEnumerable = items;
                    ReadAll(items);
                }
                return items.GetConsumingEnumerable();
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

        async void ReadAll(BlockingCollection<object[]> items)
        {
            while (await this.Reader.ReadAsync())
            {
                var buffer = new object[this.Reader.FieldCount];
                this.Reader.GetValues(buffer);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] is DBNull)
                    {
                        buffer[i] = null;
                    }
                }
                items.Add(buffer);
            }
            items.CompleteAdding();
            await this.OnRecordsetProcessed();
        }
    }
}
