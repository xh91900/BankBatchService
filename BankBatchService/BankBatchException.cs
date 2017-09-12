using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// ErrorCode
    /// </summary>
    public class BankBatchException: Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="code">错误码</param>
        public BankBatchException(EErrorCode code) : base(((int)code).ToString() + GetErrorMessage(code))
        { }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <param name="code">错误码</param>
        /// <returns>错误信息</returns>
        private static string GetErrorMessage(EErrorCode code)
        {
            string err = "";
            switch (code)
            {
                case EErrorCode.Success:
                    err = "";   // "无错误"
                    break;
                case EErrorCode.NoMessage:
                    err = "查询成功但无数据";
                    break;
                case EErrorCode.DatabaseConnectionTimeout:
                    err = "数据据链接超时";
                    break;
                case EErrorCode.DatabaseInvalidOperation:
                    err = "数据库操作失败";
                    break;
                case EErrorCode.DatabaseOperationTimeout:
                    err = "数据库操作超时";
                    break;
            }
            return err;
        }
    }

    /// <summary>
    /// 错误码
    /// </summary>
    public enum EErrorCode
    {
        NoMessage = 200,// （无内容） 服务器成功处理了请求，但没有返回任何内容。
        Success = 201,// （有内容） 服务器成功处理了请求，并返回内容。
        DatabaseConnectionTimeout = 202,// 数据库连接超时。
        DatabaseOperationTimeout = 203,// 数据库操作超时。
        DatabaseInvalidOperation = 204,// 数据库操作失败。
    }
}
