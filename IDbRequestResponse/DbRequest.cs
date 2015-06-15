using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace TTRider.Data.RequestResponse
{
    public class DbRequest : IDbRequest
    {
        public static IDbRequest Create(IDbCommand command, DbRequestMode? mode = DbRequestMode.NoBufferReuseMemory, IEnumerable<IDbCommand> prerequisiteStatements = null)
        {
            if (command == null) throw new ArgumentNullException("command");

            return new DbRequest()
            {
                Command = command,
                Mode = mode.GetValueOrDefault(),
                PrerequisiteCommands = prerequisiteStatements??new List<IDbCommand>()
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
    }
}
