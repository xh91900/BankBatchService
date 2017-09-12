using Dapper;
using Oracle.DataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// OracleDBAccess
    /// </summary>
    public class OracleDBAccess : IDisposable
    {
        //属性定义
        public const string SQLFMT_DATE = "yyyy-MM-dd";         //统一时间字符串格式
        public const string SQLFMT_DATE_TIME = "yyyy-MM-dd HH:mm:ss";//统一时间字符串格式
        public const string SQLFMT_DATE_TIMESTAMP = "yyyy-MM-dd HH:mi:ss.ffffff";//oracle中带毫秒时间格式

        //属性定义
        public const string SqlfmtDateTime = "yyyy-MM-dd HH:mm:ss";//统一时间字符串格式
        public const string SqlfmtDateTimePlusT = "yyyy-MM-ddTHH:mm:ss";//统一时间字符串格式
        public const string SqlfmtDate = "yyyy-MM-dd";
        public const string SqlfmtTime = "HH:mm:ss";
        public const string DatefmtYyyymmdd = "yyyyMMdd";

        private OracleConnection myConnection;  //数据库连接对象
        private OracleCommand myCommand;        //数据库命令对象
        private OracleTransaction myTrans;      //数据库事务对象
        private OracleDataReader myReader;      //数据读取器对象

        private string _savedSQL;
        public string CommandText
        {
            get { return _savedSQL; }
            set { _savedSQL = value == null ? "" : value; }
        }

        public IDbCommand dbCommand
        {
            get
            {
                return myCommand;
            }
            set
            {
                myCommand = value == null ? new OracleCommand() : (OracleCommand)value;
            }
        }

        #region sql语句处理静态方法

        /// <summary>
        /// 给一个字符串加上数据库相关的引号
        /// </summary>
        /// <param name="str">未加引号前的字符串</param>
        /// <returns>加引号后的字符串</returns>
        public static string GetSingleQuote(string str)
        {
            if (str == null)
            {
                return "''";
            }
            return "'" + str + "'";
        }

        /// <summary>
        /// 将指定的时刻转换为SQL标准的日期字符串（含引号，时分秒部分截断）
        /// </summary>
        /// <param name="dt1">输入的时刻</param>
        /// <returns>对应的日期字符串</returns>
        public static string StringDate(DateTime dt1)
        {
            return GetSingleQuote(dt1.ToString(SQLFMT_DATE));
        }

        /// <summary>
        /// 将指定的时刻转换为SQL标准的日期时间字符串（含引号）
        /// </summary>
        /// <param name="dt1">输入的时刻</param>
        /// <returns>对应的日期时间字符串</returns>
        //作废：用ToDbDatetime(DateTime dt1)代替 by lhy
        //public static string StrDateTime(DateTime dt1)
        //{
        //    return GetSingleQuote(dt1.ToString(SQLFMT_DATE_TIME));
        //}

        /// <summary>
        /// 将数据对象转换为数据库所识别的字符串，null/undefined等效为NULL
        /// </summary>
        /// <param name="obj">待转换的字符串</param>
        /// <returns>准许为NULL的数据库值的字符串表示</returns>
        public static string DBString(object obj)
        {
            return obj == null ? "NULL" : obj.ToString();
        }

        /// <summary>
        /// 将数据对象转换为带单引号的字符串，null/undefined等效为NULL
        /// </summary>
        /// <param name="str">待转换的字符串</param>
        /// <returns>准许为NULL的数据库值的带单引号字符串表示</returns>
        public static string DBStringWithSingleQuote(string str)
        {
            return str == null ? "NULL" : GetSingleQuote(str.ToString().Trim());
        }


        /// <summary>
        /// 将指定的时间戳字段的日期部分查询，解析为时间段查询，以运用索引
        /// </summary>
        /// <param name="fieldName">时间戳字段名</param>
        /// <param name="date1">日期</param>
        /// <returns>查询条件</returns>
        public static string DateToTimestampRange(string fieldName, DateTime date1)
        {
            return " " + fieldName + " >= to_timestamp('" + date1.ToString(SQLFMT_DATE) + " 00:00:00.000000','yyyy-mm-dd hh24:mi:ss.ff') AND "
                + fieldName + " <=  to_timestamp('" + date1.ToString(SQLFMT_DATE) + " 23:59:59.999999','yyyy-mm-dd hh24:mi:ss.ff') ";
        }

        public static string ToTimestampFromStartDate(DateTime date0)
        {
            return "to_timestamp('" + date0.ToString(SQLFMT_DATE) + " 00:00:00.000000','yyyy-mm-dd hh24:mi:ss.ff')";
        }

        public static string ToTimestampFromEndDate(DateTime date1)
        {
            return "to_timestamp('" + date1.ToString(SQLFMT_DATE) + " 23:59:59.999999','yyyy-mm-dd hh24:mi:ss.ff')";
        }

        /// <summary>
        /// 根据SQL查询语句生成查询结果统计语句
        /// </summary>
        /// <param name="selectStr">SQL查询语句</param>
        /// <returns>SQL查询结果统计语句</returns>
        public static string SelectResultCounting(string selectStr)
        {
            if (selectStr == null || selectStr == "")
            {
                return null;
            }
            string countStr = "SELECT COUNT(*) FROM (";
            string temp = selectStr.ToUpper();
            int Pos = -1;
            if ((Pos = temp.IndexOf("FROM ")) < 0)
            {
                return null;
            }
            if ((temp = temp.Substring(0, Pos)) == null)
            {
                return null;
            }
            if ((Pos = temp.IndexOf("SELECT ")) < 0)
            {
                return null;
            }
            countStr += selectStr;
            if ((Pos = countStr.IndexOf(";")) < 0)
            {
                countStr += ")";
            }
            else
            {
                countStr = countStr.Substring(0, Pos) + ")";
            }
            return countStr;
        }

        public static string ToDbDatetime(DateTime dtime, string dateType)
        {
            if (dateType == SQLFMT_DATE_TIME)
                return string.Format(" to_date('{0}','yyyy-mm-dd hh24:mi:ss')", dtime.ToString(dateType));
            else if (dateType == SQLFMT_DATE)
                return string.Format(" to_date('{0}','yyyy-mm-dd')", dtime.ToString(dateType));
            else if (dateType == SQLFMT_DATE_TIMESTAMP)
                return string.Format("to_timestamp('{0}','yyyy-mm-dd hh24:mi:ss.ff')", dtime.ToString(dateType));
            else
                return "";
        }
        public static string ToDbDatetime(string datestr)
        {
            return string.Format(" to_date('{0}','yyyy-mm-dd hh24:mi:ss')", datestr);
        }

        //替换DBAccessBase.ToDbDatetime(DateTime dt1) ORACLE匹配格式
        public static string ToDbDatetime(DateTime dtime)
        {
            return string.Format(" to_date('{0}','yyyy-mm-dd hh24:mi:ss')", dtime.ToString(SQLFMT_DATE_TIME));
        }

        /// <summary>
        /// 根据SQL查询语句生成对应限制结果数量查询语句
        /// </summary>
        /// <param name="selectStr">SQL查询语句</param>
        /// <param name="limit">限制结果数量</param>
        /// <returns>SQL限制结果数量查询语句</returns>
        public static string LimitSelectResult(string selectStr, int limit)
        {
            if (selectStr == null || selectStr == "" || limit <= 0)
            {
                return null;
            }
            string countStr = "";
            string temp = selectStr.ToUpper();
            int Pos = -1;
            if ((Pos = temp.IndexOf("FROM ")) < 0)
            {
                return null;
            }
            if ((temp = temp.Substring(0, Pos)) == null)
            {
                return null;
            }
            if ((Pos = temp.IndexOf("SELECT ")) < 0)
            {
                return null;
            }
            countStr += selectStr;
            if ((Pos = countStr.IndexOf(";")) < 0)
            {
                countStr += " ";
            }
            else
            {
                countStr = countStr.Substring(0, Pos) + " ";
            }
            //countStr += " FETCH FIRST " + limit + " ROWS ONLY;";
            if (countStr.IndexOf("WHERE") < 0)
            {
                countStr += "WHERE ROWNUM<=" + limit;
            }
            else
            {
                countStr += "AND ROWNUM<=" + limit;
            }
            return countStr;
        }

        /// <summary>
        /// 取排序后的第一条记录
        /// </summary>
        /// <param name="inputs">查询字段</param>
        /// <param name="sql">子查询语句</param>
        /// <returns></returns>
        public static string GetFirstRowSqlString(string inputs, string sql)
        {
            return string.Format("select {0} from ({1}) where rownum=1", inputs, sql);
        }

        #endregion

        /// <summary>
        /// Oracle数据库操作类 构造函数：建立数据库连接对象和执行操作的上下文环境
        /// </summary>
        public OracleDBAccess()
        {
            try
            {
                myConnection = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["oracleConnString"].ToString());

                myConnection.Open();
                myCommand = new OracleCommand();
                myCommand.Connection = myConnection;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public OracleDBAccess(string userID, string password)
        {
            OracleConnectionStringBuilder sb = new OracleConnectionStringBuilder(System.Configuration.ConfigurationManager.ConnectionStrings["oracleConnString"].ToString());
            sb.UserID = userID;
            sb.Password = password;
            myConnection = new OracleConnection(sb.ConnectionString);
            myConnection.Open();
            myCommand = new OracleCommand();
            myCommand.Connection = myConnection;
        }

        /// <summary>
        /// 析构函数,强制回收垃圾单元	
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// 析构函数,结束前执行的处理
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            try
            {
                if (myConnection.State == ConnectionState.Open)
                {
                    myConnection.Close();
                }
            }
            catch (Exception err)
            {
                //UNRESOLVED! how to report the error?
                Console.WriteLine("关闭数据库连接出现异常!\r\n " + err.Message);
            }
        }

        /// <summary>
        /// 开启数据库事务
        /// </summary>
        public void BeginTransaction()
        {
            try
            {
                //之所以使用较低的隔离级别ReadCommitted，是为了减少死锁和提高并发性：
                //通过update原子操作，在table_operationlog上加记录锁，足可有效隔离事务
                myTrans = myConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                myCommand.Transaction = myTrans;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 事务数据库提交
        /// </summary>
        public void CommitTransaction()
        {
            try
            {
                myTrans.Commit();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 数据库事务回滚
        /// </summary>
        public void RollbackTransaction()
        {
            try
            {
                if (myReader != null && !myReader.IsClosed)
                    myReader.Close();
                myCommand.Cancel(); // if any pending
                myTrans.Rollback();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 取消正在执行的命令（例如，一个复杂的或返回大数据集的查询）
        /// 如果没有需要取消的内容，那么不会发生任何情况。
        /// 但是，如果在执行命令并且尝试取消时失败，那么将不会生成异常。
        /// </summary>
        public void CancelCommand()
        {
            if (myReader != null && !myReader.IsClosed)
                myReader.Close();
            // Just cancel command would not close the reader.
            // Cancel command does not close the reader, so next ExecuteNonQuery will fail.
            if (myCommand != null)
                myCommand.Cancel();
        }

        /// <summary>
        /// 执行ORACLE数据库非查询语句
        /// </summary>
        /// <param name="strCmd">待执行的数据库命令，应为U,A,D而非R操作</param>
        /// <returns>返回值为1，或者抛出异常</returns>
        public int ExecuteNonQuery(string strCmd)
        {
            myCommand.CommandText = _savedSQL = strCmd;
            return myCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// 按给定的固定字符串查询
        /// </summary>
        /// <param name="strCmd">查询语句</param>
        /// <returns>实现IDataReader接口的数据序列</returns>
        public IDataReader ExecuteReader(string strCmd)
        {
            myCommand.CommandText = _savedSQL = strCmd;
            myReader = myCommand.ExecuteReader();
            return myReader;
        }

        /// <summary>
        /// 执行只返回一个字段的查询
        /// </summary>
        /// <param name="strCmd">查询语句</param>
        /// <returns>一个数据库值对象</returns>
        public object ExecuteScalar(string strCmd)
        {
            myCommand.CommandText = _savedSQL = strCmd;
            return myCommand.ExecuteScalar();
        }

        /// <summary>
        /// 执行数据库查询语句，返回一个标量对象
        /// </summary>
        /// <param name="strCmd">所要执行的查询语句</param>
        /// <param name="result">出口参数，所查询的第一条（通常为唯一一条）记录的第一个字段（应为唯一字段）的值</param>
        /// <returns>true如果查到一条记录，false如果未找到或字段值为NULL</returns>
        public bool GetScalar(string strCmd, ref Object result)
        {
            OracleCommand rCommand = new OracleCommand(strCmd, myConnection);
            result = rCommand.ExecuteScalar();
            return (result != null);
        }


        /// <summary>
        /// 准备执行参数化的SQL语句
        /// </summary>
        /// <param name="strCmd">带参数的SQL命令</param>
        public void PrepareSQL(string strCmd)
        {
            myCommand.CommandText = strCmd;
            myCommand.CommandType = CommandType.Text;
            myCommand.Parameters.Clear();
        }

        /// <summary>
        /// 准备执行参数化的SQL语句
        /// </summary>
        /// <param name="strCmd">带参数的SQL命令</param>
        public void PrepareSQL(string strCmd, CommandType commandtype)
        {
            myCommand.CommandText = strCmd;
            myCommand.CommandType = commandtype;
            myCommand.Parameters.Clear();
        }

        /// <summary>
        /// 给准备执行的语句增加参数
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <param name="paramType">参数类型</param>
        /// <param name="value">参数的值</param>
        /// <param name="sizes">参数大小，可选</param>
        public void AddParameterToDbCommand(string paramName, OracleDbType paramType, object value, params int[] sizes)
        {
            try
            {
                OracleParameter param = new OracleParameter(paramName, paramType);
                if (sizes.Length > 0 && sizes[0] > 0)
                {
                    param.Size = sizes[0];
                }
                //if (paramType == DbType.Date)//ORACLE数据库只有日期时间类型
                //{
                //    if (value is DateTime)
                //        param.Value = DB2Date.Parse(((DateTime)value).ToString("yyyy-MM-dd"));
                //    else
                //        throw new Exception("Invalid parameter");
                //}
                //else
                //{
                // }
                //if (myCommand.CommandType == CommandType.StoredProcedure)
                //{
                //    param.Direction = ParameterDirection.Input;
                //}
                param.Value = value;

                myCommand.Parameters.Add(param);
            }
            catch (Exception e)
            {
                
            }
        }

        public void AddParameterToDbCommand(string name, ParameterDirection parameterDirection, DbType paramType, Nullable<int> size, object value)
        {
            OracleParameter parameter = new OracleParameter(name, paramType);

            if (size.HasValue)
                parameter.Size = (int)size;

            parameter.Value = value;
            parameter.Direction = parameterDirection;
            myCommand.Parameters.Add(parameter);
        }
        public void AddOutParameterToDbCommand(string paramName
                                 , OracleDbType paramType
                                 , params int[] sizes)
        {
            OracleParameter param = new OracleParameter(paramName, paramType);
            if (sizes.Length > 0 && sizes[0] > 0)
                param.Size = sizes[0];
            param.Direction = ParameterDirection.Output;
            myCommand.Parameters.Add(param);
        }

        public object GetParameterToDbCommand(string paramName)
        {
            object result = myCommand.Parameters[paramName].Value.ToString();
            return result;
        }

        /// <summary>
        /// 根据可选的命令行为模式选项，返回只读数据序列
        /// </summary>
        /// <param name="cbs"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(params CommandBehavior[] cbs)
        {
            try
            {
                myCommand.CommandText = _savedSQL = CommandText;
                myReader = cbs.Length > 0 ? myCommand.ExecuteReader(cbs[0]) : myCommand.ExecuteReader();
                return myReader;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 执行只返回一个字段的查询
        /// </summary>
        /// <returns>一个数据库值对象</returns>
        public object ExecuteScalar()
        {
            myCommand.CommandText = _savedSQL = CommandText;
            return myCommand.ExecuteScalar();
        }

        /// <summary>
        /// 执行ORACLE数据库非查询语句
        /// </summary>
        /// <returns>返回值为1，或者抛出异常</returns>
        public int ExecuteNonQuery()
        {
            myCommand.CommandText = _savedSQL = CommandText;
            return myCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// 获得指定数据表的数据集版本号（从行记录的最大版本号中所得）
        /// </summary>
        /// <param name="tableName">有version字段的数据表名称</param>
        /// <returns>下一个数据集版本号，为"1"如果尚无数据行记录</returns>
        /// <remarks>有可能抛出数据库异常</remarks>
        public string NextDatasetVersion(string tableName)
        {
            _savedSQL = myCommand.CommandText = "SELECT MAX(VERSION) + 1 FROM " + tableName;
            //
            object result = myCommand.ExecuteScalar();
            if (result == null || result.ToString().Equals(String.Empty))
                return "1";
            return result.ToString();
        }

        /// <summary>
        /// dapper按条件查询
        /// </summary>
        /// <typeparam name="T">返回集合类型</typeparam>
        /// <param name="sql">sql</param>
        /// <param name="param">参数</param>
        /// <returns>List<T></returns>
        public List<T> QuerySql<T>(string sql,object param)
        {
            try
            {
                return myConnection.Query<T>(sql, param).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 关联查询
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="sql"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public List<TReturn> QueryMultiple<TFirst,TSecond,TReturn>(string sql,Func<TFirst, TSecond, TReturn> func)
        {
            return myConnection.Query<TFirst, TSecond, TReturn>(sql, func, null, null, true, splitOn: "ID").ToList();
        }

        /// <summary>
        /// dapper执行非查询语句
        /// </summary>
        /// <param name="sql">sql</param>
        /// <param name="param">参数</param>
        /// <returns>int</returns>
        public int ExecuteSql(string sql, object param)
        {
            try
            {
                return myConnection.Execute(sql, param, myTrans);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 处理OracleException
        /// </summary>
        /// <param name="ex">Exception 实例</param>
        /// <returns>ReturnResult</returns>
        public ReturnResult HandleOracleException(Exception ex)
        {
            bool isOracleException = ex is OracleException;
            if (!isOracleException)
                return (int)ReturnResult.NotOracleException;

            string sqlState = (((OracleException)ex).Errors[0]).Number.ToString();

            if (sqlState == ((int)ReturnResult.OracleUniqueConstraintException).ToString())
                return ReturnResult.OracleUniqueConstraintException;
            else if (sqlState == ((int)ReturnResult.OracleTimeoutOccurredException).ToString())
                return ReturnResult.OracleTimeoutOccurredException;
            else if(sqlState== ((int)ReturnResult.OracleDeadlockDetectedException).ToString())
                return ReturnResult.OracleDeadlockDetectedException;
            else if (sqlState == ((int)ReturnResult.OracleMaximumNumberOfSessionsExceededException).ToString())
                return ReturnResult.OracleMaximumNumberOfSessionsExceededException;
            else
                return ReturnResult.OracleOtherException;
        }
    }
}
