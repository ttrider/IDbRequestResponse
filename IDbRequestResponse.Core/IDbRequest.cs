// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace TTRider.Data.RequestResponse
{
    public interface IDbRequest
    {
        IEnumerable<IDbCommand> PrerequisiteCommands { get; }

        /// <summary>
        /// buffering mode 
        /// </summary>
        DbRequestMode Mode { get; }

        /// <summary>
        /// main statement
        /// </summary>
        IDbCommand Command { get; }


        IDbResponse GetResponse();
        Task<IDbResponse> GetResponseAsync();
    }
}
