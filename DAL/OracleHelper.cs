using Oracle.DataAccess.Client;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIDAO.DAL
{
    public class OracleHelper
    {
        //链接字符串
        private static readonly string connStr =  ConfigurationManager.ConnectionStrings["dbconnStr"].ConnectionString;

        /// <summary>
        /// 创建链接
        /// </summary>
        /// <returns>链接</returns>
        public static OracleConnection CreateConnection()
        {
            OracleConnection conn = new OracleConnection(connStr);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// 使用亦有链接的 非查询
        /// </summary>
        /// <param name="conn">链接</param>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>受影响行数</returns>
        public static int ExecuteNonQuery(OracleConnection conn,string sql,params OracleParameter[] parameters)
        { 
       
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 自己创建链接的 非查询
        /// </summary>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>受影响行数</returns>
        public static int ExecuteNonQuery(string sql, params OracleParameter[] parameters)
        {
            using (OracleConnection conn = CreateConnection())
            {
                return ExecuteNonQuery(conn, sql, parameters);
            }
        }

        /// <summary>
        /// 使用已有链接的 带存储过程的Insert非查询，直接写存储过程参数
        /// </summary>
        /// <param name="conn">已有链接</param>
        /// <param name="proName">存储过程名称</param>
        /// <param name="strInsertSQL">执行插入的sql语句，或者其他操作sql语句</param>
        /// <param name="seqName">序列的名称</param>
        /// <returns>当前序列号，即ID</returns>
        public static object ExecuteNonQueryWithProduce(OracleConnection conn, string proName, string strInsertSQL, string seqName)
        {
            using (OracleCommand cmd = new OracleCommand(proName, conn)) //命令中执行的不在是sql，而是存储过程
            {
                try
                {
                    cmd.CommandType = CommandType.StoredProcedure; //标记该命令的类型不是sql，而是存储过程
                    //存储过程中有参数名称，以及设置对应参数的值
                    cmd.Parameters.Add(new OracleParameter("strInsertSQL",strInsertSQL )); ////存储过程中的参入参数 strInsertSQL
                    cmd.Parameters.Add(new OracleParameter("seqName", seqName )); // //存储过程中的传入参数 seqName
                    cmd.Parameters.Add(new OracleParameter("ID", OracleDbType.Int64) { Direction = ParameterDirection.Output }); //存储过程中的传出参数ID，只需要声明
                    //cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                    string newId = cmd.Parameters["ID"].Value.ToString(); //获得传出参数的ID的值
                    return newId;
                }
                catch(Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 自己创建链接的 带存储过程的Insert非查询，直接写存储过程参数
        /// </summary>
        /// <param name="proName">存储过程名称</param>
        /// <param name="strInsertSQL">执行插入的sql语句，或者其他操作sql语句</param>
        /// <param name="seqName">序列的名称</param>
        /// <returns>当前序列号，即ID</returns>
        public static object ExecuteNonQueryWithProduce(string proName, string strInsertSQL, string seqName)
        {
            using (OracleConnection conn = CreateConnection())
            {
                return ExecuteNonQueryWithProduce(conn, proName, strInsertSQL, seqName);
            }
        }

        /// <summary>
        /// 使用已有链接的 带存储过程的Insert非查询，传存储过程参数
        /// </summary>
        /// <param name="conn">已有链接</param>
        /// <param name="proName">存储过程名称</param>
        /// <param name="parameters">存储过程中的传入、传出参数 数组</param>
        /// <returns>当前序列号，即ID</returns>
        public static object ExecuteNonQueryWithProduce(OracleConnection conn, string proName, params OracleParameter[] parameters)
        {
            using (OracleCommand cmd = new OracleCommand(proName, conn)) //命令中执行的不在是sql，而是存储过程
            {
                try
                {
                    cmd.CommandType = CommandType.StoredProcedure; //标记该命令的类型不是sql，而是存储过程
                    ////存储过程中有参数名称，以及设置对应参数的值
                    //cmd.Parameters.Add(new OracleParameter("strInsertSQL", OracleDbType.Varchar2) { Value = strInsertSQL }); ////存储过程中的参入参数 strInsertSQL
                    //cmd.Parameters.Add(new OracleParameter("seqName", OracleDbType.Varchar2) { Value = seqName }); // //存储过程中的传入参数 seqName
                    //cmd.Parameters.Add(new OracleParameter("ID", OracleDbType.Int32) { Direction = ParameterDirection.Output }); //存储过程中的传出参数ID，只需要声明
                    cmd.Parameters.AddRange(parameters); //参数中包括存储过程的传入传出参数，以及子sql语句中的参数    --------------****-----------------
                    int i = cmd.ExecuteNonQuery(); //直接返回执行插入之后，存储过程传出的变量值
                    string newId = cmd.Parameters["ID"].Value.ToString(); //获得传出参数的ID的值
                    return newId;
                }
               catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 自己创建链接的 带存储过程的Insert非查询，传存储过程参数
        /// </summary>
        /// <param name="proName">存储过程名称</param>
        /// <param name="parameters">存储过程中的传入、传出参数 数组</param>
        /// <returns>当前序列号，即ID</returns>
        public static object ExecuteNonQueryWithProduce(string proName,params OracleParameter[] parameters)
        {
            using (OracleConnection conn = CreateConnection())
            {
                return ExecuteNonQueryWithProduce(conn, proName, parameters);
            }
        }
        

        /// <summary>
        /// 使用已有链接的 单查询
        /// </summary>
        /// <param name="conn">链接</param>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>查询到的一条结果</returns>
        public static object ExecuteScalar(OracleConnection conn,string sql,params OracleParameter[] parameters)
        { 
            using(OracleCommand cmd=new OracleCommand(sql,conn))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// 自己创建链接的 单查询
        /// </summary>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>查询到的一条结果</returns>
        public static object ExecuteScalar(string sql,params OracleParameter[] parameters)
        { 
            using(OracleConnection conn=CreateConnection())
            {
                return ExecuteScalar(conn, sql, parameters);
            }
        }

        /// <summary>
        /// 使用已有链接的 reader查询
        /// </summary>
        /// <param name="conn">链接</param>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>查询到的结果集table</returns>
        public static DataTable ExecuteReader(OracleConnection conn,string sql,params OracleParameter[] parameters)
        {
            DataTable table = new DataTable();
            using(OracleCommand cmd=new OracleCommand(sql,conn))
            {
                cmd.Parameters.AddRange(parameters);
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    table.Load(reader);
                }
            }
            return table;
        }

        /// <summary>
        /// 自己创建链接的 reader查询
        /// </summary>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>查询到的结果集table</returns>
        public static DataTable ExecuteReader(string sql,params OracleParameter[] parameters)
        { 
            using(OracleConnection conn=CreateConnection())
            {
                return ExecuteReader(conn, sql, parameters);
            }
        }

        /// <summary>
        /// 使用已有链接的 stream查询
        /// </summary>
        /// <param name="conn">链接</param>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>查询到的结果流stream</returns>
        public static System.IO.Stream ExecuteStream(OracleConnection conn,string sql,params OracleParameter[] parameters)
        { 
            using(OracleCommand cmd=new OracleCommand(sql,conn))
            {
                cmd.Parameters.AddRange(parameters);
                using (System.IO.Stream stream = cmd.ExecuteStream())
                {
                    return stream;
                }
            }
        }

        /// <summary>
        /// 自己创建链接的stream查询
        /// </summary>
        /// <param name="sql">sql文本</param>
        /// <param name="parameters">sql参数</param>
        /// <returns>查询到的结果流stream</returns>
        public static System.IO.Stream ExecuteStream(string sql, params OracleParameter[] parameters)
        {
            using(OracleConnection conn=CreateConnection())
            {
                return ExecuteStream(conn, sql, parameters);
            }
        }
    }
}

