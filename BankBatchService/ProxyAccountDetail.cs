using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// 银行扣款交易记录
    /// </summary>
    public class ProxyAccountDetail
    {
        /// <summary>
        /// 2位银行标识+12位记账时间戳+循环的6位流水号,pk
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long Account_Detail_Out_Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Account_No { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Card_No { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Trans_Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal In_Money { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal Out_Money { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal Should_In_Money { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal Should_Out_Money { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal Old_Balance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Out_Offline_SN { get; set; }

        /// <summary>
        /// 发行方记账时刻
        /// </summary>
        public DateTime Charge_Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Transmit_Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Settle_Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ChargeStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Bank_Charge_Time { get; set; }

        /// <summary>
        /// 交易时刻
        /// </summary>
        public DateTime Ic_Trans_Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Provider_Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Package_No { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int TransId { get; set; }

        /// <summary>
        /// 银行渠道号，取值为BANK_CODE
        /// </summary>
        public string Agent_No { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Main_Card_No { get; set; }

        public string Bank_Tag { get; set; }

        /// <summary>
        /// 银行账号，来源于银行方发送的委托扣款协议信息的AccountID
        /// </summary>
        public string Account_Id { get; set; }

        /// <summary>
        /// 用户所属企业名称（扣款用户名称），来源于银行方发送的委托扣款协议信息BL_Corp
        /// </summary>
        public string Account_Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string NETNO { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string MID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Card_Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Physical_Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Issuer_Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string IcCard_No { get; set; }

        /// <summary>
        /// 交易前金额
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long TacNo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal Last_Balance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PsaMID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Psam_Tran_SN { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int DealStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Medium_Type { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public int Trans_SEQ { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Ticks_Type { get; set; }

        /// <summary>
        /// 入口路网号
        /// </summary>
        public string En_NetWork { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime En_Time { get; set; }

        /// <summary>
        /// 入口站/广场号
        /// </summary>
        public string En_Plazaid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int En_Operator_Id { get; set; }

        /// <summary>
        /// 入口车道号
        /// </summary>
        public string En_Shift_Id { get; set; }

        /// <summary>
        /// 出口路网号
        /// </summary>
        public string NetWork { get; set; }

        /// <summary>
        /// 出口站/广场号
        /// </summary>
        public string Plazaid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Operator_Id { get; set; }

        /// <summary>
        /// 出口车道号
        /// </summary>
        public string Shift_Id { get; set; }

        /// <summary>
        /// Detail节点中的车型字段
        /// </summary>
        public int VehClass { get; set; }

        /// <summary>
        /// 车牌号
        /// </summary>
        public string Car_Serial { get; set; }

        /// <summary>
        /// 清分日
        /// </summary>
        public DateTime Clear_Target_Date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 交易中的清分目标日
        /// </summary>
        public DateTime ClearTargetDate { get; set; }

        /// <summary>
        /// 交易金额（分）
        /// </summary>
        public decimal Due_Fare { get; set; }

        /// <summary>
        /// 交易金额（分）
        /// </summary>
        public decimal Cash { get; set; }

        /// <summary>
        /// 银行账号类型；0对公，1储蓄，2信用卡
        /// </summary>
        public int Account_Type { get; set; }
    }

    /// <summary>
    /// 银行扣款成功遗留交易记录
    /// </summary>
    public class ProxyAccountDetailSuccessNone : ProxyAccountDetail
    { }

    /// <summary>
    /// 银行扣款失败交易（准备记入扣保证金文件）记录
    /// </summary>
    public class ProxyAccountDetailBankFailWaitDeduct: ProxyAccountDetail
    {
    }

    /// <summary>
    /// 银行扣款失败交易再次扣款成功记录
    /// </summary>
    public class ProxyAccountDetailBankFailToSuccess : ProxyAccountDetail
    {
    }

    /// <summary>
    /// 银行扣款失败交易记录
    /// </summary>
    public class ProxyAccountDetailBankFail : ProxyAccountDetail
    {
    }

    /// <summary>
    /// 银行扣款成功交易通知清分交易记录表
    /// </summary>
    public class ProxyAccountDetailToClear : ProxyAccountDetail
    { }

    /// <summary>
    /// 
    /// </summary>
    public class FlAccountStalist
    {

        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Package_No { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Account_No { get; set; }

        /// <summary>
        /// 0:正常
        /// 1：低值
        /// 2：透支
        /// </summary>
        public int Issue_Status { get; set; }

        /// <summary>
        /// 当前时间
        /// </summary>
        public DateTime Start_Time { get; set; }

        /// <summary>
        /// 略
        /// </summary>
        public DateTime Package_Time { get; set; }

        /// <summary>
        /// 略
        /// </summary>
        public int Flag { get; set; }

        public string Remark { get; set; }

        public string Operator_No { get; set; }

        public string Agent_No { get; set; }
    }

    /// <summary>
    /// 银行渠道表
    /// </summary>
    public class BankAgent
    {
        /// <summary>
        /// 银行代码
        /// </summary>
        public string Bank_Code { get; set; }

        /// <summary>
        /// 两位数字标识，唯一确定一家银行
        /// </summary>
        public string Bank_Tag { get; set; }

        // 银行代码(弃)
        //public string Bank_Code { get; set; }

        /// <summary>
        /// FTPHost
        /// </summary>
        public string FTPHost { get; set; }

        /// <summary>
        /// 银行名称
        /// </summary>
        public string Bank_Name { get; set; }

        /// <summary>
        /// 记账卡备付金户账号名称
        /// </summary>
        public string Credit_Provisions_Name { get; set; }

        /// <summary>
        /// 记账卡备付金户账号
        /// </summary>
        public string Credit_Provisions_No { get; set; }

        /// <summary>
        /// 报文交易数上限
        /// </summary>
        public int Trans_Count_Max { get; set; }

        /// <summary>
        /// 银行上午接收扣款请求文件的时间点
        /// </summary>
        public string MrcvTime { get; set; }

        /// <summary>
        /// 银行下午午接收扣款请求文件的时间点
        /// </summary>
        public string ArcvTime { get; set; }

        /// <summary>
        /// 银行系统IP地址
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// 银行系统端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// ftp用户名
        /// </summary>
        public string FTPUserName { get; set; }

        /// <summary>
        /// ftp密码
        /// </summary>
        public string FTPPwd { get; set; }

        /// <summary>
        /// ftp端口号
        /// </summary>
        public int FTPPort { get; set; }

        /// <summary>
        /// ETC方从此目录接收银行文件
        /// </summary>
        public string RcvFtpDri { get; set; }

        /// <summary>
        /// ETC方从此目录发送文件给银行方
        /// </summary>
        public string SndFtpDri { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 银行支持任务
        /// </summary>
        public List<BankSupportTask> BankSupportTasks = new List<BankSupportTask>();
    }

    /// <summary>
    /// 输出定时处理任务数据集合
    /// </summary>
    public class OutPutTaskWaitingDone
    {
        /// <summary>
        /// PK
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
        /// 2010：解约信息
        /// </summary>
        public int TransType { get; set; }

        /// <summary>
        /// 当TRANSTYPE为2001时本字段为0代表T-1日的未扣款交易；为1代表T-1日之前的未扣款交易和银行扣款失败交易
        /// </summary>
        public int PriorityLevel { get; set; }

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
        /// 1：文件已上传到ftp（终态）
        /// 0：中间数据已生成,待ftp发送
        /// -1：文件生成失败
        /// -2：上传ftp失败
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 文件上传ftp时刻
        /// </summary>
        public DateTime SendTime { get; set; }

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
        /// 待扣款总笔数
        /// </summary>
        public int TotalNum { get; set; }

        /// <summary>
        /// 待扣款总金额（分）
        /// </summary>
        public long TotalAmount { get; set; }
    }

    /// <summary>
    /// BANK_CONFIG配置库
    /// </summary>
    public class Spara
    {
        /// <summary>
        /// pk
        /// </summary>
        public ObjectId _id;

        /// <summary>
        /// key
        /// </summary>
        public string Key;

        /// <summary>
        /// value
        /// </summary>
        public string Value;

        /// <summary>
        /// Remark
        /// </summary>
        public string Remark;
    }

    /// <summary>
    /// 银行签约账户表
    /// </summary>
    public class BankAccount
    {
        /// <summary>
        /// 两位数字标识，唯一确定一家银行
        /// </summary>
        public string Bank_Tag { get; set; }

        /// <summary>
        /// 银行方发送的委托扣款协议信息的AccountID
        /// </summary>
        public string Account_Id { get; set; }

        /// <summary>
        /// 银行方发送的委托扣款协议信息BL_Corp
        /// </summary>
        public string Account_Name { get; set; }

        /// <summary>
        /// 保证金金额（分）
        /// </summary>
        public decimal Cash_Deposit { get; set; }

        /// <summary>
        /// 银行端的生成日期
        /// </summary>
        public DateTime Gen_Time { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime Create_Time { get; set; }

        /// <summary>
        /// 记录最近一次修改时刻（通常修改保证金金额时修改），默认和记录创建时刻相同
        /// </summary>
        public DateTime Modify_Time { get; set; }

        /// <summary>
        /// 默认值-1由银行接口系统写入。
        /// 银行账户类型
        /// 对公0 ；储蓄1；信用卡2；
        /// </summary>
        public int Account_Type { get; set; }
    }

    /// <summary>
    /// 签约信息集合（2009和2011号报文方向1）
    /// </summary>
    public class BankAccountSign
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
        /// 银行方发送的委托扣款协议信息的AccountID银行账号
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 银行方发送的委托扣款协议信息的BL_Corp用户所属企业名称
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// 保证金金额（分）
        /// </summary>
        public int CashDeposit { get; set; }

        /// <summary>
        /// 银行端的生成日期
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 0-	新增的签约交易
        /// 1-	修改的签约交易（修改保证金）
        /// 2-	保证金补缴成功交易，若没有其他扣款失败的交易则需要漂白黑名单
        /// </summary>
        public int Command { get; set; }

        /// <summary>
        /// 银行发送的文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 0-	已入中间库未同步到核心
        /// 1-	已同步到核心（终态）
        /// </summary>
        public int Status { get; set; }
    }

    /// <summary>
    /// 一卡通黑白名单
    /// </summary>
    public class BankAccountYKTSign
    {
        /// <summary>
        /// pk，accountid
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 银行方发送的委托扣款协议信息的BL_Corp用户所属企业名称
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// 保证金金额（分）
        /// </summary>
        public int CashDeposit { get; set; }

        /// <summary>
        /// 银行端的生成日期
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 0-	账户状态正常
        /// 1-	账户余额不足
        /// </summary>
        public int Status { get; set; }
    }

    /// <summary>
    /// 银行账号绑定关系表
    /// </summary>
    public class BankAccountBinding
    {
        /// <summary>
        /// 主键 自增
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ETC账号A_ACCOUNT.ACCOUNT_NO
        /// </summary>
        public string account_No { get; set; }

        /// <summary>
        /// 两位数字标识，唯一确定一家银行
        /// </summary>
        public string Bank_Tag { get; set; }

        /// <summary>
        /// 银行账号，来源于银行方发送的委托扣款协议信息的AccountID
        /// 用户更换银行卡，本绑定记录不删除仅更新STATUS为1，同时新增一条绑定关系记录
        /// </summary>
        public string Account_Id { get; set; }

        /// <summary>
        /// 0-	已绑定ETC账号
        /// 1-	已解绑ETC账号
        /// -1- 待解约
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime Create_Time { get; set; }

        /// <summary>
        /// 记录最近一次修改时刻，默认和记录创建时刻相同
        /// </summary>
        public DateTime Modify_Time { get; set; }
    }

    /// <summary>
    /// 银行支持任务表
    /// </summary>
    public class BankSupportTask
    {
        /// <summary>
        /// 主键 自增
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 银行代码，固定6位
        /// </summary>
        public string Bank_Code { get; set; }

        /// <summary>
        /// 负2001-扣款失败和未扣款在一个扣款文件
        /// 2001-新版记账金客户转账数据
        /// 2002-记账金保证金转账数据
        /// 负2009-旧版签约信息
        /// 2009-新版签约信息
        /// 2010-解约信息
        /// 2011-记账金保证金补缴成功信息
        /// 2012-记账金保证金金额减少信息
        /// </summary>
        public int File_Task_Type { get; set; }

        /// <summary>
        /// 对FILETASKTYPE的详细说明
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 解绑车辆信息流水表
    /// </summary>
    public class RemoveCarBinding
    {
        /// <summary>
        /// 主键 自增
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ETC账号
        /// </summary>
        public string Account_No { get; set; }

        /// <summary>
        /// ETC卡号
        /// </summary>
        public string Card_No { get; set; }

        /// <summary>
        /// 两位数字标识，唯一确定一家银行
        /// </summary>
        public string Bank_Tag { get; set; }

        /// <summary>
        /// 银行账号
        /// </summary>
        public string Account_Id { get; set; }

        /// <summary>
        /// 用户所属企业名称（扣款用户名称）
        /// </summary>
        public string Account_Name { get; set; }

        /// <summary>
        /// 银行账户类型
        /// 对公0 ；储蓄1；信用卡2
        /// </summary>
        public int Account_Type { get; set; }

        /// <summary>
        /// 解绑车辆车牌号
        /// </summary>
        public string Plate_Numbers { get; set; }

        /// <summary>
        /// 0-	已预约解绑（发行系统填写）
        /// 1-	已解绑生效（银行接口系统填写）
        /// 2-	有欠费记录不予解绑
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 保证金金额（元）
        /// </summary>
        public decimal Cash_Deposit_Cut { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime Create_Time { get; set; }

        /// <summary>
        /// 记录最近一次修改时刻，默认和记录创建时刻相同
        /// </summary>
        public DateTime Modify_Time { get; set; }

        /// <summary>
        /// 操作员编号
        /// </summary>
        public string Operator_No { get; set; }

        /// <summary>
        /// 操作员姓名
        /// </summary>
        public string Operator_Name { get; set; }

        /// <summary>
        /// 操作时刻
        /// </summary>

        public DateTime OPT_Time { get; set; }

        /// <summary>
        /// 营业厅编号
        /// </summary>
        public string Agent_No { get; set; }

        /// <summary>
        /// 营业厅名称
        /// </summary>
        public string Agent_Name { get; set; }
    }
}
