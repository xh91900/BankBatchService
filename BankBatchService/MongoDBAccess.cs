using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace BankBatchService
{
    public class MongoDBAccess
    {
        public static string ConnString = ConfigurationManager.AppSettings["mongoDBConnString"].ToString();
    }

    /// <summary>
    /// MongoDBAccess
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class MongoDBAccess<T> : MongoDBAccess where T : class
    {
        private MongoClient mClient;

        private IMongoDatabase mDatabase;

        private IMongoCollection<T> mCollection;

        public MongoDBAccess(string dbName, string collectionName)
        {
            try
            {
                //建立连接
                this.mClient = new MongoClient(ConnString);

                //切换到指定的数据库
                this.mDatabase = mClient.GetDatabase(dbName);

                //根据类型获取相应的集合
                this.mCollection = mDatabase.GetCollection<T>(collectionName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 插入一条记录
        public void InsertOne(T param)
        {
            try
            {
                this.mCollection.InsertOne(param);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // 插入多条记录
        public void InsertMany(List<T> paramList)
        {
            if (paramList != null && paramList.Any())
            {
                this.mCollection.InsertMany(paramList);
            }
        }

        /// <summary>
        /// 返回符合过滤条件的集合
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <param name="limit">限制游标返回结果集记录数</param>
        /// <returns>结果集合</returns>
        public List<T> FindAsByFilter(FilterDefinition<T> filter, int? limit)
        {
            try
            {
                return this.mCollection.Find(filter).Limit(limit).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 返回符合条件的集合
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="limit">限制游标返回结果集记录数</param>
        /// <returns></returns>
        public List<T> FindAsByWhere(Expression<Func<T, bool>> condition, int? limit)
        {
            try
            {
                return this.mCollection.Find<T>(condition).Limit(limit).ToList();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 更新集合
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="update">set值</param>
        /// <returns>long</returns>
        public long UpdateDocs(Expression<Func<T, bool>> condition, UpdateDefinition<T> update)
        {
            try
            {
                UpdateResult result = this.mCollection.UpdateMany<T>(condition, update);
                return result.ModifiedCount;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 删除对应文件
        /// </summary>
        /// <param name="condition">条件</param>
        /// <returns>bool</returns>
        public bool DeleteOne(Expression<Func<T, bool>> condition)
        {
            try
            {
                return this.mCollection.DeleteOne(condition).DeletedCount > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 移除相应集合
        /// </summary>
        /// <param name="collectionName">集合名</param>
        /// <returns>long</returns>
        public void DropCollection(string collectionName)
        {
            try
            {
                this.mDatabase.DropCollection(collectionName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
