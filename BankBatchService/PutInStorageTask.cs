using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// 入库方向定时任务
    /// </summary>
    public class PutInStorageTask
    {
        /// <summary>
        /// 启动记账金客户转账（2001号文件扣款结果）数据处理定时任务
        /// </summary>
        public void StartProcessAccountTransferDataTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_ACCOUNT_TRANSFER_DATA_TASK_HEARTBEAT").Value) * 60 * 1000;// 单位分
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理记账金客户转账（2001号文件扣款结果）数据处理定时任务", System.Drawing.Color.Green);
                AccountTransferDataTask();
                ShowMessage.ShowMsgColor("记账金客户转账（2001号文件扣款结果）数据处理定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_ACCOUNT_TRANSFER_DATA_TASK_HEARTBEAT").Value + "分钟", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位分
            }
        }

        /// <summary>
        /// 记账金客户转账（2001号文件扣款结果）数据处理定时任务1号
        /// 定时线程i每10分钟启动
        /// </summary>
        public void AccountTransferDataTask()
        {
            try
            {
                MongoDBAccess<InputTaskWaitingDone> dbAccess = new MongoDBAccess<InputTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                // 数据m
                List<InputTaskWaitingDone> waitingDoneList = dbAccess.FindAsByWhere(p => p.TransType == (int)ETransType.记账金客户转账结果数据 && p.Status == 0, 0);
                foreach (var waitingDone in waitingDoneList)
                {
                    // 根据COLNAME集合名获取对应集合中的所有交易，每条交易一个事务逐一处理
                    MongoDBAccess<TransactionInfo> access = new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, waitingDone.ColName);
                    List<TransactionInfo> infoList = access.FindAsByWhere(p => !string.IsNullOrEmpty(p.ACBAccount), 0);
                    if (infoList == null || !infoList.Any())
                    {
                        LogCommon.GetErrorLogInstance().LogError("记账金客户转账银行扣款结果处理异常：集合名" + waitingDone.ColName + "未取到交易数据");
                        ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：集合名" + waitingDone.ColName + "未取到交易数据", System.Drawing.Color.Red);
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<InputTaskWaitingDone>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1);
                        dbAccess.UpdateDocs(p => p.ColName == waitingDone.ColName, mUpdateDefinition);
                        continue;
                    }
                    // 是否全部处理成功标记
                    bool isAllSuccess = true;

                    foreach (var transactionInfo in infoList)
                    {
                        // 每条交易一个事务逐一处理
                        using (OracleDBAccess oracleAccess = new OracleDBAccess())
                        {
                            #region MyRegion

                            try
                            {
                                oracleAccess.BeginTransaction();
                                // 根据交易流水号检索PROXY_ACCOUNT_DETAIL表
                                var accountDetails = QueryProxyAccountDetailById(transactionInfo._id.ToString());
                                bool haveAccountDetail = accountDetails != null && accountDetails.Any();
                                if (!haveAccountDetail)
                                {
                                    // 报错，记录日志
                                    LogCommon.GetInfoLogInstance().LogError("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失");
                                    //ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失", System.Drawing.Color.Yellow);
                                    continue;
                                }
                                if (haveAccountDetail && accountDetails[0].ChargeStatus == 2)
                                {
                                    LogCommon.GetInfoLogInstance().LogError("记账金客户转账银行扣款结果处理异常：流水号：" + transactionInfo._id + "扣款结果系统已成功处理，此条为银行重复返回数据。");
                                    ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：流水号：" + transactionInfo._id + "扣款结果系统已成功处理，此条为银行重复返回数据。", System.Drawing.Color.Yellow);
                                    continue;
                                }

                                // 根据流水号检索PROXY_ACCOUNT_DETAIL_BANKFAIL
                                var bankFailList = QueryProxyAccountDetailBankFailById(transactionInfo._id);
                                bool haveBankFailList = bankFailList != null && bankFailList.Any();

                                // 根据交易记录的ETC账号检索PROXY_ACCOUNT_DETAIL_BANKFAIL
                                //List<ProxyAccountDetailBankFail> bankFailListByAccountNo = null;
                                //bool haveBankFailListByAccountNo = false;
                                //if (haveAccountDetail)
                                //{
                                //    bankFailListByAccountNo = QueryProxyAccountDetailBankFailByAccountNo(accountDetails[0].Account_No);

                                //    // 除自身以外不存在其他记录
                                //    haveBankFailListByAccountNo = bankFailListByAccountNo != null && bankFailListByAccountNo.Any() && bankFailListByAccountNo.Count > 1;
                                //}

                                // 若交易银行扣款成功
                                if (transactionInfo.Result == 0)
                                {

                                    // 若PROXY_ACCOUNT_DETAIL_BANKFAIL存在记录
                                    if (haveBankFailList)
                                    {
                                        // 1）也取到了交易数据
                                        if (haveAccountDetail)
                                        {
                                            // 说明是银行扣款二次成功的交易，可能是历史数据二次扣款成功，数据记入PROXY_ACCOUNT_DETAIL_BANKFAILTOSUCCESS（ACCOUNT_DETAIL_OUT_ID赋值-1）
                                            ProxyAccountDetail failToSuccess = accountDetails[0];
                                            failToSuccess.Account_Detail_Out_Id = -1;
                                            failToSuccess.ChargeStatus = 2;
                                            failToSuccess.Bank_Charge_Time = transactionInfo.BankChargeTime;
                                            InsertIntoProxyAccountDetailBankFailToSuccess(failToSuccess, oracleAccess);

                                            //检索PAD_BANKFAIL和PAD_BANKFAILWAITDEDUCT
                                            //List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(accountDetails[0].Account_No);
                                            //bool haveBankFailWaitDeductByAccountNo = proxyAccountDetailBankFailWaitDeductList != null && proxyAccountDetailBankFailWaitDeductList.Any() && proxyAccountDetailBankFailWaitDeductList.Count > 1;
                                            //if (haveBankFailListByAccountNo && haveBankFailWaitDeductByAccountNo)
                                            //{

                                            //}
                                            //else
                                            //{
                                            //    // 若不存在记录则向ECDBA.FL_ACCOUNT_STALIST插入一条ISSUE_STATUS等于0的记录
                                            //    InsertIntoFlAccountStalist(new FlAccountStalist() { Account_No = accountDetails[0].Account_No, Package_No = accountDetails[0].Package_No, Start_Time = DateTime.Now, Issue_Status = 0 }, oracleAccess);
                                            //}
                                        }
                                        else
                                        {
                                            // 该交易是系统切换前的遗留交易，记入PROXY_ACCOUNT_DETAIL_SUCCESSNONE
                                            //InsertIntoProxyAccountDetailSuccessNone(accountDetails[0], oracleAccess);

                                            // 报错，记录日志
                                            LogCommon.GetInfoLogInstance().LogError("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失");
                                            ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失", System.Drawing.Color.Yellow);
                                        }
                                    }
                                    else
                                    {
                                        // 不存在记录且1）取到了交易数据
                                        if (haveAccountDetail)
                                        {
                                            /*银行扣款一次成功的交易*/

                                            // 数据记入PROXY_ACCOUNT_DETAIL_TOCLEAR（ACCOUNT_DETAIL_OUT_ID赋值0）
                                            ProxyAccountDetail detailToClear = accountDetails[0];// 待
                                            detailToClear.Account_Detail_Out_Id = 0;
                                            InsertIntoProxyAccountDetailToClear(detailToClear, transactionInfo.BankChargeTime, 2, oracleAccess);

                                            // 根据交易流水号更新PROXY_ACCOUNT_DETAIL
                                            UpdateProxyAccountDetail(transactionInfo.BankChargeTime, accountDetails[0].ID, 2);

                                            // 交易是否是本地路方交易
                                            if (IsLocalServiceProvider(accountDetails[0].Provider_Id))
                                            {
                                                //T_DISPUTE_DATA中没有该笔交易
                                                List<dynamic> disputeDataList = T_DISPUTE_DATA(accountDetails[0].Provider_Id, accountDetails[0].Package_No, accountDetails[0].TransId);
                                                if (disputeDataList == null || !disputeDataList.Any())
                                                {
                                                    // 该笔交易则数据同时写入FL_CHARGE表（金额字段填为交易金额单位分，状态为记账成功0）、T_TRANSACTIO	N数据表
                                                    //InsertIntoFlCharge(oracleAccess, accountDetails[0], 0);
                                                    UpdateMsgSndAmount(oracleAccess, accountDetails[0].Operator_Id, decimal.Parse((accountDetails[0].Cash / 100).ToString("F2")));
                                                    InsertIntoTtransaction(oracleAccess, accountDetails[0]);
                                                }
                                            }

                                            // 检索PAD_BANKFAIL和PAD_BANKFAILWAITDEDUCT
                                            //List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(accountDetails[0].Account_No);
                                            //if (haveBankFailListByAccountNo && proxyAccountDetailBankFailWaitDeductList != null && proxyAccountDetailBankFailWaitDeductList.Any())
                                            //{

                                            //}
                                            //else
                                            //{
                                            //    // 若不存在记录则向ECDBA.FL_ACCOUNT_STALIST插入一条ISSUE_STATUS等于0的记录
                                            //    InsertIntoFlAccountStalist(new FlAccountStalist() { Account_No = accountDetails[0].Account_No, Package_No = accountDetails[0].Package_No, Start_Time = DateTime.Now, Issue_Status = 0 }, oracleAccess);
                                            //}
                                        }
                                        else
                                        {
                                            // 若不存在记录且1）也未取到交易数据，说明该交易是系统切换前的遗留交易，记入PROXY_ACCOUNT_DETAIL_SUCCESSNONE
                                            //InsertIntoProxyAccountDetailBankFail(accountDetails[0], oracleAccess);

                                            // 报错，记录日志
                                            LogCommon.GetInfoLogInstance().LogError("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失");
                                            ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失", System.Drawing.Color.Yellow);
                                        }
                                    }
                                }
                                else
                                {
                                    /*交易银行扣款失败*/

                                    // 若记录已存在则返回继续处理下一交易
                                    if (haveBankFailList)
                                    {
                                        if (accountDetails[0].ChargeStatus == 5)
                                        {
                                            UpdateProxyAccountDetailBankFail(transactionInfo._id, transactionInfo.BankChargeTime, oracleAccess);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // 若记录不存在根据1）的结果将数据插入PROXY_ACCOUNT_DETAIL_BANKFAIL
                                        if (haveAccountDetail)
                                        {
                                            if (accountDetails[0].ChargeStatus == 5)
                                            {
                                                LogCommon.GetInfoLogInstance().LogError("记账金客户转账银行扣款结果处理异常：银行账单号为" + accountDetails[0].Account_Id + "的交易在PROXY_ACCOUNT_DETAIL中已为扣款失败交易");
                                                ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：银行账单号为" + accountDetails[0].Account_Id + "的交易在PROXY_ACCOUNT_DETAIL中已为扣款失败交易", System.Drawing.Color.Yellow);
                                                continue;
                                            }
                                            accountDetails[0].MID = transactionInfo.Result.ToString();
                                            accountDetails[0].Bank_Charge_Time = transactionInfo.BankChargeTime;
                                            InsertIntoProxyAccountDetailBankFail(accountDetails[0], oracleAccess);
                                            UpdateProxyAccountDetail(transactionInfo.BankChargeTime, accountDetails[0].ID, 5);
                                            InsertIntoProxyAccountDetailToClear(accountDetails[0], transactionInfo.BankChargeTime, 5, oracleAccess);

                                            // 交易是否是本地路方交易
                                            if (IsLocalServiceProvider(accountDetails[0].Provider_Id))
                                            {
                                                // T_DISPUTE_DATA中没有该笔交易
                                                List<dynamic> disputeDataList = T_DISPUTE_DATA(accountDetails[0].Provider_Id, accountDetails[0].Package_No, accountDetails[0].TransId);
                                                if (disputeDataList == null || !disputeDataList.Any())
                                                {
                                                    // 插入T_DISPUTE_DATA、T_DISPUTE_DETAIL、FL_CHARGE表（金额字段填为0，STATUS等于新增的争议类型银行扣款失败99！）
                                                    InsertIntoTdisputeData(oracleAccess, accountDetails[0]);
                                                    InsertIntoTdisputeDetail(oracleAccess, accountDetails[0]);
                                                    //InsertIntoFlCharge(oracleAccess, accountDetails[0], 99);
                                                    UpdateMsgSnd(oracleAccess, accountDetails[0].Operator_Id);
                                                }
                                            }

                                            // 检索PAD_BANKFAIL和PAD_BANKFAILWAITDEDUCT
                                            List<ProxyAccountDetailBankFail> padBankFailList = QueryProxyAccountDetailBankFailByAccountNo(accountDetails[0].Account_No);
                                            List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(accountDetails[0].Account_No);
                                            if ((padBankFailList != null && padBankFailList.Any()) || (proxyAccountDetailBankFailWaitDeductList != null && proxyAccountDetailBankFailWaitDeductList.Any()))
                                            {
                                            }
                                            else
                                            {
                                                InsertIntoFlAccountStalist(new FlAccountStalist() { Account_No = accountDetails[0].Account_No, Package_No = accountDetails[0].Package_No, Start_Time = DateTime.Now, Issue_Status = 2, Remark = accountDetails[0].MID, Agent_No = accountDetails[0].Bank_Tag }, oracleAccess);
                                            }
                                        }
                                        else
                                        {
                                            // 为空说明是系统切换前的遗留数据，也插入BANKFAIL表！
                                            //InsertIntoProxyAccountDetailBankFail(accountDetails[0], oracleAccess);

                                            // 报错，记录日志
                                            LogCommon.GetInfoLogInstance().LogError("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失");
                                            ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：遗留扣款失败交易数据遗失", System.Drawing.Color.Yellow);
                                        }

                                        //if (!haveBankFailListByCardNo)
                                        //{
                                        //    // 若不存在记录则向ECDBA.FL_ACCOUNT_STALIST插入一条ISSUE_STATUS等于2的记录
                                        //    InsertIntoFlAccountStalist(new FlAccountStalist() { Account_No = accountDetails[0].Account_No, Package_No = accountDetails[0].Package_No, Start_Time = DateTime.Now, Issue_Status = 2 }, oracleAccess);
                                        //}
                                    }
                                }
                                oracleAccess.CommitTransaction();
                            }
                            catch (Exception ex)
                            {
                                if (oracleAccess.HandleOracleException(ex) != ReturnResult.OracleUniqueConstraintException)
                                {
                                    isAllSuccess = false;
                                    ShowMessage.ShowMsgColor("流水号" + transactionInfo._id + "记账金客户转账（2001号文件扣款结果）数据处理定时任务处理异常", System.Drawing.Color.Red);
                                    LogCommon.GetErrorLogInstance().LogError("流水号" + transactionInfo._id + "记账金客户转账（2001号文件扣款结果）数据处理定时任务处理异常：" + ex.ToString());
                                }
                                else
                                {
                                    ShowMessage.ShowMsgColor("流水号" + transactionInfo._id + "记账金客户转账（2001号文件扣款结果）数据处理定时任务主键冲突", System.Drawing.Color.Yellow);
                                    LogCommon.GetInfoLogInstance().LogError("流水号" + transactionInfo._id + "记账金客户转账（2001号文件扣款结果）数据处理定时任务主键冲突");
                                }
                                oracleAccess.RollbackTransaction();
                            }
                            #endregion
                        }
                    }

                    // 全部成功则更新STATUS为1
                    if (isAllSuccess)
                    {
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<InputTaskWaitingDone>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1);
                        if (dbAccess.UpdateDocs(p => p.ColName == waitingDone.ColName, mUpdateDefinition) > 0)
                        {
                            // 交易全部已成功同步到核心）则drop掉COLNAME所对应的集合
                            access.DropCollection(waitingDone.ColName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 滚屏日志和本地文件日志
                LogCommon.GetErrorLogInstance().LogError("记账金客户转账银行扣款结果处理异常：" + ex.ToString());
                ShowMessage.ShowMsgColor("记账金客户转账银行扣款结果处理异常：" + ex.ToString(), System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 启动记账金保证金转账（2002号文件扣款结果）数据处理定时任务
        /// </summary>
        public void StartProcessDepositTransferTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_DEPOSIT_TRANSFER_TASK_HEARTBEAT").Value) * 60 * 1000; // 单位分
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理记账金保证金转账（2002号文件扣款结果）数据处理定时任务", System.Drawing.Color.Green);
                DepositTransferTask();
                ShowMessage.ShowMsgColor("记账金保证金转账（2002号文件扣款结果）数据处理定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_DEPOSIT_TRANSFER_TASK_HEARTBEAT").Value + "分钟", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位分钟
            }
        }

        /// <summary>
        /// 记账金保证金转账（2002号文件扣款结果）数据处理定时任务2
        /// </summary>
        public void DepositTransferTask()
        {
            try
            {
                MongoDBAccess<InputTaskWaitingDone> dbAccess = new MongoDBAccess<InputTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                List<InputTaskWaitingDone> waitingDoneList = dbAccess.FindAsByWhere(p => p.TransType == (int)ETransType.记账保证金转账结果数据 && p.Status == 0, 0);
                if (waitingDoneList == null || !waitingDoneList.Any())
                {
                    ShowMessage.ShowMsgColor("未获取到记账金保证金转账银行扣款结果", System.Drawing.Color.Yellow);
                    LogCommon.GetInfoLogInstance().LogError("未获取到记账金保证金转账银行扣款结果");
                    return;
                }

                foreach (var waitingDone in waitingDoneList)
                {
                    MongoDBAccess<TransactionInfo> access = new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, waitingDone.ColName);
                    List<TransactionInfo> infoList = access.FindAsByWhere(p => !string.IsNullOrEmpty(p.ACBAccountN), 0);
                    if (infoList == null || !infoList.Any())
                    {
                        ShowMessage.ShowMsgColor("集合名" + waitingDone.ColName + "未获取到记账金保证金转账银行扣款结果", System.Drawing.Color.Yellow);
                        LogCommon.GetInfoLogInstance().LogError("集合名" + waitingDone.ColName + "未获取到记账金保证金转账银行扣款结果");
                        continue;
                    }

                    using (OracleDBAccess oracleAccess = new OracleDBAccess())
                    {
                        // 是否全部处理成功标记
                        bool isAllSuccess = true;
                        foreach (var transactionInfo in infoList)
                        {
                            try
                            {
                                oracleAccess.BeginTransaction();
                                var accountDetails = QueryProxyAccountDetailById(transactionInfo._id.ToString());
                                if (accountDetails != null && accountDetails.Any())
                                {
                                    if (accountDetails[0].ChargeStatus == 2)
                                    {
                                        LogCommon.GetInfoLogInstance().LogError("流水号：" + transactionInfo._id + "记账金保证金转账银行扣款结果已成功处理，此条为银行重复返回数据。");
                                        ShowMessage.ShowMsgColor("流水号：" + transactionInfo._id + "记账金保证金转账银行扣款结果已成功处理，此条为银行重复返回数据。", System.Drawing.Color.Yellow);
                                        continue;
                                    }

                                    if (transactionInfo.Result == 0)
                                    {
                                        accountDetails[0].Account_Detail_Out_Id = -2;
                                        accountDetails[0].ChargeStatus = 2;
                                        accountDetails[0].Bank_Charge_Time = transactionInfo.BankChargeTime;
                                        InsertIntoProxyAccountDetailBankFailToSuccess(accountDetails[0], oracleAccess);

                                        //List<ProxyAccountDetailBankFail> proxyAccountDetailBankFailList = QueryProxyAccountDetailBankFailByAccountNo(accountDetails[0].AccountNo);
                                        //List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(accountDetails[0].Account_No);
                                        //if (proxyAccountDetailBankFailWaitDeductList == null || !proxyAccountDetailBankFailWaitDeductList.Any())
                                        //{
                                        //    ShowMessage.ShowMsgColor("PROXY_ACCOUNT_DETAIL表交易银行扣款成功记录Account_No：" + accountDetails[0].Account_No + "在PAD_BANKFAILWAITDEDUCT中无对应记录", System.Drawing.Color.Yellow);
                                        //    LogCommon.GetInfoLogInstance().LogError("PROXY_ACCOUNT_DETAIL表交易银行扣款成功记录Account_No：" + accountDetails[0].Account_No + "在PAD_BANKFAILWAITDEDUCT中无对应记录");
                                        //    continue;
                                        //}
                                        //else
                                        //{
                                        //    InsertIntoFlAccountStalist(new FlAccountStalist() { }, oracleAccess);
                                        //}
                                    }
                                    else
                                    {
                                        if (accountDetails[0].ChargeStatus == 5)
                                        {
                                            UpdateProxyAccountDetailBankFailWaitDeduct(transactionInfo._id, transactionInfo.BankChargeTime, oracleAccess);
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    // 待
                                    //InsertIntoProxyAccountDetailSuccessNone(new ProxyAccountDetail() { }, oracleAccess);
                                    //InsertIntoProxyAccountDetailToClear(new ProxyAccountDetailToClear() { }, oracleAccess);
                                    ShowMessage.ShowMsgColor("交易流水号" + transactionInfo._id.ToString() + "在PROXY_ACCOUNT_DETAIL表中无对应记录", System.Drawing.Color.Yellow);
                                    LogCommon.GetErrorLogInstance().LogError("交易流水号" + transactionInfo._id.ToString() + "在PROXY_ACCOUNT_DETAIL表中无对应记录");
                                }
                                oracleAccess.CommitTransaction();
                            }
                            catch (Exception ex)
                            {
                                oracleAccess.RollbackTransaction();
                                if (oracleAccess.HandleOracleException(ex) != ReturnResult.OracleUniqueConstraintException)
                                {
                                    isAllSuccess = false;
                                    LogCommon.GetErrorLogInstance().LogError("流水号" + transactionInfo._id + "记账金保证金转账（2002号文件扣款结果）数据处理定时任务处理异常" + ex.ToString());
                                }
                                else
                                {
                                    ShowMessage.ShowMsgColor("流水号" + transactionInfo._id + "记账金保证金转账（2002号文件扣款结果）数据处理定时任务主键冲突", System.Drawing.Color.Yellow);
                                }
                            }
                        }
                        if (isAllSuccess)
                        {
                            var mUpDefinitionBuilder = new UpdateDefinitionBuilder<InputTaskWaitingDone>();
                            var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1);
                            if (dbAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition) > 0)
                            {
                                access.DropCollection(waitingDone.ColName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("记账金保证金转账银行扣款结果处理异常：" + ex.ToString());
                ShowMessage.ShowMsgColor("记账金保证金转账银行扣款结果处理异常：" + ex.ToString(), System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 启动二次扣款成功交易归集定时任务
        /// </summary>
        public void StartProcessTwiceDebitSuccessTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_TWICE_DEBITSUCCESS_TASK_HEARTBEAT").Value) * 60 * 1000;// 单位分
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理二次扣款成功交易归集定时任务", System.Drawing.Color.Green);
                TwiceDebitSuccessTask();
                ShowMessage.ShowMsgColor("二次扣款成功交易归集定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_TWICE_DEBITSUCCESS_TASK_HEARTBEAT").Value + "分钟", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位分钟
            }
        }

        /// <summary>
        /// 二次扣款成功交易归集定时任务3
        /// 定时线程i每30分钟（在S_PARA集合中配置参数）启动1次
        /// </summary>
        public void TwiceDebitSuccessTask()
        {
            try
            {
                List<ProxyAccountDetailBankFailToSuccess> failToSuccessList = QueryAllProxyAccountDetailBankFailToSuccess();
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Empty;
                    foreach (var item in failToSuccessList)
                    {
                        // 删除PROXY_ACCOUNT_DETAIL_BANKFAIL 或PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT 中对应的数据（根据ACCOUNT_DETAIL_OUT_ID取值判断！

                        try
                        {
                            oracleAccess.BeginTransaction();
                            InsertIntoProxyAccountDetailToClear(item, DateTime.Now, 2, oracleAccess);

                            sql = @"DELETE FROM " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILTOSUCCESS + " where id=:id";
                            oracleAccess.ExecuteSql(sql, new { id = item.ID });

                            if (item.Account_Detail_Out_Id == -1)
                            {
                                sql = @"DELETE FROM " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where id=:id";
                                oracleAccess.ExecuteSql(sql, new { id = item.ID });
                            }
                            else if (item.Account_Detail_Out_Id == -2)
                            {
                                sql = @"DELETE FROM " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where id=:id";
                                oracleAccess.ExecuteSql(sql, new { id = item.ID });
                            }

                            sql = @"UPDATE " + DBConstant.PROXY_ACCOUNT_DETAIL + " set CHARGESTATUS=:CHARGESTATUS , BANK_CHARGE_TIME=:BANK_CHARGE_TIME where id=:id";
                            oracleAccess.ExecuteSql(sql, new { CHARGESTATUS = 2, BANK_CHARGE_TIME = item.Bank_Charge_Time, id = item.ID });

                            if (item.Account_Detail_Out_Id != -2)
                            {
                                List<ProxyAccountDetailBankFail> bankFailListByAccountNo = QueryProxyAccountDetailBankFailByAccountNo(item.Account_No);
                                // 除自身以外不存在其他记录
                                bool haveBankFailListByAccountNo = bankFailListByAccountNo != null && bankFailListByAccountNo.Any();
                                List<ProxyAccountDetailBankFailWaitDeduct> proxyAccountDetailBankFailWaitDeductList = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(item.Account_No);
                                bool haveBankFailWaitDeductByAccountNo = proxyAccountDetailBankFailWaitDeductList != null && proxyAccountDetailBankFailWaitDeductList.Any();
                                if (!haveBankFailListByAccountNo && !haveBankFailWaitDeductByAccountNo)
                                {
                                    // 若不存在记录则向ECDBA.FL_ACCOUNT_STALIST插入一条ISSUE_STATUS等于0的记录
                                    InsertIntoFlAccountStalist(new FlAccountStalist() { Account_No = item.Account_No, Package_No = item.Package_No, Start_Time = DateTime.Now, Issue_Status = 0, Remark = "0", Agent_No = item.Bank_Tag }, oracleAccess);
                                }
                            }
                            else
                            {
                                UpdateBankAccount(item.Cash, item.Account_Id, item.Bank_Tag, oracleAccess);
                            }
                            
                            oracleAccess.CommitTransaction();
                        }
                        catch (Exception ex)
                        {
                            oracleAccess.RollbackTransaction();
                            LogCommon.GetErrorLogInstance().LogError("账号" + item.Account_Id + "二次扣款成功交易归集定时任务处理异常：" + ex.ToString());
                            ShowMessage.ShowMsgColor("账号" + item.Account_Id + "二次扣款成功交易归集定时任务处理异常：" + ex.ToString(), System.Drawing.Color.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("二次扣款成功交易归集定时任务处理异常：" + ex.ToString());
                ShowMessage.ShowMsgColor("二次扣款成功交易归集定时任务处理异常：" + ex.ToString(), System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 启动签约信息处理定时任务4
        /// </summary>
        public void StartProcessSignInfoTask()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_BANK_ACCOUNT_SIGN_TASK_HEARTBEAT").Value) * 60 * 1000;// 单位分
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理签约信息定时任务", System.Drawing.Color.Green);
                ProcessSignInfoTask();
                ShowMessage.ShowMsgColor("签约信息处理定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_BANK_ACCOUNT_SIGN_TASK_HEARTBEAT").Value + "分钟", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位分钟
            }
        }

        /// <summary>
        /// 签约信息处理定时任务4
        /// </summary>
        public void ProcessSignInfoTask()
        {
            try
            {
                MongoDBAccess<BankAccountSign> mongoAccess = new MongoDBAccess<BankAccountSign>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_SIGN);
                List<BankAccountSign> bankAccountSignList = mongoAccess.FindAsByWhere(p => p.Status == 0, 10000);
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    foreach (var bankAccountSign in bankAccountSignList)
                    {
                        try
                        {
                            oracleAccess.BeginTransaction();
                            switch (bankAccountSign.Command)
                            {
                                case (int)ESignCommand.新增的签约交易: InsertIntoBankAccount(bankAccountSign, oracleAccess, -1); break;
                                case (int)ESignCommand.修改的签约交易: if (UpdateBankAccount(bankAccountSign, oracleAccess) <= 0) { InsertIntoBankAccount(bankAccountSign, oracleAccess, -1); }; break;
                                case (int)ESignCommand.保证金补缴成功交易: UpdateBankAccount(bankAccountSign, oracleAccess); QueryBankAccountBinding(bankAccountSign, oracleAccess); break;
                            }
                            oracleAccess.CommitTransaction();

                            var mUpDefinitionBuilder = new UpdateDefinitionBuilder<BankAccountSign>();
                            var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1);
                            mongoAccess.UpdateDocs(p => p._id == bankAccountSign._id, mUpdateDefinition);
                        }
                        catch (Exception ex)
                        {
                            if (oracleAccess.HandleOracleException(ex) == ReturnResult.OracleUniqueConstraintException)
                            {
                                var mUpDefinitionBuilder = new UpdateDefinitionBuilder<BankAccountSign>();
                                var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1);
                                mongoAccess.UpdateDocs(p => p._id == bankAccountSign._id, mUpdateDefinition);
                                ShowMessage.ShowMsgColor("处理账号：" + bankAccountSign.AccountName + "签约信息定时任务主键冲突", System.Drawing.Color.Yellow);
                            }
                            else
                            {
                                ShowMessage.ShowMsgColor("处理账号：" + bankAccountSign.AccountName + "签约信息定时任务异常", System.Drawing.Color.Red);
                            }
                            oracleAccess.RollbackTransaction();
                            LogCommon.GetErrorLogInstance().LogError("处理签约信息定时任务异常" + ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("处理签约信息定时任务异常：" + ex.ToString());
                ShowMessage.ShowMsgColor("处理签约信息定时任务异常：" + ex.ToString(), System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 启动一卡通黑白名单入库定时任务
        /// </summary>
        public void StartProcessYKTSignIncrement()
        {
            int timeSpan = int.Parse(SYSConstant.sParam.Find(p => p.Key == "S_BANK_ACCOUNT_YKT_SIGN_TASK_HEARTBEAT").Value) * 60 * 1000;// 单位分
            while (true)
            {
                ShowMessage.ShowMsgColor("开始处理一卡通黑白名单定时任务", System.Drawing.Color.Green);
                ProcessYKTSignIncrement();
                ShowMessage.ShowMsgColor("一卡通黑白名单定时任务处理完成，当前线程" + System.Threading.Thread.CurrentThread.ManagedThreadId + "休眠" + SYSConstant.sParam.Find(p => p.Key == "S_BANK_ACCOUNT_YKT_SIGN_TASK_HEARTBEAT").Value + "分钟", System.Drawing.Color.Green);
                System.Threading.Thread.Sleep(timeSpan);// 单位分钟
            }
        }

        /// <summary>
        /// 一卡通黑白名单入库
        /// </summary>
        public void ProcessYKTSignIncrement()
        {
            try
            {
                MongoDBAccess<BankAccountYKTSign> mongoAccess = new MongoDBAccess<BankAccountYKTSign>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_SIGN_YKTINCREMENT);
                List<BankAccountYKTSign> bankAccountList = mongoAccess.FindAsByWhere(p => !string.IsNullOrEmpty(p._id), 0);
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    foreach (var bankAccount in bankAccountList)
                    {
                        try
                        {
                            oracleAccess.BeginTransaction();
                            switch (bankAccount.Status)
                            {
                                case (int)ESignCommand.新增的签约交易: InsertIntoBankAccount(new BankAccountSign() { AccountId = bankAccount._id, AccountName = bankAccount.AccountName, BankTag = bankAccount.BankTag, CashDeposit = bankAccount.CashDeposit, Status = bankAccount.Status, GenTime = bankAccount.GenTime, CreateTime = bankAccount.CreateTime }, oracleAccess, 2); break;
                                case (int)ESignCommand.修改的签约交易:
                                    string sql = string.Empty;
                                    sql = @"SELECT * FROM " + DBConstant.BANK_ACCOUNT_BINDING + " WHERE ACCOUNT_ID=:ACCOUNT_ID and BANK_TAG=:BANK_TAG and STATUS=0";
                                    List<BankAccountBinding> accountBindingList = oracleAccess.QuerySql<BankAccountBinding>(sql, new { ACCOUNT_ID = bankAccount._id, BANK_TAG = bankAccount.BankTag });
                                    if (accountBindingList != null && accountBindingList.Any())
                                    {
                                        InsertIntoFlAccountStalist(new FlAccountStalist() { Account_No = accountBindingList[0].account_No, Issue_Status = 2, Start_Time = DateTime.Now, Remark = "0", Agent_No = bankAccount.BankTag }, oracleAccess);
                                    }
                                    break;
                            }
                            oracleAccess.CommitTransaction();
                            mongoAccess.DeleteOne(p => p._id == bankAccount._id);
                        }
                        catch (Exception ex)
                        {
                            oracleAccess.RollbackTransaction();
                            LogCommon.GetErrorLogInstance().LogError("处理一卡通黑白名单定时任务异常" + ex.ToString());
                            ShowMessage.ShowMsgColor("处理一卡通黑白名单定时任务异常", System.Drawing.Color.Red);
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("处理一卡通黑白名单定时任务异常" + ex.ToString());
                ShowMessage.ShowMsgColor("处理一卡通黑白名单定时任务异常", System.Drawing.Color.Red);
            }
        }

        /// <summary>
        /// 根据主键查询银行扣款交易记录表
        /// </summary>
        /// <param name="id">主键id</param>
        /// <returns>List<ProxyAccountDetail></returns>
        public List<ProxyAccountDetail> QueryProxyAccountDetailById(string id)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + "  from " + DBConstant.PROXY_ACCOUNT_DETAIL + " where id=:id");
                    return oracleAccess.QuerySql<ProxyAccountDetail>(sql, new { id = id });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 查询银行扣款交易记录表更新操作
        /// </summary>
        /// <param name="bankChargeTime">bankChargeTime</param>
        /// <param name="id">id</param>
        /// <param name="chargeStatus">chargeStatus</param>
        /// <returns>int</returns>
        public int UpdateProxyAccountDetail(DateTime bankChargeTime, string id,int chargeStatus)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = "UPDATE " + DBConstant.PROXY_ACCOUNT_DETAIL + " set CHARGESTATUS=:CHARGESTATUS,BANK_CHARGE_TIME=:BANK_CHARGE_TIME where id=:id";
                    return oracleAccess.ExecuteSql(sql, new { CHARGESTATUS = chargeStatus, BANK_CHARGE_TIME = bankChargeTime, id = id });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 向银行扣款成功遗留交易记录表插入一条数据
        /// </summary>
        /// <param name="accountDetail">ProxyAccountDetail</param>
        /// <returns>int</returns>
        public int InsertIntoProxyAccountDetailSuccessNone(ProxyAccountDetail failToSuccessNone, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"insert into " + DBConstant.PROXY_ACCOUNT_DETAIL_SUCCESSNONE + @"
  (id, account_detail_out_id, account_no, card_no, in_money,out_money, should_in_money, should_out_money, old_balance, out_offline_sn, charge_time, transmit_time, settle_time,
    chargestatus, bank_charge_time, provider_id, package_no,transid, agent_no, main_card_no, bank_tag, account_id, account_name, account_type, netno, mid, card_type,
      physical_type, issuer_id, iccard_no, balance,last_balance, tacno, psamid, psam_tran_sn, dealstatus,medium_type, trans_type, ic_trans_time, trans_seq,
         ticks_type, en_network, en_time, en_plazaid, en_operator_id, en_shift_id, network, plazaid,operator_id, shift_id, vehclass, car_serial, clear_target_date, description, cleartargetdate,due_fare, cash)
values
  (:id, :account_detail_out_id, :account_no, :card_no, :in_money, :out_money, :should_in_money, :should_out_money, :old_balance, :out_offline_sn, :charge_time, 
:transmit_time, :settle_time, :chargestatus, :bank_charge_time, :provider_id, :package_no, :transid, :agent_no, :main_card_no, :bank_tag, :account_id, :account_name, 
:account_type, :netno, :mid, :card_type, :physical_type, :issuer_id, :iccard_no, :balance, :last_balance, :tacno, :psamid, :psam_tran_sn, :dealstatus, :medium_type,
:trans_type, :ic_trans_time, :trans_seq, :ticks_type, :en_network, :en_time, :en_plazaid, :en_operator_id, :en_shift_id, :network, :plazaid, :operator_id, :shift_id, 
:vehclass, :car_serial, :clear_target_date, :description, :cleartargetdate, :due_fare, :cash)";
                var param = new
                {
                    id = failToSuccessNone.ID,
                    account_detail_out_id = failToSuccessNone.Account_Detail_Out_Id,
                    account_no = failToSuccessNone.Account_No,
                    card_no = failToSuccessNone.Card_No,
                    in_money = failToSuccessNone.In_Money,
                    out_money = failToSuccessNone.Out_Money,
                    should_in_money = failToSuccessNone.Should_In_Money,
                    should_out_money = failToSuccessNone.Should_Out_Money,
                    old_balance = failToSuccessNone.Old_Balance,
                    out_offline_sn = failToSuccessNone.Out_Offline_SN,
                    charge_time = failToSuccessNone.Charge_Time,
                    transmit_time = DateTime.Now,
                    settle_time = failToSuccessNone.Settle_Time,
                    chargestatus = failToSuccessNone.ChargeStatus,
                    bank_charge_time = failToSuccessNone.Bank_Charge_Time,
                    provider_id = failToSuccessNone.Provider_Id,
                    package_no = failToSuccessNone.Package_No,
                    transid = failToSuccessNone.TransId,
                    agent_no = failToSuccessNone.Agent_No,
                    main_card_no = failToSuccessNone.Main_Card_No,
                    bank_tag = failToSuccessNone.Bank_Tag,
                    account_id = failToSuccessNone.Account_Id,
                    account_name = failToSuccessNone.Account_Name,
                    account_type = 0,
                    netno = failToSuccessNone.NETNO,
                    mid = failToSuccessNone.MID,
                    card_type = failToSuccessNone.Card_Type,
                    physical_type = failToSuccessNone.Physical_Type,
                    issuer_id = failToSuccessNone.Issuer_Id,
                    iccard_no = failToSuccessNone.IcCard_No,
                    balance = failToSuccessNone.Balance,
                    last_balance = failToSuccessNone.Last_Balance,
                    tacno = failToSuccessNone.TacNo,
                    psamid = failToSuccessNone.PsaMID,
                    psam_tran_sn = failToSuccessNone.Psam_Tran_SN,
                    dealstatus = failToSuccessNone.DealStatus,
                    medium_type = failToSuccessNone.Medium_Type,
                    trans_type = failToSuccessNone.Trans_Type,
                    ic_trans_time = failToSuccessNone.Ic_Trans_Time,
                    trans_seq = failToSuccessNone.Trans_SEQ,
                    ticks_type = failToSuccessNone.Ticks_Type,
                    en_network = failToSuccessNone.En_NetWork,
                    en_time = failToSuccessNone.En_Time,
                    en_plazaid = failToSuccessNone.En_Plazaid,
                    en_operator_id = failToSuccessNone.En_Operator_Id,
                    en_shift_id = failToSuccessNone.En_Shift_Id,
                    network = failToSuccessNone.NetWork,
                    plazaid = failToSuccessNone.Plazaid,
                    operator_id = failToSuccessNone.Operator_Id,
                    shift_id = failToSuccessNone.Shift_Id,
                    vehclass = failToSuccessNone.VehClass,
                    car_serial = failToSuccessNone.Car_Serial,
                    clear_target_date = failToSuccessNone.Clear_Target_Date,
                    description = failToSuccessNone.Description,
                    cleartargetdate = failToSuccessNone.ClearTargetDate,
                    due_fare = failToSuccessNone.Due_Fare,
                    cash = failToSuccessNone.Cash
                };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 向银行扣款失败交易再次扣款成功记录表插入一条数据
        /// </summary>
        /// <param name="failToSuccess">ProxyAccountDetailBankFailToSuccess</param>
        /// <returns>int</returns>
        public int InsertIntoProxyAccountDetailBankFailToSuccess(ProxyAccountDetail failToSuccess,OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"insert into " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILTOSUCCESS + @"
  (id, account_detail_out_id, account_no, card_no, in_money,out_money, should_in_money, should_out_money, old_balance, out_offline_sn, charge_time, transmit_time, settle_time,
    chargestatus, bank_charge_time, provider_id, package_no,transid, agent_no, main_card_no, bank_tag, account_id, account_name, account_type, netno, mid, card_type,
      physical_type, issuer_id, iccard_no, balance,last_balance, tacno, psamid, psam_tran_sn, dealstatus,medium_type, trans_type, ic_trans_time, trans_seq,
         ticks_type, en_network, en_time, en_plazaid, en_operator_id, en_shift_id, network, plazaid,operator_id, shift_id, vehclass, car_serial, clear_target_date, description, cleartargetdate,due_fare, cash)
values
  (:id, :account_detail_out_id, :account_no, :card_no, :in_money, :out_money, :should_in_money, :should_out_money, :old_balance, :out_offline_sn, :charge_time, 
:transmit_time, :settle_time, :chargestatus, :bank_charge_time, :provider_id, :package_no, :transid, :agent_no, :main_card_no, :bank_tag, :account_id, :account_name, 
:account_type, :netno, :mid, :card_type, :physical_type, :issuer_id, :iccard_no, :balance, :last_balance, :tacno, :psamid, :psam_tran_sn, :dealstatus, :medium_type,
:trans_type, :ic_trans_time, :trans_seq, :ticks_type, :en_network, :en_time, :en_plazaid, :en_operator_id, :en_shift_id, :network, :plazaid, :operator_id, :shift_id, 
:vehclass, :car_serial, :clear_target_date, :description, :cleartargetdate, :due_fare, :cash)";
                var param = new
                {
                    id = failToSuccess.ID,
                    account_detail_out_id = failToSuccess.Account_Detail_Out_Id,
                    account_no = failToSuccess.Account_No,
                    card_no = failToSuccess.Card_No,
                    in_money = failToSuccess.In_Money,
                    out_money = failToSuccess.Out_Money,
                    should_in_money = failToSuccess.Should_In_Money,
                    should_out_money = failToSuccess.Should_Out_Money,
                    old_balance = failToSuccess.Old_Balance,
                    out_offline_sn = failToSuccess.Out_Offline_SN,
                    charge_time = failToSuccess.Charge_Time,
                    transmit_time = DateTime.Now,
                    settle_time = failToSuccess.Settle_Time,
                    chargestatus = failToSuccess.ChargeStatus,
                    bank_charge_time = failToSuccess.Bank_Charge_Time,
                    provider_id = failToSuccess.Provider_Id,
                    package_no = failToSuccess.Package_No,
                    transid = failToSuccess.TransId,
                    agent_no = failToSuccess.Agent_No,
                    main_card_no = failToSuccess.Main_Card_No,
                    bank_tag = failToSuccess.Bank_Tag,
                    account_id = failToSuccess.Account_Id,
                    account_name = failToSuccess.Account_Name,
                    account_type = failToSuccess.Account_Type,
                    netno = failToSuccess.NETNO,
                    mid = failToSuccess.MID,
                    card_type = failToSuccess.Card_Type,
                    physical_type = failToSuccess.Physical_Type,
                    issuer_id = failToSuccess.Issuer_Id,
                    iccard_no = failToSuccess.IcCard_No,
                    balance = failToSuccess.Balance,
                    last_balance = failToSuccess.Last_Balance,
                    tacno = failToSuccess.TacNo,
                    psamid = failToSuccess.PsaMID,
                    psam_tran_sn = failToSuccess.Psam_Tran_SN,
                    dealstatus = failToSuccess.DealStatus,
                    medium_type = failToSuccess.Medium_Type,
                    trans_type = failToSuccess.Trans_Type,
                    ic_trans_time = failToSuccess.Ic_Trans_Time,
                    trans_seq = failToSuccess.Trans_SEQ,
                    ticks_type = failToSuccess.Ticks_Type,
                    en_network = failToSuccess.En_NetWork,
                    en_time = failToSuccess.En_Time,
                    en_plazaid = failToSuccess.En_Plazaid,
                    en_operator_id = failToSuccess.En_Operator_Id,
                    en_shift_id = failToSuccess.En_Shift_Id,
                    network = failToSuccess.NetWork,
                    plazaid = failToSuccess.Plazaid,
                    operator_id = failToSuccess.Operator_Id,
                    shift_id = failToSuccess.Shift_Id,
                    vehclass = failToSuccess.VehClass,
                    car_serial = failToSuccess.Car_Serial,
                    clear_target_date = failToSuccess.Clear_Target_Date,
                    description = failToSuccess.Description,
                    cleartargetdate = failToSuccess.ClearTargetDate,
                    due_fare = failToSuccess.Due_Fare,
                    cash = failToSuccess.Cash
                };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据交易记录的ACCOUNT_NO检索PROXY_ACCOUNT_DETAIL_BANKFAIL
        /// </summary>
        /// <param name="accountNo">accountNo</param>
        /// <returns>List<ProxyAccountDetailBankFail></returns>
        public List<ProxyAccountDetailBankFail> QueryProxyAccountDetailBankFailByAccountNo(string accountNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where account_no=:accountNo");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFail>(sql, new { accountNo = accountNo });
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
        /// 根据交易记录的id检索PROXY_ACCOUNT_DETAIL_BANKFAIL
        /// </summary>
        /// <param name="accountNo">id</param>
        /// <returns>List<ProxyAccountDetailBankFail></returns>
        public List<ProxyAccountDetailBankFail> QueryProxyAccountDetailBankFailById(string id)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " where id=:id");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFail>(sql, new { id = id });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 交易是否是本地路方交易
        /// </summary>
        /// <param name="providerId">providerId</param>
        /// <returns>bool</returns>
        public bool IsLocalServiceProvider(string providerId)
        {
            try
            {
                MongoDBAccess<Spara> sparamAccess = new MongoDBAccess<Spara>(SYSConstant.BANK_CONFIG, SYSConstant.S_PARA);
                List<Spara> sparam = sparamAccess.FindAsByWhere(p => p.Key == "LocalServiceProvider", 0);
                if (sparam != null && sparam.Any())
                {
                    return sparam.FirstOrDefault().Value == providerId;
                }
                else
                { return false; }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据交易记录的ACCOUNT_NO检索PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT
        /// </summary>
        /// <param name="accountNo">accountNo</param>
        /// <returns>List<ProxyAccountDetailBankFailWaitDeduct></returns>
        public List<ProxyAccountDetailBankFailWaitDeduct> QueryProxyAccountDetailBankFailWaitDeductByAccountNo(string accountNo)
        {
            try
            {
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    string sql = string.Format(@"SELECT " + DBConstant.sqlProxyAccountDetail + " from " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " where account_no=:accountNo");
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFailWaitDeduct>(sql, new { accountNo = accountNo });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新PAD_BANKFAILWAITDEDUCT.settle_time为银行扣款失败时间
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="settleTime">银行扣款失败时间</param>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <returns>int</returns>
        public int UpdateProxyAccountDetailBankFailWaitDeduct(string id, DateTime settleTime, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = "UPDATE " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILWAITDEDUCT + " set SETTLE_TIME=:settleTime where id=:id";
                return oracleAccess.ExecuteSql(sql, new { settleTime = settleTime, id = id });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 向ECDBA.FL_ACCOUNT_STALIST插入一条记录
        /// </summary>
        /// <param name="flAccountStalist">flAccountStalist</param>
        /// <returns>int</returns>
        public int InsertIntoFlAccountStalist(FlAccountStalist flAccountStalist,OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"insert into " + DBConstant.FL_ACCOUNT_STALIST + @"
  (package_no, account_no, issue_status, start_time, package_time, flag,Remark,Operator_No,Agent_No)
values
  (:package_no, :account_no, :issue_status, :start_time, :package_time, :flag,:remark,:operator_no,:agent_no) 
";
                var param = new
                {
                    package_no = flAccountStalist.Package_No,
                    account_no = flAccountStalist.Account_No,
                    issue_status = flAccountStalist.Issue_Status,
                    start_time = flAccountStalist.Start_Time,
                    package_time = "",
                    flag = 0,
                    remark = Enum.Parse(typeof(EDebitResult), flAccountStalist.Remark).ToString(),
                    operator_no = "000000",
                    agent_no = flAccountStalist.Agent_No

                };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// 向银行扣款失败交易记录表插入一条数据
        /// </summary>
        /// <param name="bankFail">银行扣款失败交易</param>
        /// <returns>int</returns>
        public int InsertIntoProxyAccountDetailBankFail(ProxyAccountDetail bankFail, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"insert into " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + @"
  (id, account_detail_out_id, account_no, card_no, in_money,out_money, should_in_money, should_out_money, old_balance, out_offline_sn, charge_time, transmit_time, settle_time,
    chargestatus, bank_charge_time, provider_id, package_no,transid, agent_no, main_card_no, bank_tag, account_id, account_name, account_type, netno, mid, card_type,
      physical_type, issuer_id, iccard_no, balance,last_balance, tacno, psamid, psam_tran_sn, dealstatus,medium_type, trans_type, ic_trans_time, trans_seq,
         ticks_type, en_network, en_time, en_plazaid, en_operator_id, en_shift_id, network, plazaid,operator_id, shift_id, vehclass, car_serial, clear_target_date, description, cleartargetdate,due_fare, cash)
values
  (:id, :account_detail_out_id, :account_no, :card_no, :in_money, :out_money, :should_in_money, :should_out_money, :old_balance, :out_offline_sn, :charge_time, 
:transmit_time, :settle_time, :chargestatus, :bank_charge_time, :provider_id, :package_no, :transid, :agent_no, :main_card_no, :bank_tag, :account_id, :account_name, 
:account_type, :netno, :mid, :card_type, :physical_type, :issuer_id, :iccard_no, :balance, :last_balance, :tacno, :psamid, :psam_tran_sn, :dealstatus, :medium_type,
:trans_type, :ic_trans_time, :trans_seq, :ticks_type, :en_network, :en_time, :en_plazaid, :en_operator_id, :en_shift_id, :network, :plazaid, :operator_id, :shift_id, 
:vehclass, :car_serial, :clear_target_date, :description, :cleartargetdate, :due_fare, :cash)";
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
                    chargestatus = 5,
                    bank_charge_time = bankFail.Bank_Charge_Time,
                    provider_id = bankFail.Provider_Id,
                    package_no = bankFail.Package_No,
                    transid = bankFail.TransId,
                    agent_no = bankFail.Agent_No,
                    main_card_no = bankFail.Main_Card_No,
                    bank_tag = bankFail.Bank_Tag,
                    account_id = bankFail.Account_Id,
                    account_name = bankFail.Account_Name,
                    account_type = bankFail.Account_Type,
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
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新PAD_BANKFAIL.settle_time为银行扣款失败时间
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="settleTime">银行扣款失败时间</param>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <returns>int</returns>
        public int UpdateProxyAccountDetailBankFail(string id,DateTime settleTime, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = "UPDATE " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAIL + " set SETTLE_TIME=:settleTime where id=:id";
                return oracleAccess.ExecuteSql(sql, new { settleTime = settleTime, id = id });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 交易是本地路方T_DISPUTE_DATA中没有该笔交易
        /// </summary>
        /// <returns></returns>
        public List<dynamic> T_DISPUTE_DATA(string providerId,int packageNo,int TransId)
        {
            try
            {
                string sql = string.Empty;
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    sql = @"SELECT " + DBConstant.sqlFLRCVDATA + " FROM " + DBConstant.T_DISPUTE_DATA + " where PROVIDER_ID=:PROVIDER_ID and PACKAGE_NO=:PACKAGE_NO and TRANSID=:TRANSID ";
                    return oracleAccess.QuerySql<dynamic>(sql, new { PROVIDER_ID = providerId, PACKAGE_NO = packageNo, TRANSID = TransId });
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 插入一条记录到T_DISPUTE_DATA
        /// </summary>
        /// <returns></returns>
        public int InsertIntoTdisputeData(OracleDBAccess oracleAccess,ProxyAccountDetail accountDetail)
        {
            try
            {
                string sql = @"insert into " + DBConstant.T_DISPUTE_DATA + @" (PROVIDER_ID,PACKAGE_NO,TRANSID,NETNO,MID,
                                            CARD_TYPE,PHYSICAL_TYPE,ISSUER_ID,ISSUER_NUM,ICCARD_NO,
                                            BALANCE,DUE_FARE,CASH,TACNO,LAST_BALANCE,
                                            PSAMID,PSAM_TRAN_SN,DEALSTATUS,MEDIUM_TYPE,ISSUER_CARD_TYPE,
                                            TRANS_TYPE,IC_TRANS_TIME,TRANS_SEQ,TICKS_TYPE,CARD_TRANS_COUNT,
                                            EN_NETWORK,EN_TIME,EN_PLAZAID,EN_OPERATOR_ID,EN_SHIFT_ID,
                                            NETWORK,PLAZAID,OPERATOR_ID,SHIFT_ID,VEHCLASS,
                                            CAR_SERIAL,CLEARTARGETDATE,DESCRIPTION
                                            ,VERSION,DEAL_STATUS,CHARGE_STATUS,CHARGE_CASH,CHARGE_TIME,
                                            CHARGE_PACKAGE_NO,CHARGE_PACKAGE_TIME,PROCESS_TIME,CARD_ACCOUNT_BALANCE,ACCOUNT_NO,
                                            REMIT_ID,REMIT_CASH,REMIT_TIME,REMIT_PACKAGE_NO,REMIT_PACKAGE_TIME,
                                            STATISTICS_DATE,CLEAR_TARGET_DATE) values 
                                            (:PROVIDER_ID,:PACKAGE_NO,:TRANSID,:NETNO,:MID,
                                            :CARD_TYPE,:PHYSICAL_TYPE,:ISSUER_ID,:ISSUER_NUM,:ICCARD_NO,
                                            :BALANCE,:DUE_FARE,:CASH,:TACNO,:LAST_BALANCE,
                                            :PSAMID,:PSAM_TRAN_SN,:DEALSTATUS,:MEDIUM_TYPE,:ISSUER_CARD_TYPE,
                                            :TRANS_TYPE,:IC_TRANS_TIME,:TRANS_SEQ,:TICKS_TYPE,:CARD_TRANS_COUNT,
                                            :EN_NETWORK,:EN_TIME,:EN_PLAZAID,:EN_OPERATOR_ID,:EN_SHIFT_ID,
                                            :NETWORK,:PLAZAID,:OPERATOR_ID,:SHIFT_ID,:VEHCLASS,
                                            :CAR_SERIAL,:CLEARTARGETDATE,:DESCRIPTION
                                            ,:VERSION,:DEAL_STATUS,:CHARGE_STATUS,:CHARGE_CASH,:CHARGE_TIME,
                                            :CHARGE_PACKAGE_NO,:CHARGE_PACKAGE_TIME,:PROCESS_TIME,:CARD_ACCOUNT_BALANCE,:ACCOUNT_NO,
                                            :REMIT_ID,:REMIT_CASH,:REMIT_TIME,:REMIT_PACKAGE_NO,:REMIT_PACKAGE_TIME,
                                            :STATISTICS_DATE,:CLEAR_TARGET_DATE)";
                var param = new
                {
                    PROVIDER_ID = accountDetail.Provider_Id,
                    PACKAGE_NO = accountDetail.Package_No,
                    TRANSID = accountDetail.TransId,
                    NETNO = accountDetail.NETNO,
                    MID = accountDetail.MID,
                    CARD_TYPE = accountDetail.Card_Type,
                    PHYSICAL_TYPE = accountDetail.Physical_Type,
                    ISSUER_ID = accountDetail.Issuer_Id,
                    ISSUER_NUM = accountDetail.Card_No,
                    ICCARD_NO = accountDetail.IcCard_No,
                    BALANCE = accountDetail.Balance,
                    DUE_FARE = accountDetail.Due_Fare,
                    CASH = accountDetail.Cash,
                    TACNO = accountDetail.TacNo,
                    LAST_BALANCE = accountDetail.Last_Balance,
                    PSAMID = accountDetail.PsaMID,
                    PSAM_TRAN_SN = accountDetail.Psam_Tran_SN,
                    DEALSTATUS = accountDetail.DealStatus,
                    MEDIUM_TYPE = accountDetail.Medium_Type,
                    ISSUER_CARD_TYPE = "",
                    TRANS_TYPE = accountDetail.Trans_Type,
                    IC_TRANS_TIME = accountDetail.Ic_Trans_Time,
                    TRANS_SEQ = accountDetail.Trans_SEQ,
                    TICKS_TYPE = accountDetail.Ticks_Type,
                    CARD_TRANS_COUNT = "",
                    EN_NETWORK = accountDetail.En_NetWork,
                    EN_TIME = accountDetail.En_Time,
                    EN_PLAZAID = accountDetail.En_Plazaid,
                    EN_OPERATOR_ID = accountDetail.En_Operator_Id,
                    EN_SHIFT_ID = accountDetail.En_Shift_Id,
                    NETWORK = accountDetail.NetWork,
                    PLAZAID = accountDetail.Plazaid,
                    OPERATOR_ID = accountDetail.Operator_Id,
                    SHIFT_ID = accountDetail.Shift_Id,
                    VEHCLASS = accountDetail.VehClass,
                    CAR_SERIAL = accountDetail.Car_Serial,
                    CLEARTARGETDATE = accountDetail.ClearTargetDate,
                    DESCRIPTION = accountDetail.Description,
                    VERSION = 0,
                    DEAL_STATUS = accountDetail.DealStatus,
                    CHARGE_STATUS = 99,
                    CHARGE_CASH = 0,// 扣款失败取值0
                    CHARGE_TIME = accountDetail.Charge_Time,
                    CHARGE_PACKAGE_NO = 0,
                    CHARGE_PACKAGE_TIME = DateTime.Now,
                    PROCESS_TIME = accountDetail.Charge_Time,
                    CARD_ACCOUNT_BALANCE = 0,
                    ACCOUNT_NO = accountDetail.Account_No,
                    REMIT_ID = "",
                    REMIT_CASH = 0,
                    REMIT_TIME = "",
                    REMIT_PACKAGE_NO = "",
                    REMIT_PACKAGE_TIME = "",
                    STATISTICS_DATE = "",
                    CLEAR_TARGET_DATE = accountDetail.Clear_Target_Date
                };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 插入一条记录到T_DISPUTE_DETAIL
        /// </summary>
        /// <returns></returns>
        public int InsertIntoTdisputeDetail(OracleDBAccess oracleAccess, ProxyAccountDetail accountDetail)
        {
            try
            {
                string sql = @"INSERT INTO " + DBConstant.T_DISPUTE_DETAIL + @"(provider_id, package_no, transid, charge_status, create_time, closed, process_time, process_result) 
                VALUES(:provider_id, :package_no, :transid, :charge_status, :create_time, :closed, :process_time, :process_result)";

                var param = new
                {
                    provider_id = accountDetail.Provider_Id,
                    package_no = accountDetail.Package_No,
                    transid = accountDetail.TransId,
                    charge_status = 99,
                    create_time = accountDetail.Charge_Time,
                    closed = 0,
                    process_time = "",
                    process_result = ""
                };

                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 插入一条记录到FL_CHARGE
        /// </summary>
        /// <returns></returns>
        public int InsertIntoFlCharge(OracleDBAccess oracleAccess, ProxyAccountDetail accountDetail,int status)
        {
            try
            {
                string sql = @"INSERT INTO " + DBConstant.FL_CHARGE + @"(PROVIDER_ID,TRANSID,PACKAGE_NO,CHARGE_CASH,CHARGE_STATUS
                                            ,CHARGE_TIME,CHARGE_PACKAGE_NO,CHARGE_PACKAGE_TIME,VERSION,FLAG) 
                                            values (:PROVIDER_ID,:TRANSID,:PACKAGE_NO,:CHARGE_CASH,:CHARGE_STATUS,:CHARGE_TIME,:CHARGE_PACKAGE_NO,:CHARGE_PACKAGE_TIME,:VERSION,:FLAG)";
                var param = new
                {
                    PROVIDER_ID = accountDetail.Provider_Id,
                    TRANSID = accountDetail.TransId,
                    PACKAGE_NO = accountDetail.Package_No,
                    CHARGE_CASH = status == 99 ? 0 : accountDetail.Cash,
                    CHARGE_STATUS = status,
                    CHARGE_TIME = accountDetail.Charge_Time,
                    CHARGE_PACKAGE_NO = "",
                    CHARGE_PACKAGE_TIME = "",
                    VERSION = 0,
                    FLAG = 0
                };

                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新MSG_SND表
        /// </summary>
        /// <param name="oracleAccess">OracleDBAccess</param>
        /// <param name="operatorId">operatorId</param>
        /// <returns>int</returns>
        public int UpdateMsgSnd(OracleDBAccess oracleAccess, int operatorId)
        {
            try
            {
                string sql = "UPDATE " + DBConstant.MSG_SND + " SET VERSION=VERSION+1,DISPUTE_COUNT=DISPUTE_COUNT+1 where id=:id";
                var param = new { id = operatorId };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新MSG_SND表
        /// </summary>
        /// <param name="oracleAccess">OracleDBAccess</param>
        /// <param name="operatorId">operatorId</param>
        /// <param name="cash">cash(元)</param>
        /// <returns>int</returns>
        public int UpdateMsgSndAmount(OracleDBAccess oracleAccess, int operatorId, decimal cash)
        {
            try
            {
                string sql = "UPDATE " + DBConstant.MSG_SND + " SET VERSION=VERSION+1,TOTAL_AMOUNT=TOTAL_AMOUNT+" + cash + " where id=:id";
                var param = new { id = operatorId };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// T_TRANSACTION数据表插入一条数据
        /// </summary>
        /// <returns></returns>
        public int InsertIntoTtransaction(OracleDBAccess oracleAccess, ProxyAccountDetail accountDetail)
        {
            try
            {
                string sql = @"insert into " + DBConstant.T_TRANSACTION + @"
  (provider_id, package_no, transid, version, netno, mid, card_type, physical_type, issuer_id, issuer_num, iccard_no, balance, due_fare, cash, tacno, last_balance, psamid,
psam_tran_sn, dealstatus, medium_type, issuer_card_type, trans_type, ic_trans_time, trans_seq, ticks_type, card_trans_count, en_network, en_time, en_plazaid, en_operator_id, 
en_shift_id, network, plazaid, operator_id, shift_id, vehclass, car_serial, cleartargetdate, description, deal_status, charge_cash, charge_status, charge_time, charge_package_no, 
charge_package_time, remit_id, remit_cash, remit_time, remit_package_no, remit_package_time, process_time, statistics_date, card_account_balance, account_no, clear_target_date)
values
  (:provider_id, :package_no, :transid, :version, :netno, :mid, :card_type, :physical_type, :issuer_id, :issuer_num, :iccard_no, :balance, :due_fare, :cash, :tacno, :last_balance, :psamid,
:psam_tran_sn, :dealstatus, :medium_type, :issuer_card_type, :trans_type, :ic_trans_time, :trans_seq, :ticks_type, :card_trans_count, :en_network, :en_time, :en_plazaid, :en_operator_id,
:en_shift_id, :network, :plazaid, :operator_id, :shift_id, :vehclass, :car_serial, :cleartargetdate, :description, :deal_status, :charge_cash, :charge_status, :charge_time, :charge_package_no,
:charge_package_time, :remit_id, :remit_cash, :remit_time, :remit_package_no, :remit_package_time, :process_time, :statistics_date, :card_account_balance, :account_no, :clear_target_date)";
                var param = new
                {
                    provider_id = accountDetail.Provider_Id,
                    package_no = accountDetail.Package_No,
                    transid = accountDetail.TransId,
                    version = 0,
                    netno = accountDetail.NETNO,
                    mid = accountDetail.MID,
                    card_type = accountDetail.Card_Type,
                    physical_type = accountDetail.Physical_Type,
                    issuer_id = accountDetail.Issuer_Id,
                    issuer_num = accountDetail.Card_No,
                    iccard_no = accountDetail.IcCard_No,
                    balance = accountDetail.Balance,
                    due_fare = accountDetail.Due_Fare,
                    cash = accountDetail.Cash,
                    tacno = accountDetail.TacNo,
                    last_balance = accountDetail.Last_Balance,
                    psamid = accountDetail.PsaMID,
                    psam_tran_sn = accountDetail.Psam_Tran_SN,
                    dealstatus = accountDetail.DealStatus,
                    medium_type = accountDetail.Medium_Type,
                    issuer_card_type = "",
                    trans_type = accountDetail.Trans_Type,
                    ic_trans_time = accountDetail.Ic_Trans_Time,
                    trans_seq = accountDetail.Trans_SEQ,
                    ticks_type = accountDetail.Ticks_Type,
                    card_trans_count = "",
                    en_network = accountDetail.En_NetWork,
                    en_time = accountDetail.En_Time,
                    en_plazaid = accountDetail.En_Plazaid,
                    en_operator_id = accountDetail.En_Operator_Id,
                    en_shift_id = accountDetail.En_Shift_Id,
                    network = accountDetail.NetWork,
                    plazaid = accountDetail.Plazaid,
                    operator_id = accountDetail.Operator_Id,
                    shift_id = accountDetail.Shift_Id,
                    vehclass = accountDetail.VehClass,
                    car_serial = accountDetail.Car_Serial,
                    cleartargetdate = accountDetail.ClearTargetDate,
                    description = accountDetail.Description,
                    deal_status = accountDetail.DealStatus,
                    charge_cash = accountDetail.Cash,
                    charge_status = 0,
                    charge_time = accountDetail.Charge_Time,
                    charge_package_no = 0,
                    charge_package_time = DateTime.Now,
                    remit_id = "",
                    remit_cash = 0,
                    remit_time = "",
                    remit_package_no = "",
                    remit_package_time = "",
                    process_time = accountDetail.Charge_Time,
                    statistics_date = "",
                    card_account_balance = 0,
                    account_no = accountDetail.Account_No,
                    clear_target_date = accountDetail.Clear_Target_Date
                };

                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 向银行扣款成功交易通知清分交易记录表插入一条记录
        /// </summary>
        /// <param name="detailToClear">ProxyAccountDetailToClear</param>
        /// <returns>int</returns>
        public int InsertIntoProxyAccountDetailToClear(ProxyAccountDetail detailToClear, DateTime bankChargeTime, int chargeStatus, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"insert into " + DBConstant.PROXY_ACCOUNT_DETAIL_TOCLEAR + @"
  (id, account_detail_out_id, account_no, card_no, in_money,out_money, should_in_money, should_out_money, old_balance, out_offline_sn, charge_time, transmit_time, settle_time,
    chargestatus, bank_charge_time, provider_id, package_no,transid, agent_no, main_card_no, bank_tag, account_id, account_name, account_type, netno, mid, card_type,
      physical_type, issuer_id, iccard_no, balance,last_balance, tacno, psamid, psam_tran_sn, dealstatus,medium_type, trans_type, ic_trans_time, trans_seq,
         ticks_type, en_network, en_time, en_plazaid, en_operator_id, en_shift_id, network, plazaid,operator_id, shift_id, vehclass, car_serial, clear_target_date, description, cleartargetdate,due_fare, cash)
values
  (:id, :account_detail_out_id, :account_no, :card_no, :in_money, :out_money, :should_in_money, :should_out_money, :old_balance, :out_offline_sn, :charge_time, 
:transmit_time, :settle_time, :chargestatus, :bank_charge_time, :provider_id, :package_no, :transid, :agent_no, :main_card_no, :bank_tag, :account_id, :account_name, 
:account_type, :netno, :mid, :card_type, :physical_type, :issuer_id, :iccard_no, :balance, :last_balance, :tacno, :psamid, :psam_tran_sn, :dealstatus, :medium_type,
:trans_type, :ic_trans_time, :trans_seq, :ticks_type, :en_network, :en_time, :en_plazaid, :en_operator_id, :en_shift_id, :network, :plazaid, :operator_id, :shift_id, 
:vehclass, :car_serial, :clear_target_date, :description, :cleartargetdate, :due_fare, :cash)";
                var param = new
                {
                    id = detailToClear.ID,
                    account_detail_out_id = detailToClear.Account_Detail_Out_Id,
                    account_no = detailToClear.Account_No,
                    card_no = detailToClear.Card_No,
                    in_money = detailToClear.In_Money,
                    out_money = detailToClear.Out_Money,
                    should_in_money = detailToClear.Should_In_Money,
                    should_out_money = detailToClear.Should_Out_Money,
                    old_balance = detailToClear.Old_Balance,
                    out_offline_sn = detailToClear.Out_Offline_SN,
                    charge_time = detailToClear.Charge_Time,
                    transmit_time = DateTime.Now,
                    settle_time = detailToClear.Settle_Time,
                    chargestatus = chargeStatus,
                    bank_charge_time = bankChargeTime,
                    provider_id = detailToClear.Provider_Id,
                    package_no = detailToClear.Package_No,
                    transid = detailToClear.TransId,
                    agent_no = detailToClear.Agent_No,
                    main_card_no = detailToClear.Main_Card_No,
                    bank_tag = detailToClear.Bank_Tag,
                    account_id = detailToClear.Account_Id,
                    account_name = detailToClear.Account_Name,
                    account_type = detailToClear.Account_Type,
                    netno = detailToClear.NETNO,
                    mid = detailToClear.MID,
                    card_type = detailToClear.Card_Type,
                    physical_type = detailToClear.Physical_Type,
                    issuer_id = detailToClear.Issuer_Id,
                    iccard_no = detailToClear.IcCard_No,
                    balance = detailToClear.Balance,
                    last_balance = detailToClear.Last_Balance,
                    tacno = detailToClear.TacNo,
                    psamid = detailToClear.PsaMID,
                    psam_tran_sn = detailToClear.Psam_Tran_SN,
                    dealstatus = detailToClear.DealStatus,
                    medium_type = detailToClear.Medium_Type,
                    trans_type = detailToClear.Trans_Type,
                    ic_trans_time = detailToClear.Ic_Trans_Time,
                    trans_seq = detailToClear.Trans_SEQ,
                    ticks_type = detailToClear.Ticks_Type,
                    en_network = detailToClear.En_NetWork,
                    en_time = detailToClear.En_Time,
                    en_plazaid = detailToClear.En_Plazaid,
                    en_operator_id = detailToClear.En_Operator_Id,
                    en_shift_id = detailToClear.En_Shift_Id,
                    network = detailToClear.NetWork,
                    plazaid = detailToClear.Plazaid,
                    operator_id = detailToClear.Operator_Id,
                    shift_id = detailToClear.Shift_Id,
                    vehclass = detailToClear.VehClass,
                    car_serial = detailToClear.Car_Serial,
                    clear_target_date = detailToClear.Clear_Target_Date,
                    description = detailToClear.Description,
                    cleartargetdate = detailToClear.ClearTargetDate,
                    due_fare = detailToClear.Due_Fare,
                    cash = detailToClear.Cash
                };
                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 检索PROXY_ACCOUNT_DETAIL_BANKFAILTOSUCCESS所有交易
        /// </summary>
        /// <returns></returns>
        public List<ProxyAccountDetailBankFailToSuccess> QueryAllProxyAccountDetailBankFailToSuccess()
        {
            try
            {
                string sql = string.Empty;
                using (OracleDBAccess oracleAccess = new OracleDBAccess())
                {
                    sql = @"SELECT " + DBConstant.sqlProxyAccountDetail + " FROM " + DBConstant.PROXY_ACCOUNT_DETAIL_BANKFAILTOSUCCESS;
                    return oracleAccess.QuerySql<ProxyAccountDetailBankFailToSuccess>(sql, null).Take(10000).ToList();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 向核心库中银行签约账户表ISSUE.BANK_ACCOUNT插入一条数据
        /// </summary>
        /// <param name="bankAccount">BankAccount</param>
        /// <returns>int</returns>
        public int InsertIntoBankAccount(BankAccountSign bankAccountSign, OracleDBAccess oracleAccess,int accountType)
        {
            try
            {
                string sql = @"insert into " + DBConstant.BANK_ACCOUNT + @"
              (bank_tag, account_id, account_name, cash_deposit, gen_time, create_time, modify_time, account_type)
            values
              (:bank_tag, :account_id, :account_name, :cash_deposit, :gen_time, :create_time, :modify_time, :account_type)";

                var param = new
                {
                    bank_tag = bankAccountSign.BankTag,
                    account_id = bankAccountSign.AccountId,
                    account_name = bankAccountSign.AccountName,
                    cash_deposit = decimal.Parse((bankAccountSign.CashDeposit / 100).ToString("F2")),
                    gen_time = bankAccountSign.GenTime,
                    create_time = bankAccountSign.CreateTime,
                    modify_time = DateTime.Now,
                    account_type = accountType
                };

                return oracleAccess.ExecuteSql(sql, param);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新核心库银行签约账户表ISSUE.BANK_ACCOUNT对应记录的保证金金额
        /// </summary>
        /// <param name="bankAccount">BankAccount</param>
        /// <returns></returns>
        public int UpdateBankAccount(BankAccountSign bankAccountSign, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.BANK_ACCOUNT + " set Cash_Deposit=:CashDeposit where Bank_Tag=:BankTag and Account_Id=:AccountId";
                return oracleAccess.ExecuteSql(sql, new { CashDeposit = decimal.Parse((bankAccountSign.CashDeposit / 100).ToString("F2")), BankTag = bankAccountSign.BankTag, AccountId = bankAccountSign.AccountId });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 扣保证金
        /// </summary>
        /// <param name="cash">保证金</param>
        /// <param name="accountId">账户</param>
        /// <param name="bankTag">银行标识</param>
        /// <param name="oracleAccess">oracleAccess</param>
        /// <returns></returns>
        public int UpdateBankAccount(decimal cash, string accountId, string bankTag, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = @"UPDATE " + DBConstant.BANK_ACCOUNT + " set Cash_Deposit=Cash_Deposit-:CashDeposit,MODIFY_TIME=sysdate where Bank_Tag=:BankTag and Account_Id=:AccountId";
                return oracleAccess.ExecuteSql(sql, new { CashDeposit = cash / 100, BankTag = bankTag, AccountId = accountId });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 检索BANK_ACCOUNT_BINDING找出所有有效的ACCOUNT_NO
        /// </summary>
        /// <param name="bankAccount"></param>
        public void QueryBankAccountBinding(BankAccountSign bankAccountSign, OracleDBAccess oracleAccess)
        {
            try
            {
                string sql = string.Empty;
                sql = @"SELECT * FROM " + DBConstant.BANK_ACCOUNT_BINDING + " WHERE ACCOUNT_ID=:ACCOUNT_ID and BANK_TAG=:BANK_TAG and STATUS=0";
                List<BankAccountBinding> accountBindingList = oracleAccess.QuerySql<BankAccountBinding>(sql, new { ACCOUNT_ID = bankAccountSign.AccountId, BANK_TAG = bankAccountSign.BankTag });
                foreach (var item in accountBindingList)
                {
                    List<ProxyAccountDetailBankFail> bankFailList = QueryProxyAccountDetailBankFailByAccountNo(item.account_No);
                    List<ProxyAccountDetailBankFailWaitDeduct> waitDeductList = QueryProxyAccountDetailBankFailWaitDeductByAccountNo(item.account_No);
                    if (bankFailList != null && bankFailList.Any() && waitDeductList != null && waitDeductList.Any())
                    {

                    }
                    else
                    {
                        InsertIntoFlAccountStalist(new FlAccountStalist() { Issue_Status = 0, Account_No = item.account_No, Start_Time = DateTime.Now, Agent_No = item.Bank_Tag, Remark = "0" }, oracleAccess);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
