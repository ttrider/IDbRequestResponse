// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace TTRider.Data.RequestResponse
{
    public class DbRequest : IDbRequest, ILoggerFactory
    {
        public ILoggerFactory loggerFactory;

        public static IDbRequest Create(IDbCommand command, DbRequestMode? mode = DbRequestMode.NoBufferReuseMemory, IEnumerable<IDbCommand> prerequisiteStatements = null, ILoggerFactory loggerFactory = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            return new DbRequest()
            {
                Command = command,
                Mode = mode.GetValueOrDefault(),
                PrerequisiteCommands = prerequisiteStatements??new List<IDbCommand>(),
                loggerFactory = loggerFactory
            };
        }

        public IEnumerable<IDbCommand> PrerequisiteCommands { get; private set; }
        public DbRequestMode Mode { get; private set; }
        public IDbCommand Command { get; private set; }
        public IDbResponse GetResponse()
        {
            foreach (var dbCommand in PrerequisiteCommands)
            {
                dbCommand.Connection = Command.Connection;
            }

            return DbResponse.GetResponse(this);
        }

        public Task<IDbResponse> GetResponseAsync()
        {
            foreach (var dbCommand in PrerequisiteCommands)
            {
                dbCommand.Connection = Command.Connection;
            }
            return DbResponse.GetResponseAsync(this);
        }

        #region Implementation of IDisposable

        void IDisposable.Dispose()
        {
            this.loggerFactory.Dispose();
        }

        #endregion

        #region Implementation of ILoggerFactory

        ILogger ILoggerFactory.CreateLogger(string categoryName)
        {
            if (this.loggerFactory==null) return NullLogger.Instance;
            return this.loggerFactory.CreateLogger(categoryName);
        }

        void ILoggerFactory.AddProvider(ILoggerProvider provider)
        {
            this.loggerFactory?.AddProvider(provider);
        }

        #endregion
    }
}
