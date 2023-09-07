using MongoDB.Bson;
using MongoDB.Driver;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Pursuit.Model;
using System.Linq.Expressions;
/* =========================================================
    Item Name: Repository Interface-IADRepository
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{

    public interface IADRepository<TAdDoc>
        where TAdDoc : IAdDoc //where TDocument : IDocument.SubType(ISubDocument)
    {
        //All methods implemented in Pursuit.Context.ADRepository
        IQueryable<TAdDoc> AsQueryable();

        IEnumerable<TAdDoc> FilterBy(
            Expression<Func<TAdDoc, bool>> filterExpression);

        IEnumerable<TAdDoc> NameFilter(string filterExpression);

        IEnumerable<TAdDoc> PhoneFilter(string filterExpression);

        IEnumerable<TAdDoc> MultiFilter(ICollection<gFilterRequest> filters);

        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TAdDoc, bool>> filterExpression,
            Expression<Func<TAdDoc, TProjected>> projectionExpression);

        TAdDoc FindOne(Expression<Func<TAdDoc, bool>> filterExpression);

        Task<TAdDoc> FindOneAsync(Expression<Func<TAdDoc, bool>> filterExpression);
        Task RemoveManyAsync(Expression<Func<TAdDoc, bool>> filterExpression);

        Boolean RemoveAsync(String Id);
        //Find the user data by using ID

        TAdDoc FindById(string id);

        Task<TAdDoc> FindByIdAsync(string id);


        TAdDoc FindByADId(string id);

        Task<TAdDoc> FindByADIdAsync(string id);

        //Insert user whole details

        void InsertMany(ICollection<TAdDoc> documents);

        Task InsertManyAsync(ICollection<TAdDoc> documents);

        Task UpsertManyAsync(ICollection<TAdDoc> documents);

        void ReplaceOne(string userId, User user);

        Task ReplaceOneAsync(string userId, User user);

        IList<string> DistinctList(string listKey, string deptName);

      

        public IEnumerable<TAdDoc> DateRangeEmpByDept(DateRangeRequest req);
        public List<BsonDocument> DeptEmpCount();
        public List<BsonDocument> msadListCount();
        public List<BsonDocument>gwsListCount();

        public List<BsonDocument>azureListCount();
        public List<BsonDocument> MGREmpCount();
        public List<BsonDocument> CompHeadCount();
        public IEnumerable<TAdDoc> msadgetallemp();
        public IEnumerable<TAdDoc> gwsgetallemp();
        public IEnumerable<TAdDoc> azuregetallemp();
        public IEnumerable<TAdDoc> pwdLastSet(DateRangeRequest req);
        public IEnumerable<TAdDoc> lastLogOn(DateRangeRequest req);
        public IEnumerable<TAdDoc> lastLogOff(DateRangeRequest req);
        public IEnumerable<TAdDoc> logFail(DateRangeRequest req);
        public IEnumerable<TAdDoc> badPwdCount(DateRangeRequest req);
        public IEnumerable<TAdDoc> createdTime(DateRangeRequest req);

        public List<BsonDocument> msadpwdLastSet(DateRangeRequest req);
        public List<BsonDocument> msadlastLogOn(DateRangeRequest req);
        public List<BsonDocument> msadlastLogOff(DateRangeRequest req);
        public List<BsonDocument> msadbadPwd(DateRangeRequest req);

        public List<BsonDocument> msadpwdLastSetSingleData(DateRangeRequest req);
        public List<BsonDocument> msadlastLogOnSingleData(DateRangeRequest req);
        public List<BsonDocument> msadlastLogOffSingleData(DateRangeRequest req);
        public List<BsonDocument> msadbadPwdSingleData(DateRangeRequest req);
        public List<BsonDocument> msadCreateSingleData(DateRangeRequest req);

        public List<BsonDocument> gwsUserCreated(DateRangeRequest req);
        public List<BsonDocument> gwslastLogOn(DateRangeRequest req);
        public List<BsonDocument> azurepwdLastSet(DateRangeRequest req);
        public List<BsonDocument> azureUserCreated(DateRangeRequest req);
        public List<BsonDocument> msadAllAttribute(DateRangeRequest req);


    }
}