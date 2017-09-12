using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// 日志管理类
    /// </summary>
    public class LogCommon
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        private ILog logger;

        /// <summary>
        /// 消息日志管理单例
        /// </summary>
        private static LogCommon Infolog = new LogCommon("InfoLog");

        /// <summary>
        /// 错误日志管理单例
        /// </summary>
        private static LogCommon Errorlog = new LogCommon("ErrorLog");

        /// <summary>
        /// 初始化日志组件
        /// </summary>
        public static void InitLog4Net()
        {
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);
        }

        /// <summary>
        /// 不让调构造函数
        /// </summary>
        /// <param name="loggerName">自定义的日志对象</param>
        private LogCommon(string loggerName)
        {
            logger = LogManager.GetLogger(loggerName);
        }

        /// <summary>
        /// 获取消息日志实例
        /// </summary>
        /// <returns></returns>
        public static LogCommon GetInfoLogInstance()
        {
            return Infolog;
        }

        /// <summary>
        /// 获取错误日志实例
        /// </summary>
        /// <returns></returns>
        public static LogCommon GetErrorLogInstance()
        {
            return Errorlog;
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogError(string logContent)
        {
            logger.Error(logContent + "\n");
        }

        /// <summary>
        /// 记录消息
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogInfo(string logContent)
        {
            logger.Info(logContent + "\n");
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogWarn(string logContent)
        {
            logger.Warn(logContent + "\n");
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogFatal(string logContent)
        {
            logger.Fatal(logContent + "\n");
        }
    }

    public class ShowMessage
    {

        /// <summary>
        /// 打印消息委托
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="color">颜色</param>
        public delegate void ShowMessageHandle(string message, Color color);

        /// <summary>
        /// 打印消息事件
        /// </summary>
        public static event ShowMessageHandle ShowMessageEvent;

        /// <summary>
        /// locker对象
        /// </summary>
        private static object locker = new object();

        /// <summary>
        /// 打印消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="color">颜色</param>
        public static void ShowMsgColor(string message, Color color)
        {
            lock (locker)
            {
                ShowMessageEvent?.Invoke(message, color);
            }
        }
    }
}
