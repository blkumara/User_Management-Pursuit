using MongoDB.Bson;
using MongoDB.Driver;

using Pursuit.Model;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net;
using System.Reflection.Metadata;
using System;
using System.Xml.Linq;
using System.Text;
using System.Data;
using static Pursuit.Utilities.Enums;
using Pursuit.Context.AD;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Globalization;

namespace Pursuit.Context
{
    public class AzureRepository<TAdDoc> : IADRepository<TAdDoc>
    where TAdDoc : IAdDoc
    {
        private readonly IMongoCollection<TAdDoc> _gwsAdCollection;

        public AzureRepository(IADDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _gwsAdCollection = database.GetCollection<TAdDoc>("Azure_AD");
        }

        public virtual IQueryable<TAdDoc> AsQueryable()
        {
            return _gwsAdCollection.AsQueryable();
        }

        public virtual IEnumerable<TAdDoc> FilterBy(
            Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _gwsAdCollection.Find(filterExpression).ToEnumerable();
        }

        public virtual Task RemoveManyAsync(
           Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _gwsAdCollection.DeleteManyAsync(filterExpression);
        }
        public virtual IEnumerable<TAdDoc> NameFilter(string filterExpression)
        {
            // var filter = Builders<TAdDoc>.Filter.Eq("UserDocument._v.GivenName", filterExpression);
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("UserDocument.GivenName", filterExpression);
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }
        public IEnumerable<TAdDoc> PhoneFilter(string filterExpression)
        {

            // var filter = Builders<TAdDoc>.Filter.Eq("UserDocument._v.GivenName", filterExpression);
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("UserDocument.MobilePhone", filterExpression);
            return _gwsAdCollection.Find(filter).ToEnumerable();

        }
        public virtual IEnumerable<TAdDoc> MultiFilter(
       ICollection<gFilterRequest> filters)
        {
            var builder = Builders<TAdDoc>.Filter;
            var filter = builder.Empty;
            var theFilter = builder.Empty;
            var flag = false;
            foreach (gFilterRequest gr in filters)
            {
                string expr = String.Empty;

                if (!string.IsNullOrWhiteSpace(gr.Key))
                {
                    switch (gr.Key.ToUpper())
                    {
                        case "FIRSTNAME"://lambda experession here if other not work for wildcardsearch
                            expr = "UserDocument.GivenName";
                            break;
                        case "LASTNAME":
                            expr = "UserDocument.Surname";
                            break;
                        case "PHONE":
                            expr = "Phone";
                            break;
                        case "EMAIL":
                            expr = "UserDocument.Mail";
                            break;
                        case "MANAGER":
                            expr = "UserDocument.Manager";
                            break;
                        case "DEPT":
                            expr = "UserDocument.Department";
                            break;
                        default:
                            break;
                    }

                    switch (gr.ROperator.ToUpper())
                    {
                        case "EQ":
                            theFilter = builder.Eq(expr, gr.Value);

                            break;
                        case "NEQ":
                            theFilter = builder.Ne(expr, gr.Value);
                            break;
                        case "WC":
                            theFilter = new BsonDocument
                             { { expr, new BsonDocument { { "$regex", gr.Value }, { "$options", "i" } } } };

                            break;

                        default:
                           break;
                    }


                    if (flag)
                    {
                        if (gr.Operator == "AND")
                            filter &= theFilter;
                        else if (gr.Operator == "OR")
                            filter |= theFilter;
                        else
                            throw new Exception("Operator Needed");
                    }
                    else
                    {
                        filter = theFilter;
                        flag = true;
                    }
                }
            }

            try
            {
                return _gwsAdCollection.Find(filter).ToEnumerable();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TAdDoc, bool>> filterExpression,
            Expression<Func<TAdDoc, TProjected>> projectionExpression)
        {
            return _gwsAdCollection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual TAdDoc FindOne(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _gwsAdCollection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TAdDoc> FindOneAsync(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return Task.Run(() => _gwsAdCollection.Find(filterExpression).FirstOrDefaultAsync());

        }
        //Find the data by id
        public virtual TAdDoc FindById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            return _gwsAdCollection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TAdDoc> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
                return _gwsAdCollection.Find(filter).SingleOrDefaultAsync();
            });
        }

        //Insert Data
        public virtual void InsertOne(TAdDoc document)
        {
            _gwsAdCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TAdDoc document)
        {
            return Task.Run(() => _gwsAdCollection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TAdDoc> documents)
        {
            _gwsAdCollection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TAdDoc> documents)
        {
            await _gwsAdCollection.InsertManyAsync(documents);
        }
        //Update data by id
        public void ReplaceOne(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                           .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                           .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            _gwsAdCollection.UpdateOne(filter, update);
        }

        public virtual async Task ReplaceOneAsync(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                          .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                          .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            await _gwsAdCollection.UpdateOneAsync(filter, update);
        }

        public virtual async Task UpsertManyAsync(ICollection<TAdDoc> documents)
        {
            var bulkOps = new List<WriteModel<TAdDoc>>();
            foreach (var record in documents)
            {
                var upsertOne = new ReplaceOneModel<TAdDoc>(
                    Builders<TAdDoc>.Filter.Where(x => x.AzureId == record.AzureId || record.AzureId==null),
                    record)
                { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }
            await _gwsAdCollection.BulkWriteAsync(bulkOps);
        }

        public IEnumerable<TAdDoc> LastNameFilter(string filterExpression)
        {
            throw new NotImplementedException();
        }

        public string DecryptAsync(string key)
        {
            throw new NotImplementedException();
        }
        //Get Distinct List as per Key from DB
        public IList<string> DistinctList(string listKey, string value)
        {
            IList<string> List = null;

            if (listKey == "COMP")
            {

                var filter = new BsonDocument();
                List = _gwsAdCollection.Distinct<string>("UserDocument.CompanyName", filter).ToList<string>();
            }
            if (listKey == "DEPT")
            {
                if (value == null || value == "")
                {
                    var filter = new BsonDocument();
                    List = _gwsAdCollection.Distinct<string>("UserDocument.Department", filter).ToList<string>();

                }
                else
                {
                    var filter = new BsonDocument();

                    var arrayFilters1 = Builders<TAdDoc>.Filter.Eq("UserDocument.CompanyName", value);

                    List = _gwsAdCollection.Distinct<string>("UserDocument.Department", arrayFilters1).ToList<string>();

                }
            }
            if (listKey == "MANG")
            {
                if (value == null || value == "")
                {
                    var filter = new BsonDocument();

                    List = _gwsAdCollection.Distinct<string>("UserDocument.Manager.Id", filter).ToList<string>();

                }
                else
                {
                    var filter = new BsonDocument();
                    //  var dept = "Accounting";
                    var arrayFilters1 = Builders<TAdDoc>.Filter.Eq("UserDocument.Department", value);

                    List = _gwsAdCollection.Distinct<string>("UserDocument.Manager.Id", arrayFilters1).ToList<string>();

                }
            }
            return List;
        }


        public IEnumerable<TAdDoc> DateRangeEmpByDept(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> DeptEmpCount()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> pwdLastSet(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            /* var d1 = startdate.ToFileTimeUtc();
             var d2 = enddate.ToFileTimeUtc();*/

            if (req.Manager != null && req.Manager != "")
            {
                filter = builder.And(builder.Eq("UserDocument.Manager.Id", req.Manager),
                      builder.Ne("UserDocument.Mail", BsonNull.Value),
                 builder.Gte("UserDocument.LastPasswordChangeDateTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.LastPasswordChangeDateTime", new BsonDateTime(enddate))
                );
                return _gwsAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Department != null && req.Department != "")
            {
                filter = builder.And(builder.Eq("UserDocument.Department", req.Department),
                      builder.Ne("UserDocument.Mail", BsonNull.Value),
                 builder.Gte("UserDocument.LastPasswordChangeDateTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.LastPasswordChangeDateTime", new BsonDateTime(enddate))
                );
                return _gwsAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Company != null && req.Company != "")
            {
                filter = builder.And(builder.Eq("UserDocument.CompanyName", req.Company),
                      builder.Ne("UserDocument.Mail", BsonNull.Value),
                 builder.Gte("UserDocument.LastPasswordChangeDateTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.LastPasswordChangeDateTime", new BsonDateTime(enddate))
                );
                return _gwsAdCollection.Find(filter).ToEnumerable();

            }

            /*filter = builder.And(
                  builder.Ne("UserDocument.Properties.mail", BsonNull.Value),
               builder.Gte("UserDocument.Properties.pwdlastset", d1),
               builder.Lte("UserDocument.Properties.pwdlastset", d2)
              );*/
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }

        public IEnumerable<TAdDoc> lastLogOn(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> lastLogOff(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> badPwdCount(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> MGREmpCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> CompHeadCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadListCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadpwdLastSet(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOn(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOff(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadbadPwd(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> logFail(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadAllAttribute(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> msadgetallemp()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> gwsUserCreated(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> gwslastLogOn(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> gwsgetallemp()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> gwsListCount()
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> azurepwdLastSet(DateRangeRequest req)
        {
            if (req.Key == "MANG")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

                var pipeline = new BsonDocument[]
                    {
                    new BsonDocument("$match", new BsonDocument
                                      {
                                         { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                                         { "UserDocument.Department", new BsonDocument("$eq", req.Department) },
                                         { "UserDocument.LastPasswordChangeDateTime", new BsonDocument
                                             {
                                               { "$gte", new BsonDateTime(startdate) },
                                               { "$lte", new BsonDateTime(enddate) }
                                              }
                                         }
                                     }),
                    new BsonDocument("$group", new BsonDocument
                                     {
                                         { "_id", new BsonDocument("manager", "$UserDocument.Manager.Id") },
                                         { "count", new BsonDocument("$sum", 1) }
                                     }),
                    new BsonDocument("$lookup", new BsonDocument
                                     {
                                         { "from", "Azure_AD" },
                                         { "localField", "_id.manager" },
                                         { "foreignField", "UserDocument.Id" },
                                         { "as", "managerInfo" }
                                     }),
                    new BsonDocument("$unwind", "$managerInfo"),
                    new BsonDocument("$group", new BsonDocument
                                    {
                                       { "_id", "$_id.manager" },
                                       { "companyName", new BsonDocument("$first", "$managerInfo.UserDocument.CompanyName") },
                                       { "departmentName", new BsonDocument("$first", "$managerInfo.UserDocument.Department") },
                                       { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                       { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                       { "count", new BsonDocument("$sum", "$count") }
                                    })
                    };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "COMP")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

                var pipeline = new BsonDocument[]
                                {
                                     new BsonDocument("$match", new BsonDocument
                                        {
                                            { "UserDocument.CompanyName", new BsonDocument("$ne", BsonNull.Value) },
                                            { "UserDocument.LastPasswordChangeDateTime", new BsonDocument
                                              {
                                                { "$gte",  new BsonDateTime(startdate) },
                                                { "$lte",  new BsonDateTime(enddate) }
                                               }
                                            }
                                        }),
                                     new BsonDocument("$group", new BsonDocument
                                        {
                                           { "_id", new BsonDocument("company", "$UserDocument.CompanyName") },
                                           { "count", new BsonDocument("$sum", 1) }
                                      }),
                                     new BsonDocument("$group", new BsonDocument
                                      {
                                         { "_id", "$_id.company" },
                                         { "count", new BsonDocument("$sum", "$count") }
                                        })
                         };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "DEPT")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

                var pipeline = new BsonDocument[]
                                {
                                     new BsonDocument("$match", new BsonDocument
                                        {
                                            { "UserDocument.CompanyName", req.Company },
                                            { "UserDocument.LastPasswordChangeDateTime", new BsonDocument
                                              {
                                                { "$gte",  new BsonDateTime(startdate) },
                                                { "$lte",  new BsonDateTime(enddate) }
                                               }
                                            }
                                        }),
                                     new BsonDocument("$group", new BsonDocument
                                        {
                                           { "_id", new BsonDocument("department", "$UserDocument.Department") },
                                           { "count", new BsonDocument("$sum", 1) }
                                      }),
                                     new BsonDocument("$group", new BsonDocument
                                      {
                                         { "_id", "$_id.department" },
                                         { "count", new BsonDocument("$sum", "$count") }
                                        })
                         };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> azureUserCreated(DateRangeRequest req)
        {
            if (req.Key == "MANG")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

                var pipeline = new BsonDocument[]
                    {
                    new BsonDocument("$match", new BsonDocument
                                      {
                                         { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                                         { "UserDocument.Department", new BsonDocument("$eq", req.Department) },
                                         { "UserDocument.CreatedDateTime", new BsonDocument
                                             {
                                               { "$gte", new BsonDateTime(startdate) },
                                               { "$lte", new BsonDateTime(enddate) }
                                              }
                                         }
                                     }),
                    new BsonDocument("$group", new BsonDocument
                                     {
                                         { "_id", new BsonDocument("manager", "$UserDocument.Manager.Id") },
                                         { "count", new BsonDocument("$sum", 1) }
                                     }),
                    new BsonDocument("$lookup", new BsonDocument
                                     {
                                         { "from", "Azure_AD" },
                                         { "localField", "_id.manager" },
                                         { "foreignField", "UserDocument.Id" },
                                         { "as", "managerInfo" }
                                     }),
                    new BsonDocument("$unwind", "$managerInfo"),
                    new BsonDocument("$group", new BsonDocument
                                    {
                                       { "_id", "$_id.manager" },
                                       { "companyName", new BsonDocument("$first", "$managerInfo.UserDocument.CompanyName") },
                                       { "departmentName", new BsonDocument("$first", "$managerInfo.UserDocument.Department") },
                                       { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                       { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                       { "count", new BsonDocument("$sum", "$count") }
                                    })
                    };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "COMP")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

                var pipeline = new BsonDocument[]
                                {
                                     new BsonDocument("$match", new BsonDocument
                                        {
                                            { "UserDocument.CompanyName", new BsonDocument("$ne", BsonNull.Value) },
                                            { "UserDocument.CreatedDateTime", new BsonDocument
                                              {
                                                { "$gte",  new BsonDateTime(startdate) },
                                                { "$lte",  new BsonDateTime(enddate) }
                                               }
                                            }
                                        }),
                                     new BsonDocument("$group", new BsonDocument
                                        {
                                           { "_id", new BsonDocument("company", "$UserDocument.CompanyName") },
                                           { "count", new BsonDocument("$sum", 1) }
                                      }),
                                     new BsonDocument("$group", new BsonDocument
                                      {
                                         { "_id", "$_id.company" },
                                         { "count", new BsonDocument("$sum", "$count") }
                                        })
                         };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "DEPT")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

                var pipeline = new BsonDocument[]
                                {
                                     new BsonDocument("$match", new BsonDocument
                                        {
                                            { "UserDocument.CompanyName", req.Company },
                                            { "UserDocument.CreatedDateTime", new BsonDocument
                                              {
                                                { "$gte",  new BsonDateTime(startdate) },
                                                { "$lte",  new BsonDateTime(enddate) }
                                               }
                                            }
                                        }),
                                     new BsonDocument("$group", new BsonDocument
                                        {
                                           { "_id", new BsonDocument("department", "$UserDocument.Department") },
                                           { "count", new BsonDocument("$sum", 1) }
                                      }),
                                     new BsonDocument("$group", new BsonDocument
                                      {
                                         { "_id", "$_id.department" },
                                         { "count", new BsonDocument("$sum", "$count") }
                                        })
                         };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> azureListCount()
        {
            var pipeline = new BsonDocument[]
                            {
                                new BsonDocument("$match", new BsonDocument("UserDocument.CompanyName", new BsonDocument("$ne", BsonNull.Value))),
                                new BsonDocument("$group", new BsonDocument
                                 {
                                  { "_id", new BsonDocument
                                      {
                                        { "company", "$UserDocument.CompanyName" },
                                        { "department", "$UserDocument.Department" },
                                        { "manager", "$UserDocument.Manager.Id" }
                                       }
                                  },
                                  { "count", new BsonDocument("$sum", 1) }
                                 }),
                                new BsonDocument("$lookup", new BsonDocument
                                  {
                                     { "from", "Azure_AD" },
                                     { "localField", "_id.manager" },
                                     { "foreignField", "UserDocument.Id" },
                                     { "as", "managerInfo" }
                                  }),
                                new BsonDocument("$unwind", "$managerInfo"),
                                new BsonDocument("$group", new BsonDocument
                                 {
                                   { "_id", new BsonDocument
                                       {
                                         { "company", "$_id.company" },
                                         { "department", "$_id.department" }
                                        }
                                    },
                                     { "managers", new BsonDocument("$push", new BsonDocument
                                        {
                                             { "company", "$_id.company" },
                                             { "department", "$_id.department" },
                                             { "manager", "$_id.manager" },
                                             { "managerName", "$managerInfo.Firstname" },
                                             { "managerEmail", "$managerInfo.Email" },
                                             { "count", "$count" }
                                         })
                                     },
                                    { "count", new BsonDocument("$sum", "$count") }
                                 }),
                                 new BsonDocument("$sort", new BsonDocument("_id.company", 1)),
                                 new BsonDocument("$group", new BsonDocument
                                    {
                                        { "_id", "$_id.company" },
                                        { "count", new BsonDocument("$sum", "$count") },
                                        { "departments", new BsonDocument("$push", new BsonDocument
                                          {
                                             { "company", "$_id.company" },
                                             { "department", "$_id.department" },
                                             { "count", "$count" },
                                             { "managers", "$managers" }
                                              })
                                             }
                                     })
                    };
            return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public IEnumerable<TAdDoc> azuregetallemp()
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            string value = null;
            filter = builder.Ne("Email", value);
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }

        public List<BsonDocument> msadCreateSingleData(DateRangeRequest req)
        {
            if (req.Key == "COMP")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                var d1 = startdate.ToFileTimeUtc();
                var d2 = enddate.ToFileTimeUtc();
                var pipeline = new BsonDocument[]
                          {
                         new BsonDocument("$match", new BsonDocument
                         {//Conditions
                           { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                           { "UserDocument.CreatedDateTime", new BsonDocument("$gte", new BsonDateTime(startdate)).Add("$lte", new BsonDateTime(enddate)) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$UserDocument.CompanyName" }
                                }
                          },
                          //fetch The pwdlastset Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };

                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "DEPT")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                var d1 = startdate.ToFileTimeUtc();
                var d2 = enddate.ToFileTimeUtc();
                var pipeline = new BsonDocument[]
                          {
                         new BsonDocument("$match", new BsonDocument
                         {//Conditions
                           { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                           { "UserDocument.Department", new BsonDocument("$eq", req.Department) },

                           { "UserDocument.CreatedDateTime", new BsonDocument("$gte", new BsonDateTime(startdate)).Add("$lte", new BsonDateTime(enddate)) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$UserDocument.Department" }
                                }
                          },
                          //fetch The pwdlastset Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };

                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "MANG")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                var d1 = startdate.ToFileTimeUtc();
                var d2 = enddate.ToFileTimeUtc();
                var pipeline = new BsonDocument[]
                {
                new BsonDocument("$match", new BsonDocument
                {//Conditions
                    { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                    { "UserDocument.Department", new BsonDocument("$eq", req.Department) },
                    { "UserDocument.Manager.Id", new BsonDocument("$eq", req.Manager) },
                   { "UserDocument.CreatedDateTime", new BsonDocument("$gte", new BsonDateTime(startdate)).Add("$lte", new BsonDateTime(enddate)) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$UserDocument.Manager.Id" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "UserDocument.Manager.Id" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.UserDocument.CompanyName") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.UserDocument.Department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count" , new BsonDocument("$sum", "$count") }
                                    })
                                   };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }
        public List<BsonDocument> msadpwdLastSetSingleData(DateRangeRequest req)
        {
            if (req.Key == "COMP")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                var d1 = startdate.ToFileTimeUtc();
                var d2 = enddate.ToFileTimeUtc();
                var pipeline = new BsonDocument[]
                          {
                         new BsonDocument("$match", new BsonDocument
                         {//Conditions
                           { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                           { "UserDocument.LastPasswordChangeDateTime", new BsonDocument("$gte", new BsonDateTime(startdate)).Add("$lte", new BsonDateTime(enddate)) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$UserDocument.CompanyName" }
                                }
                          },
                          //fetch The pwdlastset Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };

                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "DEPT")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                var d1 = startdate.ToFileTimeUtc();
                var d2 = enddate.ToFileTimeUtc();
                var pipeline = new BsonDocument[]
                          {
                         new BsonDocument("$match", new BsonDocument
                         {//Conditions
                           { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                           { "UserDocument.Department", new BsonDocument("$eq", req.Department) },

                           { "UserDocument.LastPasswordChangeDateTime", new BsonDocument("$gte", new BsonDateTime(startdate)).Add("$lte", new BsonDateTime(enddate)) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$UserDocument.Department" }
                                }
                          },
                          //fetch The pwdlastset Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };

                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            if (req.Key == "MANG")
            {
                DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
                var d1 = startdate.ToFileTimeUtc();
                var d2 = enddate.ToFileTimeUtc();
                var pipeline = new BsonDocument[]
                {
                new BsonDocument("$match", new BsonDocument
                {//Conditions
                    { "UserDocument.CompanyName", new BsonDocument("$eq", req.Company) },
                    { "UserDocument.Department", new BsonDocument("$eq", req.Department) },
                    { "UserDocument.Manager.Id", new BsonDocument("$eq", req.Manager) },
                   { "UserDocument.LastPasswordChangeDateTime", new BsonDocument("$gte", new BsonDateTime(startdate)).Add("$lte", new BsonDateTime(enddate)) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$UserDocument.Manager.Id" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "UserDocument.Manager.Id" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.UserDocument.CompanyName") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.UserDocument.Department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count" , new BsonDocument("$sum", "$count") }
                                    })
                                   };
                return _gwsAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadlastLogOnSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadlastLogOffSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadbadPwdSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> createdTime(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();

            if (req.Manager != null && req.Manager != "")
            {
                /* filter = builder.And(
                    builder.Gte("UserDocument.Properties.CreatedDateTime", new BsonDateTime(startdate)),
                    builder.Lte("UserDocument.Properties.CreatedDateTime", new BsonDateTime(enddate))
                   );*/
                filter = builder.And(builder.Eq("UserDocument.Manager.Id", req.Manager),
                      builder.Ne("UserDocument.Mail", BsonNull.Value),
                 builder.Gte("UserDocument.CreatedDateTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.CreatedDateTime", new BsonDateTime(enddate))
                );
                return _gwsAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Department != null && req.Department != "")
            {
                /*  filter = builder.And(
                      builder.Gte("UserDocument.CreatedDateTime", new BsonDateTime(startdate)),
                      builder.Lte("UserDocument.Properties.CreatedDateTime", new BsonDateTime(enddate))
                     );*/
                filter = builder.And(builder.Eq("UserDocument.Department", req.Department),
                      builder.Ne("UserDocument.Mail", BsonNull.Value),
                 builder.Gte("UserDocument.CreatedDateTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.CreatedDateTime", new BsonDateTime(enddate))
                );
                return _gwsAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Company != null && req.Company != "")
            {
                /*  filter = builder.And(
                      builder.Gte("UserDocument.CreatedDateTime", new BsonDateTime(startdate)),
                      builder.Lte("UserDocument.CreatedDateTime", new BsonDateTime(enddate))
                     );*/
                filter = builder.And(builder.Eq("UserDocument.CompanyName", req.Company),
                      builder.Ne("UserDocument.Mail", BsonNull.Value),
                 builder.Gte("UserDocument.CreatedDateTime", new BsonDateTime(startdate)),
                 builder.Lte("UserDocument.CreatedDateTime", new BsonDateTime(enddate))
                );
                return _gwsAdCollection.Find(filter).ToEnumerable();

            }
            return _gwsAdCollection.Find(filter).ToEnumerable();
        }

        
            public Boolean RemoveAsync(string Id)
            {
                var builder = Builders<TAdDoc>.Filter;
                var filter = builder.Eq("UserDocument.Id", Id);


                _gwsAdCollection.DeleteOneAsync(filter);

                return true;
            }
        

        public TAdDoc FindByADId(string id)
        {
            throw new NotImplementedException();
        }

        public Task<TAdDoc> FindByADIdAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}