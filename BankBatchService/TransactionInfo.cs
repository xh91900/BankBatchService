using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// 交易信息集合（表）说明（2001号和2002报文方向0/1）
    /// </summary>
    public class TransactionInfo
    {
        /// <summary>
        /// 流水号PROXY_ACCOUNT_DETAIL.ID,pk,ListNO
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 记账卡客户账号,AccountId
        /// </summary>
        public string ACBAccount { get; set; }

        /// <summary>
        /// 记账卡客户账号名称,AccountName
        /// </summary>
        public string ACBAccountN { get; set; }

        /// <summary>
        /// 银行账户类型
        /// 对公0 ；储蓄1；信用卡2；
        /// </summary>
        public int AccType { get; set; }

        /// <summary>
        /// 交易金额（分）,AMOUNT
        /// </summary>
        public int Income { get; set; }

        /// <summary>
        /// 车牌号
        /// </summary>
        public string PlateNumbers { get; set; }

        /// <summary>
        /// 交易时刻,ICTransTime
        /// </summary>
        public DateTime TransTime { get; set; }

        /// <summary>
        /// 车型：0-未知；1-客一；2-客二；3-客三；
        /// 4-客四；5-货1；6-货2；7-货3；8-货4；9-货5
        /// </summary>
        public int VehicleType { get; set; }

        /// <summary>
        /// 银行（成功/失败）扣款返回时间
        /// </summary>
        public DateTime BankChargeTime { get; set; }

        /// <summary>
        /// 扣款结果
        /// 0-转账成功；1-账户余额不足；2-源账户不存在；3-目的账户不存在；4-未授权；5-其他
        /// </summary>
        public int Result { get; set; }
    }

    /// <summary>
    /// A_ACCOUNT
    /// </summary>
    public class AAccount
    {
        public string Account_No { get; set; }

        public int Version { get; set; }

        public int User_Type { get; set; }

        public string User_No { get; set; }

        public int Account_Type { get; set; }

        public int Deposit_Type { get; set; }

        public string Main_Card_No { get; set; }

        public decimal Balance { get; set; }

        public decimal Credit_Money { get; set; }

        public decimal Low_Money { get; set; }

        public decimal Cash_Limit { get; set; }

        public int Owne_Type { get; set; }

        public DateTime Registe_Date { get; set; }

        public DateTime Modify_Date { get; set; }

        public int Account_Status { get; set; }

        public string Operator { get; set; }

        public int Check_Flag { get; set; }

        public int Issue_Status { get; set; }

        public decimal Preassign_Balance { get; set; }

        public decimal Service_Fee_Balance { get; set; }

        public string Agent_No { get; set; }

        public decimal Cash_Deposit { get; set; }
    }

    /// <summary>
    /// 解约信息集合说明（2010和2012号报文方向0）
    /// </summary>
    public class BankAccountCancel
    {
        /// <summary>
        /// 流水号String[20],banktag+timespan+AccountID后六位
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 发送给银行信息的AccountID银行账号
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 发送给银行信息的BL_Corp用户所属企业名称（扣款用户名称）
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// 保证金减少部分的金额，记账金保证金金额减少信息
        /// </summary>
        public decimal CashDepositCut { get; set; }

        /// <summary>
        /// 解约/解绑车辆生效时刻
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 车牌号，记账金保证金金额减少信息
        /// </summary>
        public string PlateNumbers { get; set; }

        /// <summary>
        /// 发送给银行的文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 0-	已入中间库未发送
        /// 1-	已打包发送给银行
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 0-	2010号报文交易
        /// 1-	2012号报文交易
        /// </summary>
        public int Command { get; set; }
    }

    /// <summary>
    /// 银行账号与ETC卡绑定信息库及其集合
    /// </summary>
    public class BankAccountidCardNo
    {
        /// <summary>
        /// 二十位流水号String[20]
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 记账卡客户账号名称
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// ETC卡号
        /// </summary>
        public string JtcardId { get; set; }

        /// <summary>
        /// ETC卡片状态
        /// </summary>
        public string CardStatus { get; set; }
    }
}
