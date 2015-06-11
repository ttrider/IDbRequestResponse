// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2015 All Rights Reserved
// </copyright>

using System.Collections.Generic;

namespace TTRider.Data.RequestResponse
{
    public interface IDbResponse
    {
        IDbRequest Request { get; }

        IEnumerable<object[]> Records { get; }

        IDictionary<string, object> Output { get; }

        int? ReturnCode { get; }

        bool HasMoreData { get; }
    }
}
