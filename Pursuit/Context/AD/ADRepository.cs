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
using System.Reflection;
using System.Globalization;

/* =========================================================
    Item Name: Repository Class-ADRepository
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{
    public class ADRepository<TAdDoc> : IADRepository<TAdDoc>
    where TAdDoc : IAdDoc
    {
        private readonly IMongoCollection<TAdDoc> _msAdCollection;

        public ADRepository(IADDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _msAdCollection = database.GetCollection<TAdDoc>("MS_AD");
        }

        public virtual IQueryable<TAdDoc> AsQueryable()
        {
            return _msAdCollection.AsQueryable();
        }

        public virtual IEnumerable<TAdDoc> FilterBy(
            Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _msAdCollection.Find(filterExpression).ToEnumerable();
        }
        public virtual Task RemoveManyAsync(
           Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _msAdCollection.DeleteManyAsync(filterExpression);
        }
        public virtual IEnumerable<TAdDoc> NameFilter(string filterExpression)
        {
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("givenname", filterExpression);

            return _msAdCollection.Find(filter).ToEnumerable();
        }


        public virtual IEnumerable<TAdDoc> PhoneFilter(string filterExpression)
        {
            var builder = Builders<TAdDoc>.Filter;

            var filter = builder.AnyEq("telephonenumber", filterExpression);
            return _msAdCollection.Find(filter).ToEnumerable();
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
                        case "FIRSTNAME":
                            expr = "Firstname";
                            break;
                        case "LASTNAME":
                            expr = "Lastname";
                            break;
                        case "PHONE":
                            expr = "Phone";
                            break;
                        case "EMAIL":
                            expr = "Email";
                            break;
                        case "DEPT":
                            expr = "department";
                            break;
                        case "MANG":
                            expr = "manager";
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

                return _msAdCollection.Find(filter).ToEnumerable();
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
            return _msAdCollection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual TAdDoc FindOne(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return _msAdCollection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TAdDoc> FindOneAsync(Expression<Func<TAdDoc, bool>> filterExpression)
        {
            return Task.Run(() => _msAdCollection.Find(filterExpression).FirstOrDefaultAsync());

        }
        //Find the data by id
        public virtual TAdDoc FindById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            return _msAdCollection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TAdDoc> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
                return _msAdCollection.Find(filter).SingleOrDefaultAsync();
            });
        }

        //Insert Data
        public virtual void InsertOne(TAdDoc document)
        {
            _msAdCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TAdDoc document)
        {
            return Task.Run(() => _msAdCollection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TAdDoc> documents)
        {
            _msAdCollection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TAdDoc> documents)
        {
            await _msAdCollection.InsertManyAsync(documents);
        }
        //Update data by id
        public void ReplaceOne(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                           .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                           .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            _msAdCollection.UpdateOne(filter, update);
        }

        public virtual async Task ReplaceOneAsync(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TAdDoc>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TAdDoc>.Update
                          .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username)
                          .Set("FirstName", user.FirstName).Set("LastName", user.LastName);
            await _msAdCollection.UpdateOneAsync(filter, update);
        }

        public virtual async Task UpsertManyAsync(ICollection<TAdDoc> documents)
        {
            var bulkOps = new List<WriteModel<TAdDoc>>();
            foreach (var record in documents)
            {
                var upsertOne = new ReplaceOneModel<TAdDoc>(
                    Builders<TAdDoc>.Filter.Where(x => x.Email == record.Email),
                    record)
                { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }
            await _msAdCollection.BulkWriteAsync(bulkOps);
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
                List = _msAdCollection.Distinct<string>("company", filter).ToList<string>();
            }
            if (listKey == "DEPT")
            {
                if (value == null || value == "")
                {
                    var filter = new BsonDocument();
                    List = _msAdCollection.Distinct<string>("department", filter).ToList<string>();

                }
                else
                {
                    var filter = new BsonDocument();

                    var arrayFilters1 = Builders<TAdDoc>.Filter.Eq("company", value);

                    List = _msAdCollection.Distinct<string>("department", arrayFilters1).ToList<string>();

                }
            }
            if (listKey == "MANG")
            {
                if (value == null || value == "")
                {
                    var filter = new BsonDocument();

                    List = _msAdCollection.Distinct<string>("manager", filter).ToList<string>();

                }
                else
                {
                    var filter = new BsonDocument();
                    //  var dept = "Accounting";
                    var arrayFilters1 = Builders<TAdDoc>.Filter.Eq("department", value);

                    List = _msAdCollection.Distinct<string>("manager", arrayFilters1).ToList<string>();

                }
            }
            return List;
        }





        public IEnumerable<TAdDoc> DateRangeEmpByDept(DateRangeRequest req)
        {
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();



            var filter = builder.And(
                    builder.Gte("whencreated", new BsonDateTime(startdate)),
                    builder.Lte("whencreated", new BsonDateTime(enddate))
                   );
            return _msAdCollection.Find(filter).ToEnumerable();
        }



        public List<BsonDocument> DeptEmpCount()
        {
            var pipeline = new BsonDocument[]
                {
                     new BsonDocument("$group", new BsonDocument
                      {
                          { "_id", "$department" },
                          { "count", new BsonDocument("$sum", 1) }
                     })
                 };

            return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public IEnumerable<TAdDoc> pwdLastSet(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            Console.WriteLine(startdate);

            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();
            Console.WriteLine(d1);
            Console.WriteLine(d2);
            if (req.Manager != null && req.Manager != "")
            {
                filter = builder.And(builder.Eq("manager", req.Manager),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("pwdlastset", d1.ToString()),
                 builder.Lte("pwdlastset", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Department != null && req.Department != "")
            {
                filter = builder.And(builder.Eq("department", req.Department),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("pwdlastset", d1.ToString()),
                 builder.Lte("pwdlastset", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Company != null && req.Company != "")
            {
                filter = builder.And(builder.Eq("company", req.Company),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("pwdlastset", d1.ToString()),
                 builder.Lte("pwdlastset", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();

            }

            /*filter = builder.And(
                  builder.Ne("UserDocument.Properties.mail", BsonNull.Value),
               builder.Gte("UserDocument.Properties.pwdlastset", d1.ToString()),
               builder.Lte("UserDocument.Properties.pwdlastset", d2)
              );*/
            return _msAdCollection.Find(filter).ToEnumerable();
            
        }

        public IEnumerable<TAdDoc> lastLogOn(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();

            
                
            
            if (req.Manager != null && req.Manager != "")
            {
                filter = builder.And(builder.Eq("manager", req.Manager),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("lastlogon", d1.ToString()),
                 builder.Lte("lastlogon", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Department != null && req.Department != "")
            {
                filter = builder.And(builder.Eq("department", req.Department),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("lastlogon", d1.ToString()),
                 builder.Lte("lastlogon", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();

            }
            if (req.Company != null && req.Company != "")
            {
                filter = builder.And(builder.Eq("company", req.Company),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("lastlogon", d1.ToString()),
                 builder.Lte("lastlogon", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();

            }
            /* filter = builder.And(builder.Ne("UserDocument.Properties.mail", BsonNull.Value),
                    builder.Gte("UserDocument.Properties.lastlogon", d1.ToString()),

                    builder.Lte("UserDocument.Properties.lastlogon", d2)
                   );
             return _msAdCollection.Find(filter).ToEnumerable();*/
            return _msAdCollection.Find(filter).ToEnumerable();
        }

        public IEnumerable<TAdDoc> lastLogOff(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();

            
            
               

            if (req.Manager != null && req.Manager != "")
            {
                filter = builder.And(
                 builder.Eq("manager", req.Manager),
                 builder.Ne("mail", BsonNull.Value),
                 builder.Gte("lastlogoff", d1.ToString()),
                 builder.Lte("lastlogoff", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();
            }
            if (req.Department != null && req.Department != "")
            {
                filter = builder.And(builder.Eq("department", req.Department),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("lastlogoff", d1.ToString()),
                 builder.Lte("lastlogoff", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();
            }
            if (req.Company != null && req.Company != "")
            {
                filter = builder.And(builder.Eq("company", req.Company),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("lastlogoff", d1.ToString()),
                 builder.Lte("lastlogoff", d2.ToString())
                );
                return _msAdCollection.Find(filter).ToEnumerable();
            }
            /*filter = builder.And(
                  builder.Ne("UserDocument.Properties.mail", BsonNull.Value),
                   builder.Gte("UserDocument.Properties.lastlogoff", d1),
                   builder.Lte("UserDocument.Properties.lastlogoff", d2)
                  );
            return _msAdCollection.Find(filter).ToEnumerable();*/
            return _msAdCollection.Find(filter).ToEnumerable();
        }
        public IEnumerable<TAdDoc> logFail(DateRangeRequest req)
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();

           
            
               
            
            if (req.Manager != null || req.Manager != "")
            {
                filter = builder.And(builder.Eq("manager", req.Manager),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("badpasswordtime", d1.ToString()),
                 builder.Lte("badpasswordtime", d2.ToString())
                );

            }
            if (req.Department != null || req.Department != "")
            {
                filter = builder.And(builder.Eq("manager", req.Department),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("badpasswordtime", d1.ToString()),
                 builder.Lte("badpasswordtime", d2.ToString())
                );

            }
            if (req.Company != null || req.Company != "")
            {
                filter = builder.And(builder.Eq("manager", req.Company),
                      builder.Ne("mail", BsonNull.Value),
                 builder.Gte("badpasswordtime", d1.ToString()),
                 builder.Lte("badpasswordtime", d2.ToString())
                );

            }
            /* filter = builder.And(
                    builder.Gte("UserDocument.Properties.badpasswordtime", d1),
                      builder.Ne("UserDocument.Properties.mail", BsonNull.Value),
                    builder.Lte("UserDocument.Properties.badpasswordtime", d2)
                   );*/
            return _msAdCollection.Find(filter).ToEnumerable();
        }
        public IEnumerable<TAdDoc> badPwdCount(DateRangeRequest req)
        {
            var builder = Builders<TAdDoc>.Filter;
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();

            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();

            var filter = builder.And(
                    builder.Gte("badpwdcount", d1.ToString()),
                    builder.Lte("badpwdcount", d2.ToString())
                   );
            return _msAdCollection.Find(filter).ToEnumerable();
        }

        public List<BsonDocument> MGREmpCount()
        {
            var pipeline = new BsonDocument[]
                  {
                     new BsonDocument("$group", new BsonDocument
                      {
                          { "_id", "$manager" },
                          { "count", new BsonDocument("$sum", 1) }
                     })
                   };

            return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public List<BsonDocument> CompHeadCount()
        {
            var pipeline = new BsonDocument[]
                            {
                     new BsonDocument("$group", new BsonDocument
                      {
                          { "_id", "$company" },
                          { "count", new BsonDocument("$sum", 1) }
                     })
                             };

            return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public List<BsonDocument> msadListCount()
        {
                 var pipeline = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("company", new BsonDocument("$ne", BsonNull.Value))),
                new BsonDocument("$group", new BsonDocument {
                    //Condition
                    { "_id", new BsonDocument {
                        { "company", "$company" },
                        { "department", "$department" },
                        { "manager", "$manager" }
                    }},
                    { "count", new BsonDocument("$sum", 1) }
                }),

                new BsonDocument("$lookup", new BsonDocument {
                    { "from", "MS_AD" },
                    { "localField", "_id.manager" },
                    { "foreignField", "distinguishedname" },
                    { "as", "managerInfo" }
                }),

                    new BsonDocument("$unwind", "$managerInfo"),
                    new BsonDocument("$group", new BsonDocument {
                        { "_id", new BsonDocument {
                            { "company", "$_id.company" },
                            { "department", "$_id.department" }
                        }},
                        { "managers", new BsonDocument {
                            { "$push", new BsonDocument {
                                { "company", "$_id.company" },
                                { "department", "$_id.department" },
                                { "manager", "$_id.manager" },
                                { "managerName", "$managerInfo.Firstname" },
                                { "managerEmail", "$managerInfo.Email" },
                                { "count", "$count" }
                            }}
                        }},
                { "count", new BsonDocument("$sum", "$count") }
    }),

                    new BsonDocument("$sort", new BsonDocument("_id.company", 1)),
                    new BsonDocument("$group", new BsonDocument {
                        { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") },
                        { "departments", new BsonDocument {
                            { "$push", new BsonDocument {
                                { "company", "$_id.company" },
                                { "department", "$_id.department" },
                                { "count", "$count" },
                                { "managers", "$managers" }
                            }}
                        }}
                    })
                };


            return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public List<BsonDocument> msadpwdLastSet(DateRangeRequest req)
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
                           { "company", new BsonDocument("$ne", BsonNull.Value) },
                           { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
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

                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           
                           { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
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

                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                   { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count" , new BsonDocument("$sum", "$count") }
                                    })
                                   };
                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadlastLogOn(DateRangeRequest req)
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
                           { "company", new BsonDocument("$ne", BsonNull.Value) },
                           { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
                                }
                          },
                          //fetch The badpasswordtime Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                     })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
                                }
                          },
                          //fetch The lastlogon Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                     })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                   { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The lastlogon Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count" , new BsonDocument("$sum", "$count") }
                                    })
                   };
                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadlastLogOff(DateRangeRequest req)
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
                           { "company", new BsonDocument("$ne", BsonNull.Value) },
                           { "lastlogoff", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
                                }
                          },
                          //fetch The lastlogoff Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };




                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "lastlogoff", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
                                }
                          },
                          //fetch The lastlogoff Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };




                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                   { "lastlogoff", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count", new BsonDocument("$sum", "$count") }
                                    })
                                   };
                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadbadPwd(DateRangeRequest req)
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
                           { "company", new BsonDocument("$ne", BsonNull.Value) },
                           { "badpasswordtime", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
                                }
                          },
                          //fetch The badpasswordtime Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "badpasswordtime", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
                                }
                          },
                          //fetch The badpasswordtime Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                {
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                   { "badpasswordtime", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {//Condition
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count", new BsonDocument("$sum", "$count") }
                                    })
                                   };

                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }
        //It will give pwd last set and last logon count respective to company,dept and manager with hierarchie
        public List<BsonDocument> msadAllAttribute(DateRangeRequest req)
        {
            DateTime startdate = DateTime.ParseExact(req.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            DateTime enddate = DateTime.ParseExact(req.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToUniversalTime();
            var d1 = startdate.ToFileTimeUtc();
            var d2 = enddate.ToFileTimeUtc();
            var pipeline = new BsonDocument[]
                {
                  new BsonDocument("$match", new BsonDocument
                  {//Conditions
                         { "company", new BsonDocument("$ne", BsonNull.Value) },
                         { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) },
                         { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                  }),
                  new BsonDocument("$group", new BsonDocument
                 {
                     { "_id", new BsonDocument
                            {//Main Document to be fetched
                                 { "company", "$company" },
                                 { "department", "$department" },
                                 { "manager", "$manager" }
                            }
                     },
                     //Pwd last set and last log on counts
                     { "pwdlastcount", new BsonDocument("$sum", 1) },
                     { "logonlastcount", new BsonDocument("$sum", 2) }  
                  }),
                new BsonDocument("$lookup", new BsonDocument
                {//Taking manager whole document for more details extraction
                       { "from", "MS_AD" },
                       { "localField", "_id.manager" },
                       { "foreignField", "distinguishedname" },
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
                            { "pwdlastcount", "$pwdlastcount" },
                            { "logonlastcount", new BsonDocument("$ifNull", new BsonArray(new BsonValue[] { "$logonlastcount", 0 } )) }
                           
                     })
                     },
                     { "pwdlastcount", new BsonDocument("$sum", "$pwdlastcount") },
                     { "logonlastcount", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray(new BsonValue[] { "$logonlastcount", 0 }))) }
        

                 }),
                 new BsonDocument("$sort", new BsonDocument("_id.company", 1)),
                 new BsonDocument("$group", new BsonDocument
                 {//Final output array formation
                    { "_id", "$_id.company" },
                    { "pwdlastcount", new BsonDocument("$sum", "$pwdlastcount") },
                    { "logonlastcount", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray(new BsonValue[] { "$logonlastcount", 0 }))) },
                    { "departments", new BsonDocument("$push", new BsonDocument
                        {
                            { "company", "$_id.company" },
                            { "department", "$_id.department" },
                            { "pwdlastcount", "$pwdlastcount" },
                            { "logonlastcount","$logonlastcount"},
                            { "managers", "$managers" }
                    })
                 }
             })
            };

            return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        public IEnumerable<TAdDoc> msadgetallemp()
        {
            FilterDefinition<TAdDoc> filter = null;
            var builder = Builders<TAdDoc>.Filter;
            string value = null;
            filter = builder.Ne("Email", value);
            return _msAdCollection.Find(filter).ToEnumerable();
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
            throw new NotImplementedException();
        }

        public List<BsonDocument> azureUserCreated(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> azureListCount()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAdDoc> azuregetallemp()
        {
            throw new NotImplementedException();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
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

                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "department", new BsonDocument("$eq", req.Department) },

                           { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
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

                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                    { "manager", new BsonDocument("$eq", req.Manager) },
                   { "pwdlastset", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count" , new BsonDocument("$sum", "$count") }
                                    })
                                   };
                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadlastLogOnSingleData(DateRangeRequest req)
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
                                }
                          },
                          //fetch The badpasswordtime Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                     })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "department", new BsonDocument("$eq", req.Department) },
                           { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
                                }
                          },
                          //fetch The lastlogon Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                     })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                     { "manager", new BsonDocument("$eq", req.Manager) },
                   { "lastlogon", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The lastlogon Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count" , new BsonDocument("$sum", "$count") }
                                    })
                   };
                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadlastLogOffSingleData(DateRangeRequest req)
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "lastlogoff", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
                                }
                          },
                          //fetch The lastlogoff Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };




                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "department", new BsonDocument("$eq", req.Department) },
                           { "lastlogoff", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
                                }
                          },
                          //fetch The lastlogoff Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };




                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                    { "manager", new BsonDocument("$eq", req.Manager) },
                   { "lastlogoff", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count", new BsonDocument("$sum", "$count") }
                                    })
                                   };
                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public List<BsonDocument> msadbadPwdSingleData(DateRangeRequest req)
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "badpasswordtime", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "company", "$company" }
                                }
                          },
                          //fetch The badpasswordtime Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.company" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                           { "company", new BsonDocument("$eq", req.Company) },
                           { "department", new BsonDocument("$eq", req.Department) },
                           { "badpasswordtime", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                         }),
                        new BsonDocument("$group", new BsonDocument
                        {
                          { "_id", new BsonDocument
                               {
                                   { "department", "$department" }
                                }
                          },
                          //fetch The badpasswordtime Count
                        { "count", new BsonDocument("$sum", 1) }
                       }),
                      new BsonDocument("$group", new BsonDocument
                      {//Final output array formation
                         { "_id", "$_id.department" },
                        { "count", new BsonDocument("$sum", "$count") }

                    })
                 };


                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
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
                {
                    { "company", new BsonDocument("$eq", req.Company) },
                    { "department", new BsonDocument("$eq", req.Department) },
                     { "manager", new BsonDocument("$eq", req.Manager) },
                   { "badpasswordtime", new BsonDocument("$gte", d1.ToString()).Add("$lte", d2.ToString()) }
                    }),
                        new BsonDocument("$group", new BsonDocument
                         {//Condition
                             { "_id", new BsonDocument
                             {
                                 { "manager", "$manager" }
                                }
                            },
                             //fetch The badpasswordtime Count
                            { "count", new BsonDocument("$sum", 1) }
                             }),
                                 new BsonDocument("$lookup", new BsonDocument
                                  {//Taking manager whole document for more details extraction
                                    { "from", "MS_AD" },
                                    { "localField", "_id.manager" },
                                    { "foreignField", "distinguishedname" },
                                    { "as", "managerInfo" }
                                   }),

                                    new BsonDocument("$unwind", "$managerInfo"),
                                    new BsonDocument("$group", new BsonDocument
                                    {//Final output array formation
                                        { "_id", "$_id.manager" },
                                        { "companyName", new BsonDocument("$first", "$managerInfo.company") },
                                        { "departmentName", new BsonDocument("$first", "$managerInfo.department") },
                                        { "managerName", new BsonDocument("$first", "$managerInfo.Firstname") },
                                        { "managerEmail", new BsonDocument("$first", "$managerInfo.Email") },
                                        { "count", new BsonDocument("$sum", "$count") }
                                    })
                                   };

                return _msAdCollection.Aggregate<BsonDocument>(pipeline).ToList();
            }
            return null;
        }

        public IEnumerable<TAdDoc> createdTime(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public List<BsonDocument> msadCreateSingleData(DateRangeRequest req)
        {
            throw new NotImplementedException();
        }

        public Boolean RemoveAsync(string EmailId)
        {
            var builder = Builders<TAdDoc>.Filter;
            var filter = builder.Eq("Email", EmailId);


            _msAdCollection.DeleteOneAsync(filter);

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