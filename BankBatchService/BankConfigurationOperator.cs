using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankBatchService
{
    /// <summary>
    /// BANK_CONFIG配置库
    /// </summary>
    public class BankConfigurationOperator
    {
        /// <summary>
        /// 获取S_PARA集合中的配置
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public List<Spara> GetSparaByKey(string key)
        {
            MongoDBAccess<Spara> mongoAccess = new MongoDBAccess<Spara>(SYSConstant.BANK_CONFIG, SYSConstant.S_PARA);
            return mongoAccess.FindAsByWhere(p => p.Key == key, 0);
        }

        /// <summary>
        /// 获取配置库中所有的BankAgent
        /// </summary>
        /// <returns></returns>
        public List<BankAgent> GetAllBankAgent()
        {
            MongoDBAccess<BankAgent> mongoAccess = new MongoDBAccess<BankAgent>(SYSConstant.BANK_CONFIG, SYSConstant.BANK_AGENT);
            return mongoAccess.FindAsByFilter(null, 0);
        }
    }
}
