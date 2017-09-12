using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// 输入定时处理任务数据集合
    /// </summary>
    public class InputTaskWaitingDone
    {
        /// <summary>
        /// pk
        /// </summary>
        public ObjectId _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 交易类型
        /// 2001：记账金客户转账数据
        /// 2002：记账保证金客户转账数据
        /// 2009：委托扣款协议信息
        /// 2011：保证金补缴程序信息
        /// </summary>
        public int TransType { get; set; }

        /// <summary>
        /// 库名
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// 集合名
        /// </summary>
        public string ColName { get; set; }

        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 1：已同步到核心（终态）
        /// 0：文件已入中间库待同步到核心
        /// -1：ftp下载文件失败（文件不存在/ftp服务器通信失败等原因）
        /// -2：解密文件失败
        /// -3：校验文件失败
        /// -4：其他错误原因
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 加解密密钥
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 扣款成功数量
        /// </summary>
        public int SuccessNum { get; set; }

        /// <summary>
        /// 扣款成功金额（分）
        /// </summary>
        public long SuccessAmount { get; set; }

        /// <summary>
        /// 扣款失败数量
        /// </summary>
        public int FailNum { get; set; }

        /// <summary>
        /// 扣款失败金额（分）
        /// </summary>
        public long FailAmount { get; set; }
    }
}
