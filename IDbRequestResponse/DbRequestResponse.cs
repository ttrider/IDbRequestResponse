// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;


namespace TTRider.Data.RequestResponse
{
    public abstract class DbRequestResponse
    {
        private readonly string sessionHash;
        protected IDbCommand Command;
        protected IDataReader Reader;
        private IDictionary<string, object> output;
        private int? returnCode;
        protected bool completed;
        private bool disposed;
        protected IDbConnection Connection;


        public static event EventHandler<LogEventArgs> PrerequisiteCommandExecuting;
        public static event EventHandler<LogEventArgs> CommandExecuting;
        public static event EventHandler<LogEventArgs> CommandExecuted;


        ~DbRequestResponse()
        {
            Dispose(false);
        }

        protected DbRequestResponse(IDbRequest request)
        {
            this.Request = request;
            this.Command = request.Command;
            this.sessionHash = (request.Command.CommandText + String.Join("-", request.PrerequisiteCommands) + DateTime.Now.ToString("s")).GetHashCode().ToString("x8");

        }

        protected void FireCompleted()
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }

        protected void EnsureOutputValues()
        {
            if (this.output == null)
            {
                // we need to fetch output data
                while (!this.completed)
                {
                    // ReSharper disable once UnusedVariable
                    var dummy = this.Records.Count();
                }

                var outputData = new Dictionary<string, object>();
                foreach (var parameter in Command.Parameters.OfType<DbParameter>().Where(p => p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output))
                {
                    var value = parameter.Value;
                    if (value == DBNull.Value)
                    {
                        value = null;
                    }
                    outputData[parameter.ParameterName] = value;
                }

                var retCode =
                    Command.Parameters.OfType<DbParameter>()
                        .FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
                if (retCode != null)
                {
                    this.returnCode = (int?)retCode.Value;
                }

                this.output = outputData;
            }
        }

        protected void LogPrerequisite(string message)
        {
            PrerequisiteCommandExecuting?.Invoke(this, new LogEventArgs(this.sessionHash, message));
        }

        protected void LogCommand(string message)
        {
            CommandExecuting?.Invoke(this, new LogEventArgs(this.sessionHash, message));
        }
        protected void LogCompleted()
        {
            CommandExecuted?.Invoke(this, new LogEventArgs(this.sessionHash, string.Empty));
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || disposed) return;

            if (Command != null)
            {
                Command.Dispose();
                Command = null;
            }

            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
            this.disposed = true;
        }

        public IDbRequest Request { get; }

        public event EventHandler Completed;

        public abstract IEnumerable<object[]> Records { get; }

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

        public bool HasMoreData => !this.completed;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}