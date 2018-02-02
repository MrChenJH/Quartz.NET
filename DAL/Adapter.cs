using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Fy.Framework.Data;

namespace Fy.Framework.Adapter
{
    internal abstract class Adapter
    {
        internal Adapter()
        {
            AdapterState = AdapterState.UnCheck;
        }

        #region 基本属性

        /// <summary>
        /// DB CONNCTION STRING.
        /// </summary>
        internal string ConnectionStr { get; set; }

        /// <summary>
        /// DB CURRENT STATUS
        /// </summary>
        internal AdapterState AdapterState { get; set; }

        #endregion.

        #region 基本方法

        /// <summary>
        /// 置换DB CONNECTION 字符串
        /// </summary>
        internal virtual void ChangeDbConnnection(string _dbConnectionStr)
        {
           AdapterState= AdapterState.UnCheck;
           ConnectionStr = _dbConnectionStr;
        }

        /// <summary>
        /// 检测DB CONNECTION.
        /// </summary>
        internal abstract void CheckConnectionStr();

        /// <summary>
        /// 键入执行语句
        /// </summary>
        internal abstract void SetCommandText(string cmdText, Dictionary<string, object> parameters);

        /// <summary>
        /// 键入执行语句
        /// </summary>
        internal abstract void SetCommandText(string cmdText, CommandType cmdType, Dictionary<string, object> parameters);

        /// <summary>
        /// 读取返回值的第一行第一列，其他全部忽略。
        /// </summary>
        internal abstract object ExecuteScalar();

        /// <summary>
        /// 执行无返回值方法
        /// </summary>
        internal abstract void ExecuteNonQuery();

        /// <summary>
        /// 读取全部数据集合
        /// </summary>
        internal abstract RecordCollection ExecuteReader();

        /// <summary>
        /// 读取带偏移量集合
        /// </summary>
        /// <param name="offset">偏移量集合</param>
        /// <param name="limit">最大记录集合数</param>
        internal abstract RecordCollection ExecuteReader(int offset, int limit);

        #endregion.
    }
}
