using Oracle.DataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BankBatchService
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 初始化日志组件
            LogCommon.InitLog4Net();

            // 初始化系统配置
            SYSConstant.GetParam();

            // 2001/2002出库
            Task.Run(() =>
            {
                PutOutStorageTask putOutTask = new PutOutStorageTask();
                putOutTask.Start();
            });

            // 生成扣保证金
            Task.Run(() =>
            {
                PutOutStorageTask task = new PutOutStorageTask();
                task.StartProcessDepositTask();
            });

            // 解约
            Task.Run(() =>
            {
                PutOutStorageTask task = new PutOutStorageTask();
                task.StartCancellationInfoTask();
            });

            // 保证金减少
            Task.Run(() =>
            {
                PutOutStorageTask task = new PutOutStorageTask();
                task.StartProcessDepositAmountReduceTask();
            });

            // 银行账号与ETC卡绑定信息（2008号报文）定时处理任务
            Task.Run(() =>
            {
                PutOutStorageTask task = new PutOutStorageTask();
                task.StartProcessBankAccountBingETCTask();
            });

            // 2001入库
            Task.Run(() =>
            {
                PutInStorageTask task = new PutInStorageTask();
                task.StartProcessAccountTransferDataTask();
            });

            // 2002入库
            Task.Run(() =>
            {
                PutInStorageTask task = new PutInStorageTask();
                task.StartProcessDepositTransferTask();
            });

            // 二次扣款成功
            Task.Run(() =>
            {
                PutInStorageTask task = new PutInStorageTask();
                task.StartProcessTwiceDebitSuccessTask();
            });

            //签约 
            Task.Run(() =>
            {
                PutInStorageTask task = new PutInStorageTask();
                task.StartProcessSignInfoTask();
            });

            // 黑白名单
            Task.Run(() =>
            {
                PutInStorageTask task = new PutInStorageTask();
                task.StartProcessYKTSignIncrement();
            });

            BankBatchWindow window = new BankBatchWindow();
            ShowMessage.ShowMessageEvent += window.BankBatchWindow_ShowMessage;
            ShowMessage.ShowMsgColor("后置程序已启动", System.Drawing.Color.Green);
            Application.Run(window);
        }
    }
}
