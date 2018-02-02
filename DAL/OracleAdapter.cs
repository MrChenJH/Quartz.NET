using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.OracleClient;
using System.Reflection;
using Fy.Framework.Data;

namespace Fy.Framework.Adapter
{
    /// <summary>
    /// ORACLE 执行SQL 语句类
    /// </summary>
    internal class OracleAdapter: Adapter
    {
        internal OracleAdapter()
            : base()
        {
        }

        private string  CommandText { get; set; }
        private CommandType CommandType { get; set; }

        private List<OracleParameter> Parameters { get; set; }

        internal override void CheckConnectionStr()
        {
            if (string.IsNullOrEmpty(base.ConnectionStr))
                throw new ArgumentNullException("ConnectionStr");

            if (AdapterState == AdapterState.UnCheck)
            {
                using (OracleConnection oracleCn = new OracleConnection(base.ConnectionStr))
                {
                    oracleCn.Open();
                    AdapterState = AdapterState.Checked;
                    oracleCn.Close();
                    oracleCn.Dispose();
                }
            }
        }

        internal override void SetCommandText(string cmdText, Dictionary<string, object> parameters)
        {
            SetCommandText(cmdText, System.Data.CommandType.Text, parameters);
        }

        internal override void SetCommandText(string cmdText, CommandType cmdType, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(cmdText))
                throw new ArgumentNullException("cmdText");

            if (!string.IsNullOrEmpty(CommandText))
                Parameters = null;

            this.CommandText = cmdText;
            this.CommandType = cmdType;

            if (parameters != null)
            {
                Parameters = new List<OracleParameter>();

                parameters.ToList().ForEach(property =>
                {
                    Parameters.Add(new OracleParameter(string.Format(":{0}", property.Key), property.Value));
                });
            }
        }

        internal override void ExecuteNonQuery()
        {
            if (string.IsNullOrEmpty(CommandText))
                throw new ArgumentNullException("CommandText");

            AdapterState = AdapterState.Executing;

            CheckConnectionStr();

            using (OracleConnection oracleCn = new OracleConnection(base.ConnectionStr))
            {
                oracleCn.Open();

                OracleCommand oracleCmd = new OracleCommand(CommandText, oracleCn);
                oracleCmd.CommandType = CommandType;

                if (Parameters != null)
                {
                    oracleCmd.Parameters.AddRange(Parameters.ToArray());
                }

                oracleCmd.ExecuteNonQuery();

                oracleCmd.Cancel();
                oracleCn.Close();
                oracleCmd.Dispose();
                oracleCn.Dispose();
            }

            AdapterState = AdapterState.Waiting;
        }

        internal override object ExecuteScalar()
        {
            if (string.IsNullOrEmpty(CommandText))
                throw new ArgumentNullException("CommandText");

            AdapterState = AdapterState.Fetching;

            CheckConnectionStr();

            using (OracleConnection oracleCn = new OracleConnection(base.ConnectionStr))
            {
                oracleCn.Open();

                OracleCommand oracleCmd = new OracleCommand(CommandText, oracleCn);
                oracleCmd.CommandType = CommandType;

                if (Parameters != null)
                {
                    oracleCmd.Parameters.AddRange(Parameters.ToArray());
                }

                AdapterState = AdapterState.Waiting;

                object value = oracleCmd.ExecuteScalar();

                oracleCmd.Cancel();
                oracleCn.Close();
                oracleCmd.Dispose();
                oracleCn.Dispose();

                return value;
            }
        }

        internal override RecordCollection ExecuteReader()
        {
           return ExecuteReader(0, Int32.MaxValue);
        }

        internal override RecordCollection ExecuteReader(int offset, int limit)
        {
            if (string.IsNullOrEmpty(CommandText))
                throw new ArgumentNullException("CommandText");

            AdapterState = AdapterState.Fetching;

            CheckConnectionStr();

            using (OracleConnection oracleCn = new OracleConnection(base.ConnectionStr))
            {
                oracleCn.Open();

                OracleCommand oracleCmd = new OracleCommand(CommandText, oracleCn);
                oracleCmd.CommandType = CommandType;

                if (Parameters != null)
                {
                    oracleCmd.Parameters.AddRange(Parameters.ToArray());
                }

                OracleDataReader oracleReader = oracleCmd.ExecuteReader(CommandBehavior.CloseConnection);

                RecordCollection records = new RecordCollection();

                //获取字段信息集
                for (int i = 0; i < oracleReader.FieldCount; i++)
                {
                    records.Columns.Add(oracleReader.GetName(i), oracleReader.GetFieldType(i), i);
                }

                // 开始读取信息
                int move = 0;
                while (oracleReader.Read())
                {
                    if (limit <= 0) break;

                    if (move >= offset)
                    {
                        if (move < (offset + limit))
                        {
                            Record newRecord = records.NewRecord();

                            for (int i = 0; i < newRecord.Count; i++)
                            {
                                try
                                {
                                    newRecord[i] = oracleReader.GetValue(newRecord.Columns[i].ReadIdx); 
                                }
                                catch
                                {
                                    newRecord[i] = default(object);
                                }
                                
                            }

                            records.Add(newRecord);
                        }
                        else
                            break;
                    }
                    move++;
                }
                oracleCmd.Cancel();
                oracleReader.Close();
                oracleReader.Dispose();
                oracleCmd.Dispose();
                oracleCn.Dispose();

                AdapterState = AdapterState.Waiting;

                return records;
            }
        }
    }
}
