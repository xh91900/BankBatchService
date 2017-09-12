using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BankBatchService
{
    public partial class BankBatchWindow : Form
    {

        /// <summary>
        /// 构造
        /// </summary>
        public BankBatchWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 消息事件绑定函数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        public void BankBatchWindow_ShowMessage(string message, Color color)
        {
            try
            {
                if (this.richTextBox.InvokeRequired == false)
                {
                    if (this.richTextBox.Text.Length > 10000)
                    {
                        this.richTextBox.Clear();
                    }

                    this.richTextBox.SelectionColor = color;
                    this.richTextBox.ScrollToCaret();
                    this.richTextBox.AppendText(string.Format("{0}: {1}{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message, Environment.NewLine));
                }
                else
                {
                    Action<string, Color> msgAction = new Action<string, Color>(this.BankBatchWindow_ShowMessage);
                    this.BeginInvoke(msgAction, new object[] { message, color });
                }
            }
            catch (Exception ex)
            {
                LogCommon.GetErrorLogInstance().LogError("打印消息异常：" + ex.ToString());
            }
        }

        private void 更新系统配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SYSConstant.GetParam();
            ShowMessage.ShowMsgColor("系统配置更新成功，将在各任务下一次轮询启动时生效！", System.Drawing.Color.White);
        }

        private void 打印系统配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SYSConstant.sParam.ForEach(p =>
            {
                ShowMessage.ShowMsgColor($"{p.Remark}：{p.Value}", System.Drawing.Color.White);
            });
        }

        private void 清屏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.richTextBox.Clear();
        }

        private void 打印发送信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                PutOutStorageTask task = new PutOutStorageTask();
                List<BankAgent> bankAgentList = task.QueryAllBankAgent();
                if (bankAgentList != null && bankAgentList.Any())
                {
                    bankAgentList.ForEach(o =>
                    {
                        MongoDBAccess<OutPutTaskWaitingDone> mongoAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                        List<OutPutTaskWaitingDone> waitingDoneList = mongoAccess.FindAsByWhere(p => p.SendTime >= DateTime.Now.Date && p.BankTag == o.Bank_Tag && p.Status == 1 && (p.TransType == 2001 || p.TransType == 2002), 0);
                        if (waitingDoneList != null && waitingDoneList.Any())
                        {
                            waitingDoneList.ForEach(q =>
                            {
                                ShowMessage.ShowMsgColor($"{q.SendTime}发送{o.Bank_Name}批量扣款文件{q.FileName}包含{q.TotalNum}条交易，涉及金额：{(q.TotalAmount / 100.00).ToString("F2")}元", System.Drawing.Color.White);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor($"系统错误：{ex.ToString()}", System.Drawing.Color.Red);
            }
        }

        private void 打印接收信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                PutOutStorageTask task = new PutOutStorageTask();
                List<BankAgent> bankAgentList = task.QueryAllBankAgent();
                if (bankAgentList != null && bankAgentList.Any())
                {
                    bankAgentList.ForEach(o =>
                    {
                        MongoDBAccess<InputTaskWaitingDone> mongoAccess = new MongoDBAccess<InputTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                        List<InputTaskWaitingDone> waitingDoneList = mongoAccess.FindAsByWhere(p => p.CreateTime >= DateTime.Now.Date && p.BankTag == o.Bank_Tag && p.Status == 1 && (p.TransType == 2001 || p.TransType == 2002), 0);
                        if (waitingDoneList != null && waitingDoneList.Any())
                        {
                            waitingDoneList.ForEach(q =>
                            {
                                ShowMessage.ShowMsgColor($"{q.CreateTime}接收到{o.Bank_Name}批量扣款结果文件{q.FileName}，其中成功扣款{q.SuccessNum}条交易，涉及金额：{(q.SuccessAmount / 100.00).ToString("F2")}元，扣款失败{q.FailNum}条交易，涉及金额：{(q.FailAmount / 100.00).ToString("F2")}元", System.Drawing.Color.White);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowMsgColor($"系统错误：{ex.ToString()}", System.Drawing.Color.Red);
            }
        }
    }
}
