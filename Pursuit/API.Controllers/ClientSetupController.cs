using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Pursuit.Context;
using Pursuit.Context.ConfigFile;
using Pursuit.Helpers;
using Pursuit.Model;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    public class ClientSetupController : ControllerBase
    {
        private readonly IWritableOptions<PursuitDBSettings> _writableSettings;
        private readonly IWritableOptions<ADDBSettings> _writableADDB;
        private readonly ILogger<ClientSetupController> _logger;
        readonly string url = "";
        private readonly IOptionsSnapshot<ADDBSettings> _optionsSnapshot;
        private readonly IWritableOptions<ConfigSettings> _writableconfig;
        private readonly IWritableOptions<Serilogs> _writablelog;
        private readonly IOptionsMonitor<PursuitDBSettings> _dbOptionsMonitor;
        private readonly IOptionsMonitor<ADDBSettings> _aDOptionsMonitor;

        
        private IConfigurationRoot configRoot { get; set; }

        public ClientSetupController(IWritableOptions<Serilogs> writablelog,IWritableOptions<ConfigSettings> writableconfig,
            ILogger<ClientSetupController>? logger, IWritableOptions<PursuitDBSettings> writableSettings, IWritableOptions<ADDBSettings> writableADDB,
            IOptionsMonitor<PursuitDBSettings> DBOptionsMonitor, IOptionsMonitor<ADDBSettings> ADOptionsMonitor, IOptionsSnapshot<ADDBSettings> optionsSnapshot)
        {
            _writableSettings = writableSettings;
            _logger = logger;
            _writableADDB = writableADDB;
            _optionsSnapshot = optionsSnapshot;
           
            _dbOptionsMonitor = DBOptionsMonitor;
            _aDOptionsMonitor = ADOptionsMonitor;
            _writableconfig = writableconfig;
            _writablelog = writablelog;
        }
       
        [HttpPost("SetupDomain")]
        public async Task<IActionResult> SetupDomain(ConfigSettings configs)
        {
            try
            {
                //Verify Domain
                Ping ping = new Ping();
                PingReply reply = ping.Send(configs.DomainName); 
                if (reply.Status == IPStatus.Success)
                {
                    //Add Domain to appsetting.json
                    _writableconfig.Update(opt =>
                    {
                        opt.DomainName = configs.DomainName;


                    });
                    return Ok(new { ResponseCode = "200", ResponseMessege = "Domain Name Has Been Setup Successfully" });

                }
                else
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Domain Cannot be Accessed, Please Provide Correct Domain" });
                }
               
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Domain Cannot be Accessed, Please Provide Correct Domain" });

            }
        }

        [HttpGet("GetDomain")]
        public async Task<IActionResult> GetDomain()
        {
            try
            {
                var domain = _writableconfig.Value;
                return Ok(domain);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Domain" });

            }
        }
        [HttpGet("GetDBDetails")]
        public async Task<IActionResult> GetDBDetails()
        {
            try
            {
                PursuitDBSettings pDB = new PursuitDBSettings();
                var pursuitdb = _writableSettings.Value;

                return Ok(pursuitdb);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching DB Details" });

            }
        }
        [HttpPost("SetupDBDetails")]
        public async Task<IActionResult> UpdateDBSettings(PursuitDBSettings pDB)
        {
            try
            {
                string logconstring = "";
                if ((pDB.ConnectionUsername != null && pDB.ConnectionUsername != "") && (pDB.ConnectionPassword != null && pDB.ConnectionPassword != ""))
                {
                    string password = HttpUtility.UrlEncode(pDB.ConnectionPassword);
                    pDB.ConnectionString = "mongodb://" + pDB.ConnectionUsername + ":" + password + "@" + pDB.ConnectionDomain + ":" + pDB.ConnectionPort + "/";
                     logconstring = pDB.ConnectionString + pDB.DatabaseName + "?authSource=admin&authMechanism=SCRAM-SHA-1";
                }

                else
                {
                    pDB.ConnectionString = "mongodb://" + pDB.ConnectionDomain + ":" + pDB.ConnectionPort + "/";
                    logconstring = pDB.ConnectionString + pDB.DatabaseName;
                }
                if (!checkDBConnection(pDB))
                    return Ok(new { ErrorCode = "400", ErrorMessege = "DB Connection String Is Not Valid!. Please Make Sure The Server Exists." });

                _writableSettings.Update(opt =>
                {
                    opt.ConnectionDomain = pDB.ConnectionDomain;
                    opt.ConnectionPort=pDB.ConnectionPort;
                    if ((pDB.ConnectionUsername != null && pDB.ConnectionUsername != "") && (pDB.ConnectionPassword != null && pDB.ConnectionPassword != ""))
                    {
                        opt.ConnectionUsername = pDB.ConnectionUsername;
                        opt.ConnectionPassword = pDB.ConnectionPassword;
                    }
                    else
                    {
                        opt.ConnectionUsername = "";
                        opt.ConnectionPassword = "";
                    }
                    opt.ConnectionString = pDB.ConnectionString;
                    opt.DatabaseName = pDB.DatabaseName;
                    opt.CollectionName = pDB.CollectionName;
                    opt.AdminCollectionName= pDB.AdminCollectionName;
                    opt.EmailCollectionName= pDB.EmailCollectionName;
                    opt.LogCollectionName = "log";

                });
                _writableADDB.Update(opt =>
                {
                    opt.ConnectionString = pDB.ConnectionString;
                    opt.DatabaseName = "ADData";
                    opt.MSADCollectionName = "MS_AD"; 
                    opt.AzureADCollectionName = "Azure_AD";
                    opt.GWSADCollectionName = "GWS_AD";
                });
              
                _writablelog.Update(opt =>
                {
                 
                     opt.Using.Append("Serilog.Sinks.MongoDB");
                     opt.MinimumLevel.Default = "Debug";
                     opt.MinimumLevel.Override.Microsoft = "Warning";
                     opt.MinimumLevel.Override.System = "Warning";
                     opt.WriteTo.FirstOrDefault().Name = "MongoDBBson";
                    opt.WriteTo.FirstOrDefault().Args.databaseUrl
                    = logconstring;
                     opt.WriteTo.FirstOrDefault().Args.collectionName = "log";
                     opt.WriteTo.FirstOrDefault().Args.cappedMaxSizeMb = "50";
                     opt.WriteTo.FirstOrDefault().Args.cappedMaxDocuments= "1000";

                   
                });
            
                //configRoot.Reload();

                //We need to implement auto reload for Config File
                //Microsoft.Extensions.Configuration.ConfigurationManager.RefreshSection("configuration");
                //Properties.Settings.Default.Reload();

              
                if (!setupMongoDB(pDB.ConnectionString, pDB.DatabaseName))
                    return Ok(new { ErrorCode = "400", ErrorMessege = "DB Connection String Is Valid. Can Not Create DB And/Or Collections." });

                return Ok(new { ResponseCode = "200", ResponseMessege = "DB Has Been Setup Successfully" });

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Setting DB Details." });

            }
        }

        [HttpPost("ConfigureADDB")]
        public async Task<IActionResult> SetupADDBConnections(ADDBSettings aDB)
        {

            try
            {
                _writableADDB.Update(opt =>
                {
                    opt.ConnectionString = aDB.ConnectionString;
                    opt.DatabaseName = aDB.DatabaseName;
                    opt.MSADCollectionName = aDB.MSADCollectionName;
                    opt.AzureADCollectionName = aDB.AzureADCollectionName;
                    opt.GWSADCollectionName = aDB.GWSADCollectionName;
                });
                return Ok(new { ResponseCode = "200", ResponseMessege = "AD DB Connection Has Been Setup Successfully" });

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Setup AD DB Connections" });

            }
        }
        [HttpPost("TestDBConnection")]
        public async Task<IActionResult> TestDBConnection(PursuitDBSettings pDB)
        {

            try
            {
                if ((pDB.ConnectionUsername != null && pDB.ConnectionUsername != "") && (pDB.ConnectionPassword != null && pDB.ConnectionPassword != ""))
                {
                    string password = HttpUtility.UrlEncode(pDB.ConnectionPassword);
                    pDB.ConnectionString = "mongodb://" + pDB.ConnectionUsername + ":" + pDB.ConnectionPassword + "@" + pDB.ConnectionDomain + ":" + pDB.ConnectionPort;
                   
                }
                else
                {
                    pDB.ConnectionString = "mongodb://"+ pDB.ConnectionDomain + ":" + pDB.ConnectionPort + "/";
                    
                }

                if (!checkDBConnection(pDB))
                {
                    return Ok(new { ErrorCode = "400", ErrorMessege = "DB Connection String Is Not Valid. Please Make Sure The Server Exists." });
                }
                return Ok(new { ResponseCode = "200", ResponseMessege = "DB Connected Successfully" });

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Database Testing Failed" });

            }
        }
        private bool checkDBConnection(PursuitDBSettings pdb)
        {
            if ((pdb.ConnectionUsername != null && pdb.ConnectionUsername != "") && (pdb.ConnectionPassword != null && pdb.ConnectionPassword != ""))
            {
                var credentials = MongoCredential.CreateCredential(
                 databaseName: pdb.DatabaseName,
                 username: pdb.ConnectionUsername,
                 password: pdb.ConnectionPassword
             );
                var server = new MongoServerAddress(host: pdb.ConnectionDomain, port: Int16.Parse(pdb.ConnectionPort));

                var mongoClientSettings = new MongoClientSettings
                {
                    Credential = credentials,
                    Server = server,
                    ConnectionMode = ConnectionMode.Standalone,
                    ServerSelectionTimeout = TimeSpan.FromSeconds(3)
                };

                var database1 = new MongoClient(mongoClientSettings);
                database1.StartSession();
               // Thread.Sleep(2000);
                var sConnected1 = database1.Cluster.Description.State.ToString();

                if (sConnected1 != "Connected")
                {
                   
                    return false;
                }

              
                return true;
            }
            else
            {
                var database = new MongoClient(pdb.ConnectionString);
                Thread.Sleep(500);
                var sConnected = database.Cluster.Description.State.ToString();

                if (sConnected != "Connected")
                {
                    return false;
                }
                Console.WriteLine(sConnected);
                return true;
            }
        }

        private bool setupMongoDB(string conString, string dbName)
        {
          
            var dbClient = new MongoClient(conString);
            IMongoDatabase mongoDB = dbClient.GetDatabase(dbName);

            //For Creating AppUsers -- DB will not be created till any one collection is created.
            //So AppUsers is default and compulsary collection
            var _appUsers = mongoDB.GetCollection<User>("AppUsers");
            _appUsers.InsertOne(new Model.User());
            _appUsers.DeleteMany(Builders<User>.Filter.Eq(x => x.Username, null));
            //AdminConfig is default and compulsary collection
            var _adminConfig = mongoDB.GetCollection<Admin_Configuration>("AdminConfig");
            _adminConfig.InsertOne(new Model.Admin_Configuration());
            _adminConfig.DeleteMany(Builders<Admin_Configuration>.Filter.Eq(x => x.Notification_Preference, null));
            //AdminConfig is default and compulsary collection
            var _emailconfig = mongoDB.GetCollection<EmailContentConfiguration>("MailTemplates");
            _emailconfig.InsertOne(new Model.EmailContentConfiguration());
            _emailconfig.DeleteMany(Builders<EmailContentConfiguration>.Filter.Eq(x => x.EmailType, null));

            //Log is default and compulsary collection
            var _logconfig = mongoDB.GetCollection<Log>("log");
            _logconfig.InsertOne(new Model.Log());
            _logconfig.DeleteMany(Builders<Log>.Filter.Eq(x => x.Level, null));


            _logger.LogInformation("AppUsers and AdminConfig Collections have been created");

            return true;
        }
    }
}