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
using log4net;

namespace TTRider.Data.RequestResponse
{
    public abstract class DbResponseBase
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(DbResponse));
        private readonly string sessionHash;

        protected IDbCommand Command;
        protected IDataReader Reader;
        private IDictionary<string, object> output;
        private int? returnCode;
        protected bool completed;
        private bool disposed;
        protected IDbConnection Connection;

        ~DbResponseBase()
        {
            Dispose(false);
        }

        protected DbResponseBase(IDbRequest request)
        {
            this.Request = request;
            this.Command = request.Command;
            this.sessionHash = (request.Command.CommandText + String.Join("-", request.PrerequisiteCommands) + DateTime.Now.ToString("s")).GetHashCode().ToString("x8");
        }

        protected void LogPrerequisite(string message)
        {
            Log.DebugFormat("{0}-PREREQUISITE_COMMAND: {1}", this.sessionHash, message);
        }

        protected void LogCommand(string message)
        {
            Log.DebugFormat("{0}-COMMAND: {1}", this.sessionHash, message);
        }
        protected void LogCompleted()
        {
            Log.DebugFormat("{0}-COMMAND: COMPLETED", this.sessionHash);
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