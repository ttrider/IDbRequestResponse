// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System;

namespace TTRider.Data.RequestResponse
{
    internal interface IDbRequestLogging
    {
        void OnPrerequisiteCommandExecuting(string sessionId, string message);
        void OnCommandExecuting(string sessionId, string message);
        void OnCommandExecuted(string sessionId);
    }
}