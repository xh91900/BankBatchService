using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// 出库方向定时任务
    /// </summary>
    public class PutOutStorageTask
    {
        // 最多几个线程同时访问
        SemaphoreSlim semLim;
        
        public void Start()
        {
            try
            {
                while (true)
                {
                    // 获取配置集合里的启动时间
                    BankConfigurationOperator configuration = new BankConfigurationOperator();
                    List<Spara> sparaList = SYSConstant.sParam.FindAll(p => p.Key.Equals("S_ACCOUNT_TRANSFER_TASK"));

                    // 00:10
                    foreach (var item in sparaList)
                    {
                        DateTime startTime = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + item.Value);
                        if (startTime.TimeOfDay < DateTime.Now.TimeOfDay)
                        {
                            startTime = startTime.AddDays(1);
                        }
                        TimeSpan timeSpan = startTime - DateTime.Now;
                        double interval = timeSpan.TotalMilliseconds;

                        // 创建心跳
                        System.Timers.Timer _HeartBeat = new System.Timers.Timer();
                        _HeartBeat.Interval = System.Math.Abs(interval);
                        _HeartBeat.Enabled = true;
                        _HeartBeat.Elapsed += _HeartBeat_Elapsed;
                        _HeartBeat.AutoReset = false;
                    }

                    // 每24小时启动一次
                    Thread.Sleep(24 * 60 * 60 * 1000);
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务主线程执行异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务主线程执行异常" + ex.ToString());
            }
        }

        /// <summary>
        /// 定时执行
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">args</param>
        private void _HeartBeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 设置最大线程数
            try
            {
                semLim = new SemaphoreSlim(int.Parse(SYSConstant.sParam.Find(p => p.Key.Equals("S_SEMAPHORE_SLIM")).Value));
                List<BankAgent> bankAgents = QueryAllBankAgent();
                if (bankAgents != null && bankAgents.Any())
                {
                    foreach (var bankAgent in bankAgents)
                    {
                        Task.Run(() => { Worker(bankAgent); });
                    }
                }
            }
            catch (AggregateException ex)
            {
                ShowMessage.ShowMsgColor("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常" + ex.ToString());
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ShowMessage.ShowMsgColor("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常" + ex.ToString());
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常" + ex.ToString());
            }
        }
        /// <summary>
        /// 工作子线程
        /// 记账金客户转账（2001和2002号文件扣款请求）数据0时（S_PARA集合中的配置项）归集定时任务
        /// </summary>
        /// <param name="bankAgent">BankAgent</param>
        public void Worker(BankAgent bankAgent)
        {
            try
            {
                ShowMessage.ShowMsgColor("开始处理" + bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）数据定时归集任务", System.Drawing.Color.Green);
                LogCommon.GetInfoLogInstance().LogError("开始处理" + bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）数据定时归集任务");

                int writeCount;
                if (bankAgent.BankSupportTasks.Any(p => p.File_Task_Type == (int)ETransType.扣款失败和未扣款在一个扣款文件))
                {
                    writeCount = PutAccountDetailInToCollection(bankAgent, 2, ETransType.记账金客户转账结果数据);
                }
                else
                {
                    writeCount = PutAccountDetailInToCollection(bankAgent, 0, ETransType.记账金客户转账结果数据);

                    writeCount += PutAccountDetailInToCollection(bankAgent, 1, ETransType.记账金客户转账结果数据);
                }

                if (writeCount == 0)
                {
                    // 若没有取到PROXY_ACCOUNT_DETAIL和PROXY_ACCOUNT_DETAIL_BANKFAIL交易,则直接写入OUTPUTTASK_WAITING_DONE（TRANSTYPE=2001、STATUS=0、PRIORITYLEVEL=0）一条交易
                    InsertIntoOutPutTaskWaitingDone(bankAgent, 0, ETransType.记账金客户转账结果数据, string.Empty);
                    ShowMessage.ShowMsgColor(string.Format("写入一条空汇总信息作为心跳包：{0}_{1}_{2}", bankAgent.Bank_Name, (int)ETransType.记账金客户转账结果数据, string.Empty), System.Drawing.Color.Green);
                }
                PutAccountDetailInToCollection(bankAgent, 0, ETransType.记账保证金转账结果数据);

                ShowMessage.ShowMsgColor(bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）数据定时归集任务处理完成", System.Drawing.Color.Green);
                LogCommon.GetInfoLogInstance().LogError(bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）数据定时归集任务处理完成");
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("记账金客户转账（2001和2002号文件扣款请求）数据归集定时任务工作线程执行异常" + ex.ToString());
            }
        }

        /// <summary>
        /// 执行reader逐条读取，按交易数限额（程序实现计数器）逐条写入中间业务数据库明细集合
        /// </summary>
        /// <param name="bankAgent">BankAgent</param>
        /// <param name="PriorityLevel">默认0:t-1日，1：t-1日之前和扣款失败的,2：0和1合并的</param>
        /// <param name="TransType">交易类型</param>
        private int PutAccountDetailInToCollection(BankAgent bankAgent, int PriorityLevel, ETransType TransType)
        {
            // 超过设置的最大线程数则阻止当前线程进入
            semLim.Wait();

            // 写入明细集合的记录数
            int writeCount = 0;

            // 计数器
            int counter = 0;

            // 文件名
            string collectionName = string.Empty;

            // mongo操作类
            MongoDBAccess<TransactionInfo> mongoAccess = null;

            try
            {
                // 取前一天的数据
                DateTime yesterday = DateTime.Now.AddDays(-1).Date;
                string sql = string.Empty;
                if (PriorityLevel == 0 && TransType == ETransType.记账金客户转账结果数据)
                {
                    sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time >=" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE_TIME) + " AND charge_time<" + OracleDBAccess.ToDbDatetime(DateTime.Now.Date, OracleDBAccess.SQLFMT_DATE_TIME) + " AND CHARGESTATUS=0 ";
                }
                else if (PriorityLevel == 1 && TransType == ETransType.记账金客户转账结果数据)
                {
                    sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time<" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE) + "and CHARGESTATUS = 0" + @" union all 
SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag);

                    //                    if (bankAgent.BankSupportTasks.Any(p => p.File_Task_Type == (int)ETransType.记账保证金转账结果数据))
                    //                    {
                    //                        sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time<" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE) + "and CHARGESTATUS = 0" + @" union all 
                    //SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag);
                    //                    }
                    //                    else
                    //                    {
                    //                        sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time<" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE) + "and CHARGESTATUS = 0" + @" union all 
                    //SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " union all SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag);
                    //                    }
                }
                else if (PriorityLevel == 2 && TransType == ETransType.记账金客户转账结果数据)
                {

                    sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time<" + OracleDBAccess.ToDbDatetime(DateTime.Now.Date, OracleDBAccess.SQLFMT_DATE) + "and CHARGESTATUS = 0" + @" union all 
SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag);

                    //                    if (bankAgent.BankSupportTasks.Any(p => p.File_Task_Type == (int)ETransType.记账保证金转账结果数据))
                    //                    {
                    //                        sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time<" + OracleDBAccess.ToDbDatetime(DateTime.Now.Date, OracleDBAccess.SQLFMT_DATE) + "and CHARGESTATUS = 0" + @" union all 
                    //SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag);

                    //                        sql+= @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time >=" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE_TIME) + " AND charge_time<" + OracleDBAccess.ToDbDatetime(DateTime.Now.Date, OracleDBAccess.SQLFMT_DATE_TIME) + " AND CHARGESTATUS=0 ";
                    //                    }
                    //                    else
                    //                    {
                    //                        sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time<" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE) + "and CHARGESTATUS = 0" + @" union all 
                    //SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " union all SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + "union all ";

                    //                        sql+= @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag) + " and charge_time >=" + OracleDBAccess.ToDbDatetime(yesterday, OracleDBAccess.SQLFMT_DATE_TIME) + " AND charge_time<" + OracleDBAccess.ToDbDatetime(DateTime.Now.Date, OracleDBAccess.SQLFMT_DATE_TIME) + " AND CHARGESTATUS=0 ";
                    //                    }
                }
                else if (PriorityLevel == 0 && TransType == ETransType.记账保证金转账结果数据)
                {
                    if (bankAgent.BankSupportTasks.Any(p => p.File_Task_Type == (int)ETransType.记账保证金转账结果数据))
                    {
                        sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where BANK_TAG=" + OracleDBAccess.GetSingleQuote(bankAgent.Bank_Tag);
                    }
                }

                if (!string.IsNullOrEmpty(sql))
                {
                    using (OracleDBAccess oracleAccess = new OracleDBAccess())
                    {
                        IDataReader myReader;
                        TransactionInfo info = new TransactionInfo();
                        oracleAccess.CommandText = sql;
                        myReader = oracleAccess.ExecuteReader();
                        try
                        {
                            while (myReader.Read())
                            {
                                // 超过最大限制数量新建集合插入
                                if (counter % bankAgent.Trans_Count_Max == 0)
                                {
                                    // 包类型代码_文件名（不含后缀）例如,“2001_001000090020020170515091020”表示结算中心发送给工商银行的记账金客户转账请求数据集合
                                    collectionName = string.Format("{0}_{1}{2}{3}", (int)TransType, SYSConstant.SettleCenterCode, bankAgent.Bank_Code, DateTime.Now.ToString("yyyyMMddHHmmss"));
                                    mongoAccess = new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, collectionName);

                                    // 将汇总信息写入输出定时处理任务数据集合
                                    InsertIntoOutPutTaskWaitingDone(bankAgent, PriorityLevel, TransType, collectionName);
                                    ShowMessage.ShowMsgColor(string.Format("写入一条汇总信息：{0}_{1}_{2}", bankAgent.Bank_Name, (int)TransType, collectionName), System.Drawing.Color.Green);
                                }
                                info._id = myReader["id"].ToString();
                                info.ACBAccount = myReader["ACCOUNT_ID"].ToString();
                                info.ACBAccountN = myReader["ACCOUNT_NAME"].ToString();
                                info.Income = int.Parse((decimal.Parse(myReader["OUT_MONEY"].ToString()) * 100).ToString("F0"));
                                info.BankChargeTime = DateTime.Parse(myReader["CHARGE_TIME"].ToString()).AddHours(8);
                                info.TransTime = DateTime.Parse(myReader["IC_TRANS_TIME"].ToString()).AddHours(8);
                                info.PlateNumbers = myReader["CAR_SERIAL"].ToString();
                                if (string.IsNullOrEmpty(myReader["CAR_SERIAL"].ToString()))
                                {
                                    info.PlateNumbers = "未知";
                                }
                                int vehicleType = 0;
                                int.TryParse(myReader["VEHCLASS"].ToString(), out vehicleType);
                                info.VehicleType = new int[] { 0, 1, 2, 3, 4, 11, 12, 13, 14, 15 }.Contains(vehicleType) ? vehicleType : 0;
                                info.AccType = new string[] { "0", "1", "2", "3", "4" }.Contains(myReader["account_type"].ToString()) ? int.Parse(myReader["account_type"].ToString()) : 9;
                                if (info.AccType == 3)
                                {
                                    info.AccType = 1;
                                }
                                else if (info.AccType == 4)
                                {
                                    info.AccType = 2;
                                }

                                // 写入动态库失败会发送不完整的扣款文件。
                                mongoAccess.InsertOne(info);

                                // 每次插入之后清空
                                ClearTransactionInfo(info);
                                counter++;
                                writeCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            myReader.Close();
                        }
                    }
                }
            }
            catch (TimeoutException ex)
            {
                LogCommon.GetErrorLogInstance().LogError(bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）归集定时任务执行超时" + ex.ToString());
                ShowMessage.ShowMsgColor(bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）归集定时任务执行超时", System.Drawing.Color.Red);
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError(bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）归集定时任务异常" + ex.ToString());
                ShowMessage.ShowMsgColor(bankAgent.Bank_Name + "记账金客户转账（2001和2002号文件扣款请求）归集定时任务异常", System.Drawing.Color.Red);
            }
            finally
            {
                // 执行完毕给一个退出信号
                semLim.Release();
            }
            return writeCount;
        }

        /// <summary>
        /// 将汇总信息写入输出定时处理任务数据集合
        /// </summary>
        /// <param name="bankAgent">BankAgent</param>
        /// <param name="PriorityLevel">默认0</param>
        /// <param name="TransType">交易类型</param>
        private void InsertIntoOutPutTaskWaitingDone(BankAgent bankAgent, int PriorityLevel, ETransType TransType, string colName)
        {
            try
            {
                MongoDBAccess<OutPutTaskWaitingDone> mongoAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                mongoAccess.InsertOne(new OutPutTaskWaitingDone() { BankTag = bankAgent.Bank_Tag, TransType = (int)TransType, Status = 0, PriorityLevel = PriorityLevel, ColName = colName, CreateTime = DateTime.Now.AddHours(8), TotalAmount = 0, TotalNum = 0 });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 清空TransactionInfo对象
        /// </summary>
        /// <param name="info">TransactionInfo</param>
        private void ClearTransactionInfo(TransactionInfo info)
        {
            info.ACBAccount = string.Empty;
            info.ACBAccountN = string.Empty;
            info.Income = 0;
            info.BankChargeTime = new DateTime();
            info.TransTime = new DateTime();
            info.PlateNumbers = string.Empty;
            info.Result = 0;
            info.VehicleType = 0;
        }

        /// <summary>
        /// 获取所有银行基础信息
        /// </summary>
        /// <returns></returns>
        public List<BankAgent> QueryAllBankAgent()
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = @"select * from " + DBConstant.BANK_AGENT;
                    List<BankAgent> agentList = oracleAccess.QuerySql<BankAgent>(sql, null);
                    foreach (BankAgent bankAgent in agentList)
                    {
                        sql = @"select * from " + DBConstant.BANK_SUPPORT_TASK + " where bank_code=:bank_code";
                        List<BankSupportTask> supportList = oracleAccess.QuerySql<BankSupportTask>(sql, new { bank_code = bankAgent.Bank_Code });
                        bankAgent.BankSupportTasks = supportList;
                    }
                    ShowMessage.ShowMsgColor("线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "获得" + agentList.Count + "条银行基础信息", System.Drawing.Color.Green);
                    return agentList;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 启动保证金扣款交易归集定时任务
        /// </summary>
        public void StartProcessDepositTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_MARGIN_CALL_TASK_HEARTBEAT").Value) * 60 * 60 * 1000;// 小时
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理保证金扣款交易归集定时任务", System.Drawing.Color.Green);
                LogCommon.GetInfoLogInstance().LogError("开始处理保证金扣款交易归集定时任务");
                DepositTask();
                ShowMessage.ShowMsgColor("保证金扣款交易归集定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_MARGIN_CALL_TASK_HEARTBEAT").Value + "小时", System.Drawing.Color.Green);
                LogCommon.GetInfoLogInstance().LogError("保证金扣款交易归集定时任务处理完成");
                System.Threading.Thread.Sleep(timeSpan);// 单位小时
            }
        }

        /// <summary>
        /// 保证金扣款交易归集定时任务3
        /// 定时线程i每天启动1次
        /// </summary>
        private void DepositTask()
        {
            try
            {
                string sql = string.Empty;
                List<BankAgent> bankAgent = QueryAllBankAgent().FindAll(p => p.BankSupportTasks.Any(o => o.File_Task_Type == (int)ETransType.记账保证金转账结果数据));

                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    // 前时刻与BANK_CHARGE_TIME之差大于30天的记录,只做支持2002的银行
                    sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where trunc(sysdate)-trunc(BANK_CHARGE_TIME)>" + int.Parse(SYSConstant.sParam.Find(p => p.Key == "DEPOSITCAPITALDISPUTEPERIOD").Value) + " AND ACCOUNT_TYPE<>2 AND BANK_TAG in ('" + string.Join("','", bankAgent.Select(p => p.Bank_Tag)) + "')";
                    List<ProxyAccountDetailBankFail> bankFailList = oracleAccess.QuerySql<ProxyAccountDetailBankFail>(sql, null);
                    foreach (var bankFail in bankFailList)
                    {
                        try
                        {
                            oracleAccess.BeginTransaction();
                            // 交易数据插入PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT
                            sql = @"insert into " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + @"
  (id, account_detail_out_id, account_no, card_no, in_money, out_money, should_in_money, should_out_money, old_balance, out_offline_sn, charge_time, transmit_time, 
settle_time, chargestatus, bank_charge_time, provider_id, package_no, transid, agent_no, main_card_no, bank_tag, account_id, account_name, account_type, netno, mid, 
card_type, physical_type, issuer_id, iccard_no, balance, last_balance, tacno, psamid, psam_tran_sn, dealstatus, medium_type, trans_type, ic_trans_time, trans_seq, 
ticks_type, en_network, en_time, en_plazaid, en_operator_id, en_shift_id, network, plazaid, operator_id, shift_id, vehclass, car_serial, clear_target_date, 
description, cleartargetdate, due_fare, cash)
values
  (:id, :account_detail_out_id, :account_no, :card_no, :in_money, :out_money, :should_in_money, :should_out_money, :old_balance, :out_offline_sn, 
:charge_time, :transmit_time, :settle_time, :chargestatus, :bank_charge_time, :provider_id, :package_no, :transid, :agent_no, :main_card_no, :bank_tag, 
:account_id, :account_name, :account_type, :netno, :mid, :card_type, :physical_type, :issuer_id, :iccard_no, :balance, :last_balance, :tacno, :psamid, 
:psam_tran_sn, :dealstatus, :medium_type, :trans_type, :ic_trans_time, :trans_seq, :ticks_type, :en_network, :en_time, :en_plazaid, :en_operator_id, 
:en_shift_id, :network, :plazaid, :operator_id, :shift_id, :vehclass, :car_serial, :clear_target_date, :description, :cleartargetdate, :due_fare, :cash)";
                            var param = new
                            {
                                id = bankFail.ID,
                                account_detail_out_id = bankFail.Account_Detail_Out_Id,
                                account_no = bankFail.Account_No,
                                card_no = bankFail.Card_No,
                                in_money = bankFail.In_Money,
                                out_money = bankFail.Out_Money,
                                should_in_money = bankFail.Should_In_Money,
                                should_out_money = bankFail.Should_Out_Money,
                                old_balance = bankFail.Old_Balance,
                                out_offline_sn = bankFail.Out_Offline_SN,
                                charge_time = bankFail.Charge_Time,
                                transmit_time = DateTime.Now,
                                settle_time = bankFail.Settle_Time,
                                chargestatus = bankFail.ChargeStatus,
                                bank_charge_time = bankFail.Bank_Charge_Time,
                                provider_id = bankFail.Provider_Id,
                                package_no = bankFail.Package_No,
                                transid = bankFail.TransId,
                                agent_no = bankFail.Agent_No,
                                main_card_no = bankFail.Main_Card_No,
                                bank_tag = bankFail.Bank_Tag,
                                account_id = bankFail.Account_Id,
                                account_name = bankFail.Account_Name,
                                account_type = 0,
                                netno = bankFail.NETNO,
                                mid = bankFail.MID,
                                card_type = bankFail.Card_Type,
                                physical_type = bankFail.Physical_Type,
                                issuer_id = bankFail.Issuer_Id,
                                iccard_no = bankFail.IcCard_No,
                                balance = bankFail.Balance,
                                last_balance = bankFail.Last_Balance,
                                tacno = bankFail.TacNo,
                                psamid = bankFail.PsaMID,
                                psam_tran_sn = bankFail.Psam_Tran_SN,
                                dealstatus = bankFail.DealStatus,
                                medium_type = bankFail.Medium_Type,
                                trans_type = bankFail.Trans_Type,
                                ic_trans_time = bankFail.Ic_Trans_Time,
                                trans_seq = bankFail.Trans_SEQ,
                                ticks_type = bankFail.Ticks_Type,
                                en_network = bankFail.En_NetWork,
                                en_time = bankFail.En_Time,
                                en_plazaid = bankFail.En_Plazaid,
                                en_operator_id = bankFail.En_Operator_Id,
                                en_shift_id = bankFail.En_Shift_Id,
                                network = bankFail.NetWork,
                                plazaid = bankFail.Plazaid,
                                operator_id = bankFail.Operator_Id,
                                shift_id = bankFail.Shift_Id,
                                vehclass = bankFail.VehClass,
                                car_serial = bankFail.Car_Serial,
                                clear_target_date = bankFail.Clear_Target_Date,
                                description = bankFail.Description,
                                cleartargetdate = bankFail.ClearTargetDate,
                                due_fare = bankFail.Due_Fare,
                                cash = bankFail.Cash
                            };
                            oracleAccess.ExecuteSql(sql, param);

                            sql = @"delete from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where id=:id";
                            oracleAccess.ExecuteSql(sql, new { id = bankFail.ID });

                            oracleAccess.CommitTransaction();
                        }
                        catch (Exception ex)
                        {
                            oracleAccess.RollbackTransaction();
                            LogCommon.GetErrorLogInstance().LogError("保证金扣款交易归集定时任务异常：" + ex.ToString());
                            ShowMessage.ShowMsgColor("保证金扣款交易归集定时任务异常", System.Drawing.Color.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("保证金扣款交易归集定时任务异常：" + ex.ToString());
                ShowMessage.ShowMsgColor("保证金扣款交易归集定时任务异常", System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 启动解约信息（2010号报文）处理定时任务4
        /// </summary>
        public void StartCancellationInfoTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_CANCELLATION_INFO_TASK_HEARTBEAT").Value) * 60 * 60 * 1000;// 小时
            while(true)
            {
                ShowMessage.ShowMsgColor("开始处理解约信息定时任务", System.Drawing.Color.Green);
                CancellationInfoTask();
                ShowMessage.ShowMsgColor("解约信息定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_CANCELLATION_INFO_TASK_HEARTBEAT").Value + "小时", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位小时
            }
        }

        /// <summary>
        /// 解约信息（2010号报文）处理定时任务4
        /// </summary>
        private void CancellationInfoTask()
        {
            try
            {
                List<AAccount> accountList = new List<AAccount>();

                List<AAccount> accountListWithFilter = QueryAAccountByBankCode();
                List<AAccount> accountListWithCondition = QueryAAccount();
                if (accountListWithFilter != null && accountListWithFilter.Any())
                {
                    accountList.AddRange(accountListWithFilter);
                }

                if (accountListWithCondition != null && accountListWithCondition.Any())
                {
                    accountList.AddRange(accountListWithCondition);
                }

                List<BankAccountBinding> temp = new List<BankAccountBinding>();
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    foreach (AAccount aaccount in accountList)
                    {
                        List<BankAccountBinding> bankAccountBindingList = QueryBankAccountBindingByAccountNo(aaccount.Account_No);
                        if (bankAccountBindingList == null || !bankAccountBindingList.Any())
                        {
                            UpdateAAccount(oracleAccess, aaccount.Account_No, DateTime.Now);
                            continue;
                        }
                        BankAccountBinding bankAccountBinding = bankAccountBindingList.FirstOrDefault();

                        List<ProxyAccountDetailBankFail> bankFailList = QueryProxyAccountDetailBankFailByAccountNo(bankAccountBinding.account_No);
                        List<ProxyAccountDetailBankFailWaitDeduct> bankFailWaitDeduct = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(bankAccountBinding.account_No);
                        List<BankAccountBinding> babList = QueryBankAccountBindingByBankTagAndAccountId(bankAccountBinding.Bank_Tag, bankAccountBinding.Account_Id);
                        int count = babList.FindAll(p => p.Account_Id == bankAccountBinding.Account_Id && p.Bank_Tag == bankAccountBinding.Bank_Tag && p.Status == 0).Count();

                        DateTime modifyTime = DateTime.Now;
                        if ((bankFailList == null || !bankFailList.Any()) && (bankFailWaitDeduct == null || !bankFailList.Any()) && count == 1)
                        {
                            // 记录a在内存中状态标记为待解约后放入解约集合m中
                            bankAccountBinding.Status = -1;
                            bankAccountBinding.Modify_Time = modifyTime;
                            temp.Add(bankAccountBinding);

                            UpdateBankAccount(bankAccountBinding.Account_Id, oracleAccess);
                            BankAccount bankAccount = QueryBankAccountByBankTagAndAccountId(bankAccountBinding.Account_Id);
                            // 放入解约集合
                            MongoDBAccess<BankAccountCancel> access = new MongoDBAccess<BankAccountCancel>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_CANCEL);
                            string sequence = SYSConstant.GetNextSequence(SYSConstant.currentSequence);
                            SYSConstant.currentSequence = sequence;
                            access.InsertOne(new BankAccountCancel() { _id = bankAccountBinding.Bank_Tag + DateTime.Now.ToString("yyMMddHHmmss") + sequence, BankTag = bankAccountBinding.Bank_Tag, AccountId = bankAccountBinding.Account_Id, AccountName = bankAccount.Account_Name, CashDepositCut = bankAccount.Cash_Deposit, GenTime = modifyTime.AddHours(8), PlateNumbers = "", CreateTime = DateTime.Now.AddHours(8), Command = 0, Status = 0, FileName = "" });
                        }
                        else if ((bankFailList == null || !bankFailList.Any()) && (bankFailWaitDeduct == null || !bankFailWaitDeduct.Any()) && count > 1)
                        {
                            // 记录a在内存中状态标记为待解绑后放入解约集合m中
                            bankAccountBinding.Status = -2;
                            bankAccountBinding.Modify_Time = modifyTime;
                            temp.Add(bankAccountBinding);
                        }
                        else if ((bankFailList != null && bankFailList.Any()) || (bankFailWaitDeduct != null && bankFailWaitDeduct.Any()))
                        {
                            UpdateAAccountCheckFlag(oracleAccess, bankAccountBinding.account_No);
                        }
                    }

                    if (temp.Any())
                    {
                        foreach (var item in temp)
                        {
                            try
                            {
                                oracleAccess.BeginTransaction();
                                if (item.Status == -2)
                                {
                                    item.Modify_Time = DateTime.Now;
                                }
                                UpdateAAccount(oracleAccess, item.account_No, item.Modify_Time);
                                UpdateBankAccountBinding(oracleAccess, item.Id, item.Modify_Time);
                                oracleAccess.CommitTransaction();
                            }
                            catch (Exception ex)
                            {
                                oracleAccess.RollbackTransaction();
                                LogCommon.GetErrorLogInstance().LogError("解约信息（2010号报文）处理定时任务异常：" + ex.ToString());
                                ShowMessage.ShowMsgColor("解约信息（2010号报文）处理定时任务异常：", System.Drawing.Color.Red);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("解约信息（2010号报文）处理定时任务异常：" + ex.ToString());
                ShowMessage.ShowMsgColor("解约信息（2010号报文）处理定时任务异常：", System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 启动保证金金额减少信息（2012号报文）处理定时任务
        /// </summary>
        public void StartProcessDepositAmountReduceTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_DEPOSIT_AMOUNT_REDUCE_TASK_HEARTBEAT").Value) * 60 * 60 * 1000;// 小时
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理保证金金额减少信息（2012号报文）定时任务", System.Drawing.Color.Green);
                DepositAmountReduceTask();
                ShowMessage.ShowMsgColor("保证金金额减少信息（2012号报文）定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_DEPOSIT_AMOUNT_REDUCE_TASK_HEARTBEAT").Value + "小时", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位小时
            }
        }

        /// <summary>
        /// 保证金金额减少信息（2012号报文）处理定时任务5
        /// </summary>
        private void DepositAmountReduceTask()
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    try
                    {
                        List<RemoveCarBinding> removeCarBindingList = QueryRemoveCarBinding();
                        List<RemoveCarBinding> rcbList = QueryRemoveCarBindingWithCondition();
                        ShowMessage.ShowMsgColor("获得解绑车辆信息" + removeCarBindingList.Count + rcbList.Count() + "条", System.Drawing.Color.Green);

                        foreach (RemoveCarBinding removeCarBinding in rcbList)
                        {
                            oracleAccess.BeginTransaction();
                            List<ProxyAccountDetailBankFail> proxyAccountDetailBankFailList = QueryProxyAccountDetailBankFailByCardNo(removeCarBinding.Card_No);
                            List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByCardNo(removeCarBinding.Card_No);
                            bool haveBankFail = proxyAccountDetailBankFailList != null && proxyAccountDetailBankFailList.Any();
                            bool haveBankFailWaitDeduct = proxyAccountDetailBankFailWaitDeductList != null && proxyAccountDetailBankFailWaitDeductList.Any();
                            DateTime modifyTime = DateTime.Now;
                            if (haveBankFail || haveBankFailWaitDeduct)
                            {

                            }
                            else
                            {
                                removeCarBinding.Status = 1;
                                removeCarBinding.Modify_Time = modifyTime;
                                MongoDBAccess<BankAccountCancel> access = new MongoDBAccess<BankAccountCancel>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_CANCEL);
                                string sequence = SYSConstant.GetNextSequence(SYSConstant.currentSequence);
                                SYSConstant.currentSequence = sequence;
                                access.InsertOne(new BankAccountCancel() { _id = removeCarBinding.Bank_Tag + DateTime.Now.ToString("yyMMddHHmmss") + sequence, BankTag = removeCarBinding.Bank_Tag, AccountId = removeCarBinding.Account_Id, AccountName = removeCarBinding.Account_Name, CashDepositCut = removeCarBinding.Cash_Deposit_Cut, GenTime = modifyTime.AddHours(8), PlateNumbers = removeCarBinding.Plate_Numbers, CreateTime = DateTime.Now.AddHours(8), Command = 1, Status = removeCarBinding.Status, FileName = "" });
                                UpdateBankAccount(removeCarBinding.Account_Id, removeCarBinding.Bank_Tag, removeCarBinding.Cash_Deposit_Cut, oracleAccess);
                                UpdateRemoveCarBinding(removeCarBinding, oracleAccess);
                            }
                            oracleAccess.CommitTransaction();
                        }

                        foreach (RemoveCarBinding removeCarBinding in removeCarBindingList)
                        {
                            oracleAccess.BeginTransaction();
                            List<ProxyAccountDetailBankFail> proxyAccountDetailBankFailList = QueryProxyAccountDetailBankFailByCardNo(removeCarBinding.Card_No);
                            List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByCardNo(removeCarBinding.Card_No);

                            bool haveBankFail = proxyAccountDetailBankFailList != null && proxyAccountDetailBankFailList.Any();
                            bool haveBankFailWaitDeduct = proxyAccountDetailBankFailWaitDeductList != null && proxyAccountDetailBankFailWaitDeductList.Any();
                            DateTime modifyTime = DateTime.Now;
                            if (haveBankFail || haveBankFailWaitDeduct)
                            {
                                removeCarBinding.Status = 2;
                                removeCarBinding.Modify_Time = modifyTime;
                                UpdateRemoveCarBinding(removeCarBinding, oracleAccess);
                            }
                            else
                            {
                                removeCarBinding.Status = 1;
                                removeCarBinding.Modify_Time = modifyTime;
                                MongoDBAccess<BankAccountCancel> access = new MongoDBAccess<BankAccountCancel>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_CANCEL);
                                string sequence = SYSConstant.GetNextSequence(SYSConstant.currentSequence);
                                SYSConstant.currentSequence = sequence;
                                access.InsertOne(new BankAccountCancel() { _id = removeCarBinding.Bank_Tag + DateTime.Now.ToString("yyMMddHHmmss") + sequence, BankTag = removeCarBinding.Bank_Tag, AccountId = removeCarBinding.Account_Id, AccountName = removeCarBinding.Account_Name, CashDepositCut = removeCarBinding.Cash_Deposit_Cut, GenTime = modifyTime.AddHours(8), PlateNumbers = removeCarBinding.Plate_Numbers, CreateTime = DateTime.Now.AddHours(8), Command = 1, Status = removeCarBinding.Status, FileName = "" });
                                UpdateBankAccount(removeCarBinding.Account_Id, removeCarBinding.Bank_Tag, removeCarBinding.Cash_Deposit_Cut, oracleAccess);
                                UpdateRemoveCarBinding(removeCarBinding, oracleAccess);
                            }
                            oracleAccess.CommitTransaction();
                        }
                    }
                    catch (Exception ex)
                    {
                        oracleAccess.RollbackTransaction();
                        ShowMessage.ShowMsgColor("保证金金额减少信息（2012号报文）处理定时任务异常", System.Drawing.Color.Red);
                        LogCommon.GetErrorLogInstance().LogError("保证金金额减少信息（2012号报文）处理定时任务异常：" + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor("保证金金额减少信息（2012号报文）处理定时任务异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("保证金金额减少信息（2012号报文）处理定时任务异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// 启动银行账号与ETC卡绑定信息（2008号报文）定时处理任务
        /// </summary>
        public void StartProcessBankAccountBingETCTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_BANK_ACCOUNT_BINGETC_TASK_HEARTBEAT").Value) * 60 * 60 * 1000;// 小时
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理银行账号与ETC卡绑定信息（2008号报文）定时处理任务", System.Drawing.Color.Green);
                BankAccountBingETCTask();
                ShowMessage.ShowMsgColor("银行账号与ETC卡绑定信息（2008号报文）定时处理任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_BANK_ACCOUNT_BINGETC_TASK_HEARTBEAT").Value + "小时", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位小时
            }
        }

        /// <summary>
        /// 银行账号与ETC卡绑定信息（2008号报文）定时处理任务6
        /// </summary>
        public void BankAccountBingETCTask()
        {
            try
            {
                List<BankAgent> bankAgentList = QueryAllBankAgent().FindAll(p => p.BankSupportTasks.Any(o => o.File_Task_Type == 2008));// 获取所有支持2008号报文的银行基础信息
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    foreach (BankAgent bankAgent in bankAgentList)
                    {
                        string collectionName = string.Format("{0}_{1}{2}{3}", (int)ETransType.银行账号与ETC卡绑定信息, SYSConstant.SettleCenterCode, bankAgent.Bank_Code, DateTime.Now.ToString("yyyyMMddHHmmss"));
                        MongoDBAccess<BankAccountidCardNo> cardMongoAccess = new MongoDBAccess<BankAccountidCardNo>(SYSConstant.BANK_ACCOUNTID_CARDNO, collectionName);
                        MongoDBAccess<OutPutTaskWaitingDone> outPutTaskMongoAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                        IDataReader myReader;
                        string sql = @"select a.CARD_NO,a.CARD_STATUS,d.ACCOUNT_ID,d.BANK_TAG from " + DBConstant.CARD_ACCOUNT + " a inner join " + DBConstant.AAccount + @" b on a.account_no=b.account_no inner join  
" + DBConstant.BANK_ACCOUNT_BINDING + " c on a.account_no=c.account_no inner join " + DBConstant.BANK_ACCOUNT + " d on c.account_id=d.account_id where c.bank_tag='" + bankAgent.Bank_Tag + "' and c.status=0";
                        oracleAccess.CommandText = sql;
                        myReader = oracleAccess.ExecuteReader();
                        try
                        {
                            while (myReader.Read())
                            {
                                BankAccountidCardNo cardNo = new BankAccountidCardNo();
                                string sequence = SYSConstant.GetNextSequence(SYSConstant.currentSequence);
                                SYSConstant.currentSequence = sequence;
                                cardNo._id = myReader["BANK_TAG"].ToString() + DateTime.Now.ToString("yyMMddHHmmss") + sequence;
                                cardNo.AccountId = myReader["ACCOUNT_ID"].ToString();
                                cardNo.CardStatus = myReader["CARD_STATUS"].ToString();
                                cardNo.JtcardId = myReader["CARD_NO"].ToString();
                                cardMongoAccess.InsertOne(cardNo);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            myReader.Close();
                        }
                        OutPutTaskWaitingDone outPutTask = new OutPutTaskWaitingDone();
                        outPutTask.BankTag = bankAgent.Bank_Tag;
                        outPutTask.ColName = collectionName;
                        outPutTask.CreateTime = DateTime.Now.AddHours(8);
                        outPutTask.TransType = (int)ETransType.银行账号与ETC卡绑定信息;
                        outPutTaskMongoAccess.InsertOne(outPutTask);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor("银行账号与ETC卡绑定信息（2008号报文）定时处理任务异常", System.Drawing.Color.Red);
                LogCommon.GetErrorLogInstance().LogError("银行账号与ETC卡绑定信息（2008号报文）定时处理任务异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// 根据BankCode查询account表
        /// </summary>
        /// <returns></returns>
        public List<AAccount> QueryAAccountByBankCode()
        {
            try
            {
                // 资金争议期
                int CapitalDisputePeriod = int.Parse(SYSConstant.sParam.Find(p => p.Key == "CANCELLATIONCAPITALDISPUTEPERIOD").Value);
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select a.* from " + DBConstant.AAccount + " a inner join " + DBConstant.ClearAccountCheckInfos + " b on a.ACCOUNT_NO= b.ACCOUNT_NO where trunc(sysdate)-trunc(b.OPERATION_TIME)>=:CapitalDisputePeriod and trunc(sysdate)-trunc(b.OPERATION_TIME)<=:CDPAfter and a.DEPOSIT_TYPE=3 and a.CHECK_FLAG=-2 ");
                    return oracleAccess.QuerySql<AAccount>(sql, new { CapitalDisputePeriod = CapitalDisputePeriod, CDPAfter = CapitalDisputePeriod + int.Parse(SYSConstant.sParam.Find(p => p.Key == "CANCELLATIONCAPITALDISPUTEPERIODPLUS").Value) });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 联表查询A_ACCOUNT、ISSUE.CLEAR_ACCOUNT_CHECK_INFOS
        /// </summary>
        /// <returns>List<AAccount></returns>
        public List<AAccount> QueryAAccount()
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select a.* from " + DBConstant.AAccount + " a inner join " + DBConstant.ClearAccountCheckInfos + " b on a.ACCOUNT_NO= b.ACCOUNT_NO where a.DEPOSIT_TYPE=3 and a.CHECK_FLAG=-3 ");
                    return oracleAccess.QuerySql<AAccount>(sql, null);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据ACCOUNT_NO更新A_ACCOUNT.ACCOUNT_STATUS=1，CHECK_FLAG=0
        /// </summary>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <param name="accountNo">accountNo</param>
        /// <returns>bool</returns>
        public bool UpdateAAccount(OracleDBAccess oracleAccess, string accountNo, DateTime modifyTime)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.AAccount + " SET ACCOUNT_STATUS=1 , CHECK_FLAG=0, MODIFY_DATE=:MODIFY_DATE where ACCOUNT_NO=:ACCOUNT_NO";
                var param = new { ACCOUNT_NO = accountNo, MODIFY_DATE = modifyTime };
                return oracleAccess.ExecuteSql(sql, param) > 0;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 根据ACCOUNT_NO更新A_ACCOUNT
        /// </summary>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <param name="accountNo">accountNo</param>
        /// <returns>bool</returns>
        public bool UpdateAAccountCheckFlag(OracleDBAccess oracleAccess, string accountNo)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.AAccount + " SET CHECK_FLAG=-3 where ACCOUNT_NO=:ACCOUNT_NO";
                var param = new { ACCOUNT_NO = accountNo };
                return oracleAccess.ExecuteSql(sql, param) > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据ID更新BANK_ACCOUNT_BINDING.STATUS=1
        /// </summary>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <param name="id">id</param>
        /// <param name="modifyTime">modifyTime</param>
        /// <returns>bool</returns>
        public bool UpdateBankAccountBinding(OracleDBAccess oracleAccess, int id, DateTime modifyTime)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.BANK_ACCOUNT_BINDING + " SET STATUS=1 , MODIFY_TIME=:MODIFY_TIME where id=:id";
                var param = new { MODIFY_TIME = modifyTime, id = id };
                return oracleAccess.ExecuteSql(sql, param) > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据ACCOUNT_NO检索BANK_ACCOUNT_BINDING表已签约的记录
        /// </summary>
        /// <param name="accountNo">ACCOUNT_NO</param>
        /// <returns>List<BankAccountBinding></returns>
        public List<BankAccountBinding> QueryBankAccountBindingByAccountNo(string accountNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select * from " + DBConstant.BANK_ACCOUNT_BINDING + " where ACCOUNT_NO=:ACCOUNT_NO and STATUS=0");
                    return oracleAccess.QuerySql<BankAccountBinding>(sql, new { ACCOUNT_NO = accountNo });
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 根据BankTagAndAccountId检索BANK_ACCOUNT_BINDING表已签约的记录
        /// </summary>
        /// <param name="BankTag">BankTag</param>
        /// <param name="AccountId">AccountId</param>
        /// <returns>List<BankAccountBinding></returns>
        public List<BankAccountBinding> QueryBankAccountBindingByBankTagAndAccountId(string BankTag, string AccountId)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select * from " + DBConstant.BANK_ACCOUNT_BINDING + " where ACCOUNT_ID=:ACCOUNT_ID and BANK_TAG=:BANK_TAG and STATUS=0");
                    return oracleAccess.QuerySql<BankAccountBinding>(sql, new { BANK_TAG = BankTag, ACCOUNT_ID = AccountId });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// 根据BankTagAndAccountId检索BANK_ACCOUNT表
        /// </summary>
        /// <param name="AccountId"></param>
        /// <returns></returns>
        public BankAccount QueryBankAccountByBankTagAndAccountId(string AccountId)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select * from " + DBConstant.BANK_ACCOUNT + " where ACCOUNT_ID=:ACCOUNT_ID");
                    return oracleAccess.QuerySql<BankAccount>(sql, new { ACCOUNT_ID = AccountId }).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 检索RemoveCarBinding表已预约解绑的记录
        /// </summary>
        public List<RemoveCarBinding> QueryRemoveCarBinding()
        {
            try
            {
                // 资金争议期
                int CapitalDisputePeriod = int.Parse(SYSConstant.sParam.Find(p => p.Key == "CANCELLATIONCAPITALDISPUTEPERIOD").Value);

                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select * from " + DBConstant.REMOVE_CAR_BINDING + " where trunc(sysdate)-trunc(MODIFY_TIME)>=:CapitalDisputePeriod and trunc(sysdate)-trunc(MODIFY_TIME)<=:CDPAfter and STATUS=0");
                    return oracleAccess.QuerySql<RemoveCarBinding>(sql, new { CapitalDisputePeriod = CapitalDisputePeriod, CDPAfter = CapitalDisputePeriod + int.Parse(SYSConstant.sParam.Find(p => p.Key == "CANCELLATIONCAPITALDISPUTEPERIODPLUS").Value) });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 检索RemoveCarBinding表已预约解绑的记录
        /// </summary>
        /// <returns>List<RemoveCarBinding></returns>
        public List<RemoveCarBinding> QueryRemoveCarBindingWithCondition()
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"select * from " + DBConstant.REMOVE_CAR_BINDING + " where STATUS=2");
                    return oracleAccess.QuerySql<RemoveCarBinding>(sql, null);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据交易记录的ETC卡号检索PROXY_ACCOUNT_DETAIL_BANKFAIL
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        public List<ProxyAccountDetailBankFail> QueryProxyAccountDetailBankFailByCardNo(string cardNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where card_no=:cardNo");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFail>(sql, new { cardNo = cardNo });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据ETC账号检索PAD_BANKFAIL
        /// </summary>
        /// <param name="accountNo">ETC账号</param>
        /// <returns></returns>
        public List<ProxyAccountDetailBankFail> QueryProxyAccountDetailBankFailByAccountNo(string accountNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where ACCOUNT_NO=:accountNo");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFail>(sql, new { accountNo = accountNo });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据ETC账号检索PAD_BANKFAILWaitDeduct
        /// </summary>
        /// <param name="accountNo">ETC账号</param>
        /// <returns></returns>
        public List<ProxyAccountDetailBankFailWaitDeduct> QueryProxyAccountDetailBankFailWaitDeductByAccountNo(string accountNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where ACCOUNT_NO=:accountNo");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFailWaitDeduct>(sql, new { accountNo = accountNo });
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// 根据ETC卡号检索PAD_BANKFAILWaitDeduct
        /// </summary>
        /// <param name="cardNo">ETC卡号</param>
        /// <returns></returns>
        public List<ProxyAccountDetailBankFailWaitDeduct> QueryProxyAccountDetailBankFailWaitDeductByCardNo(string cardNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where Card_NO=:cardNo");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFailWaitDeduct>(sql, new { cardNo = cardNo });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新RemoveCarBinding表
        /// </summary>
        /// <param name="removeCarBinding"></param>
        /// <param name="oracleAccess"></param>
        /// <returns></returns>
        public bool UpdateRemoveCarBinding(RemoveCarBinding removeCarBinding, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = string.Format(@"UPDATE " + DBConstant.REMOVE_CAR_BINDING + " SET STATUS=:STATUS ,MODIFY_TIME=:MODIFY_TIME where id=:id");
                return oracleAccess.ExecuteSql(sql, new { STATUS = removeCarBinding.Status, MODIFY_TIME = DateTime.Now, id = removeCarBinding.Id }) > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新核心库银行签约账户表ISSUE.BANK_ACCOUNT对应记录的保证金金额
        /// </summary>
        /// <param name="accountId">accountId</param>
        /// <param name="bankTag">bankTag</param>
        /// <param name="cashDeposit">cashDeposit</param>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <returns></returns>
        public int UpdateBankAccount(string accountId,string bankTag,decimal CashDepositCut, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.BANK_ACCOUNT + " set Cash_Deposit=Cash_Deposit-:CashDepositCut,MODIFY_TIME=sysdate where Bank_Tag=:BankTag and Account_Id=:AccountId";
                return oracleAccess.ExecuteSql(sql, new { CashDepositCut = CashDepositCut, BankTag = bankTag, AccountId = accountId });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新核心库银行签约账户表ISSUE.BANK_ACCOUNT对应记录的保证金金额
        /// </summary>
        /// <param name="accountId">accountId</param>
        /// <param name="CashDepositCut">CashDepositCut</param>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <returns>int</returns>
        public int UpdateBankAccount(string accountId, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.BANK_ACCOUNT + " set Cash_Deposit=0,MODIFY_TIME=sysdate where Account_Id=:AccountId";
                return oracleAccess.ExecuteSql(sql, new { AccountId = accountId });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
