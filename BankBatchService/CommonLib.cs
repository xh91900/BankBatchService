using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    public static class SYSConstant
    {
        #region MongoDB数据库名

        /// <summary>
        /// 共用的配置库
        /// </summary>
        public const string BANK_CONFIG = "BANK_CONFIG";

        /// <summary>
        /// 任务库
        /// </summary>
        public const string BANK_TASK = "BANK_TASK";

        /// <summary>
        /// 交易库
        /// </summary>
        public const string BANK_TRANS = "BANK_TRANS";

        /// <summary>
        /// 签约解约信息库
        /// </summary>
        public const string BANK_ACCOUNT = "BANK_ACCOUNT";

        /// <summary>
        /// 银行账号与ETC卡绑定信息库
        /// </summary>
        public const string BANK_ACCOUNTID_CARDNO = "BANK_ACCOUNTID_CARDNO";

        #endregion

        #region MongoDB集合名

        // 输入定时处理任务数据集合
        public const string INPUTTASK_WAITING_DONE = "INPUTTASK_WAITING_DONE";

        // 输出定时处理任务数据集合
        public const string OUTPUTTASK_WAITING_DONE = "OUTPUTTASK_WAITING_DONE";

        // S_PARA集合
        public const string S_PARA = "S_PARA";

        // BANK_AGENT集合
        public const string BANK_AGENT = "BANK_AGENT";

        // 中间业务数据库签约信息集合
        public const string BANK_ACCOUNT_SIGN = "BANK_ACCOUNT_SIGN";

        // 一卡通签约信息增量集合
        public const string BANK_ACCOUNT_SIGN_YKTINCREMENT = "BANK_ACCOUNT_SIGN_YKTINCREMENT";

        // 解约信息集合说明（2010和2012号报文方向0）
        public const string BANK_ACCOUNT_CANCEL = "BANK_ACCOUNT_CANCEL";

        #endregion

        // 结算中心编码
        public const string SettleCenterCode = "0010000";

        // 扣保证金资金争议期（弃）
        public const int DepositCapitalDisputePeriod = 30;

        // 解约资金争议期（弃）
        public const int CancellationCapitalDisputePeriod = 30;

        // 解约资金争议期窗口期（弃）
        public const int CancellationCapitalDisputePeriodPlus = 3;

        /// <summary>
        /// 应用配置参数集合
        /// </summary>
        public static List<Spara> sParam;

        /// <summary>
        /// 获取系统配置
        /// </summary>
        public static void GetParam()
        {
            try
            {
                MongoDBAccess<Spara> mongoAccess = new MongoDBAccess<Spara>(SYSConstant.BANK_CONFIG, SYSConstant.S_PARA);
                List<Spara> sPara = mongoAccess.FindAsByWhere(p => !string.IsNullOrEmpty(p.Key), 0);
                sParam = sPara;
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor("获取系统配置异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("获取系统配置异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// sequences
        /// </summary>
        public static List<string> sequences = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        /// <summary>
        /// 起始序列号
        /// </summary>
        public static string currentSequence = "000000";

        /// <summary>
        /// 获取下一个sequences(6位)
        /// 000000-ZZZZZZ循环累加
        /// </summary>
        /// <param name="curSequence">上一个字符串</param>
        /// <returns>string</returns>
        public static string GetNextSequence(string curSequence)
        {
            if (string.IsNullOrEmpty(curSequence) == false)
            {
                string str = curSequence[curSequence.Length - 1].ToString();
                int index = sequences.IndexOf(str);
                if (index + 1 == sequences.Count)
                {
                    return GetNextSequence(curSequence.Substring(0, curSequence.Length - 1)) + sequences[0];
                }
                else
                {
                    return curSequence.Substring(0, curSequence.Length - 1) + sequences[index + 1];
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public static class DBConstant
    {
        public const string FL_ACCOUNT_STALIST = "ECDBA.FL_ACCOUNT_STALIST";

        // 银行扣款失败交易（准备记入扣保证金文件）记录表
        public const string PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT = "ISSUE.PAD_BANKFAILWAITDEDUCT";

        // 银行扣款失败交易记录表
        public const string PROXY_ACCOUNT_DETAIL_BANKFAIL = "ISSUE.PAD_BANKFAIL";

        // 银行扣款失败交易再次扣款成功记录表
        public const string PROXY_ACCOUNT_DETAIL_BANKFAILTOSUCCESS = "ISSUE.PAD_BANKFAILTOSUCCESS";

        // 银行扣款成功遗留交易记录表
        public const string PROXY_ACCOUNT_DETAIL_SUCCESSNONE = "ISSUE.PAD_SUCCESSNONE";

        // 银行扣款交易记录表
        public const string PROXY_ACCOUNT_DETAIL = "ISSUE.PROXY_ACCOUNT_DETAIL";

        // 银行扣款成功交易通知清分交易记录表
        public const string PROXY_ACCOUNT_DETAIL_TOCLEAR = "ISSUE.PAD_TOCLEAR";

        // 银行渠道表
        public const string BANK_AGENT = "ISSUE.BANK_AGENT";

        // 银行支持任务表
        public const string BANK_SUPPORT_TASK="ISSUE.BANK_SUPPORT_TASK";

        // a_account
        public const string AAccount = "ISSUE.A_Account";

        // ISSUE.CLEAR_ACCOUNT_CHECK_INFOS
        public const string ClearAccountCheckInfos = "ISSUE.CLEAR_ACCOUNT_CHECK_INFOS";

        // 核心库中银行签约账户表
        public const string BANK_ACCOUNT = "ISSUE.BANK_ACCOUNT";

        // 解绑车辆信息流水表
        public const string REMOVE_CAR_BINDING = "ISSUE.REMOVE_CAR_BINDING";

        // 银行账号绑定关系表
        public const string BANK_ACCOUNT_BINDING = "ISSUE.BANK_ACCOUNT_BINDING";

        public const string CARD_ACCOUNT = "ISSUE.CARD_ACCOUNT";

        // 记账程序相关数据表
        public const string T_DISPUTE_DATA = "DB2INST2.T_DISPUTE_DATA";

        public const string T_DISPUTE_DETAIL = "DB2INST2.T_DISPUTE_DETAIL";

        public const string FL_CHARGE = "ECDBA.FL_CHARGE";

        public const string T_TRANSACTION = "ISSUE.T_TRANSACTION";

        public const string MSG_SND = "ECDBA.MSG_SND";

        /// <summary>
        /// 银行扣款交易记录表字段
        /// </summary>
        public const string sqlProxyAccountDetail = @"id, account_detail_out_id, account_no, card_no, in_money, out_money, should_in_money, should_out_money,
old_balance, out_offline_sn, charge_time, transmit_time, settle_time, chargestatus, bank_charge_time, provider_id, package_no, transid, agent_no, main_card_no,
bank_tag, account_id, account_name, account_type, netno, mid, card_type, physical_type, issuer_id, iccard_no, balance, last_balance, tacno, psamid, psam_tran_sn,
dealstatus, medium_type, trans_type, ic_trans_time, trans_seq, ticks_type, en_network, en_time, en_plazaid, en_operator_id, en_shift_id, network, plazaid,
operator_id, shift_id, vehclass, car_serial, clear_target_date, description, cleartargetdate, due_fare, cash";


        public const string sqlFLRCVDATA = @"PROVIDER_ID,PACKAGE_NO,TRANSID,NETNO,MID,
                                            CARD_TYPE,PHYSICAL_TYPE,ISSUER_ID,ISSUER_NUM,ICCARD_NO,
                                            BALANCE,DUE_FARE,CASH,TACNO,LAST_BALANCE,
                                            PSAMID,PSAM_TRAN_SN,DEALSTATUS,MEDIUM_TYPE,ISSUER_CARD_TYPE,
                                            TRANS_TYPE,IC_TRANS_TIME,TRANS_SEQ,TICKS_TYPE,CARD_TRANS_COUNT,
                                            EN_NETWORK,EN_TIME,EN_PLAZAID,EN_OPERATOR_ID,EN_SHIFT_ID,
                                            NETWORK,PLAZAID,OPERATOR_ID,SHIFT_ID,VEHCLASS,
                                            CAR_SERIAL,CLEARTARGETDATE,DESCRIPTION";//共38字段

        public const string sqlRCVTOTT = @" ,VERSION,DEAL_STATUS,CHARGE_STATUS,CHARGE_CASH,CHARGE_TIME,
                                            CHARGE_PACKAGE_NO,CHARGE_PACKAGE_TIME,PROCESS_TIME,CARD_ACCOUNT_BALANCE,ACCOUNT_NO,
                                            REMIT_ID,REMIT_CASH,REMIT_TIME,REMIT_PACKAGE_NO,REMIT_PACKAGE_TIME,
                                            STATISTICS_DATE,CLEAR_TARGET_DATE";//共17字段

    }

    /// <summary>
    /// 交易类型
    /// </summary>
    public enum ETransType
    {
        记账金客户转账结果数据 = 2001,
        记账保证金转账结果数据 = 2002,
        委托扣款协议信息 = 2009,
        解约信息 = 2010,
        保证金补缴成功信息 = 2011,
        记账金保证金金额减少信息 = 2012,
        银行账号与ETC卡绑定信息=2008,
        扣款失败和未扣款在一个扣款文件=-2001
    }

    /// <summary>
    /// 签约操作类别
    /// </summary>
    public enum ESignCommand
    {
        新增的签约交易=0,
        修改的签约交易=1,
        保证金补缴成功交易=2
    }

    /// <summary>
    /// 返回结果
    /// </summary>
    public enum ReturnResult
    {
        NotOracleException=0,
        OracleUniqueConstraintException = 1,// 违反唯一约束条件 
        OracleMaximumNumberOfSessionsExceededException = 2,// 超出最大会话数
        OracleTimeoutOccurredException = 51,// 等待资源超时
        OracleDeadlockDetectedException= 60,// 等待资源时检测到死锁 
        OracleOtherException=500// 其他错误
    }

    /// <summary>
    /// 扣款结果
    /// </summary>
    public enum EDebitResult
    {
        银行系统返回转账成功 = 0,
        银行系统返回账户余额不足 = 1,
        银行系统返回源账户不存在 = 2,
        银行系统返回目的账户不存在 = 3,
        银行系统返回未授权 = 4,
        银行系统返回其他=5
    }
}
