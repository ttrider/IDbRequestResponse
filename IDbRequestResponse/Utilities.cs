// <license>
// The MIT License (MIT)
// </license>
// <copyright company="TTRider, L.L.C.">
// Copyright (c) 2014-2016 All Rights Reserved
// </copyright>

using System.Data;
using System.Linq;
using System.Text;

namespace TTRider.Data.RequestResponse
{
    public static class Utilities
    {
        public static string GetCommandSummary(this IDbCommand command)
        {
            if (command == null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var p in command.Parameters.Cast<IDbDataParameter>())
            {
                sb.AppendFormat("-- DECLARE {0} ", p.ParameterName);

                switch (p.DbType)
                {
                    case DbType.VarNumeric: sb.AppendFormat("NUMERIC({1},{2}) = {0}", p.Value, p.Precision, p.Scale); break;
                    case DbType.UInt64:
                    case DbType.Int64: sb.AppendFormat("BIGINT = {0}", p.Value); break;
                    case DbType.Binary: sb.AppendFormat("BINARY({1}) = '{0}'", p.Value, p.Size); break;
                    case DbType.Boolean: sb.AppendFormat("BIT = {0}", p.Value); break;
                    case DbType.AnsiStringFixedLength: sb.AppendFormat("CHAR({1}) = N'{0}'", p.Value, p.Size); break;
                    case DbType.DateTime: sb.AppendFormat("DATETIME = N'{0}'", p.Value); break;
                    case DbType.Decimal: sb.AppendFormat("DECIMAL({1},{2}) = {0}", p.Value, p.Precision, p.Scale); break;
                    case DbType.Single: sb.AppendFormat("FLOAT = {0}", p.Value); break;
                    case DbType.UInt32:
                    case DbType.Int32: sb.AppendFormat("INT = {0}", p.Value); break;
                    case DbType.Currency: sb.AppendFormat("MONEY = {0}", p.Value); break;
                    case DbType.StringFixedLength:
                    case DbType.AnsiString: sb.AppendFormat("NCHAR({1}) = N'{0}'", p.Value, p.Size); break;
                    case DbType.String: sb.AppendFormat("NVARCHAR({1}) = N'{0}'", p.Value, (p.Size == -1) ? "MAX" : p.Size.ToString()); break;
                    case DbType.Double: sb.AppendFormat("REAL = {0}", p.Value); break;
                    case DbType.Guid: sb.AppendFormat("UNIQUEIDENTIFIER = N'{0}'", p.Value); break;
                    case DbType.UInt16:
                    case DbType.Int16: sb.AppendFormat("SMALLINT = {0}", p.Value); break;
                    case DbType.Byte:
                    case DbType.SByte: sb.AppendFormat("TINYINT = {0}", p.Value); break;
                    case DbType.Xml: sb.AppendFormat("XML = N'{0}'", p.Value); break;
                    case DbType.Date: sb.AppendFormat("DATE = N'{0}'", p.Value); break;
                    case DbType.Time: sb.AppendFormat("TIME = N'{0}'", p.Value); break;
                    case DbType.DateTime2: sb.AppendFormat("DATETIME2({1}) = N'{0}'", p.Value, p.Size); break;
                    case DbType.DateTimeOffset: sb.AppendFormat("DATETIMEOFFSET({1}) = N'{0}'", p.Value, p.Size); break;
                }
                sb.AppendLine();
            }
            sb.AppendLine(command.CommandText);
            return sb.ToString();
        }
    }
}
