// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System;

namespace TTRider.Data.RequestResponse
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string sessionHash, string message)
        {
            this.SessionHash = sessionHash;
            this.Message = message;
        }
        public string Message { get; private set; }
        public string SessionHash { get; private set; }
    }
}