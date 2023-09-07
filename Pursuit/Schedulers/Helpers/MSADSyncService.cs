
using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using Pursuit.Model;
using Pursuit.API.Controllers;
using Pursuit.Context;
using Pursuit.Schedulers.Helpers.Interfaces;
using AdminDir = Google.Apis.Admin.Directory;
using Google.Apis.Auth.OAuth2;
using Pursuit.Context.AD;
using Pursuit.Helpers;

namespace Pursuit.Schedulers.Helpers
{
    public class MSADSyncService : IMSADSyncService
    {
        private readonly ILogger<MSADSyncService> _logger;
        private readonly IADRepository<GUser> _gRepository;
        public MSADSyncService(IADRepository<GUser> gRepository, ILogger<MSADSyncService> logger)
        {
            //SetUpNLog();
            _gRepository = gRepository;
            _logger = logger;
        }
        public async Task ADSyncService(string schedule)
        {
            try
            {
                try
                {
                    //WORKS
                    //List<ADRecord> _users = ADController.GetListOfAdUsersByGroup("192.168.169.150");
                    //Document must be encrypted before adding to Mongo DB
                    //await _adRepository.UpsertManyAsync(_users);
                    SyncUpGoogleUsers();


                }
                catch (Exception ex)
                {
                    throw ex;
                }


               /* if (CloudStorageAccount.TryParse(StorageConnectionString, out CloudStorageAccount cloudStorageAccount))
                {
                    _logger.LogInformation($"{DateTime.Now}: The UploadToAzureBlobStorage() is called with {schedule} schedule");
                    var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                    var cloudBlobContainer = cloudBlobClient.GetContainerReference(schedule);
                    var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                    await cloudBlockBlob.UploadFromFileAsync(path);
                    _logger.LogInformation($"{DateTime.Now}: The file is been uploaded to the blob with {schedule} schedule");
                }
                else
                {
                    _logger.LogError($"{DateTime.Now}: {CustomConstants.NoStorageConnectionStringSettings}");
                    throw new CustomConfigurationException(CustomConstants.NoStorageConnectionStringSettings);
                }*/


            }
            catch (Exception ex)
            {
                _logger.LogError($"{DateTime.Now}: Exception is occured at UploadToAzureBlobStorage(): {ex.Message}");
                throw new EvolveConfigurationException($"Error when uploading to blob: {ex.Message}");
            }
        }


        private bool SyncUpGoogleUsers()
        {
            IList<AdminDir.directory_v1.Data.User> gUserList;
            var privateKey = "-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCKbGg+8c9dnP0n\nh2N4amF3njgTaOlGr+jebDFDkEvqC3wuGPwbMWkCkpOZt+jtNui+EWTITOFr/2Vu\nyd76EwTkQ1RSdFgh4InA368oUYioz1FC3ftO8yUVP3ZP7SpMgjjKx52A65dm7MW4\nGwsebM5wrpkCTmYhsQ6PBxg0Fq5IL9YVu6OxIMJw+x+kDBFKU5Wp3yAvdRf11RY1\njlTgpSdrXjJ67sk13SNA8sFx+MIB7mPBVANxLufzZ+X4Doi7nbeIhqpoLctY+oub\n9ogNLPBSp7W9vU2ba7R02ZHKoQqRx7ie4Yvr9v3Qa4agoteCMZh5T51OOV2R8zUZ\nuzZzN9j7AgMBAAECggEAN1BXneOJ/joIDV40OP+loCOo+9Sd90G/F/Z6/ykvtMBP\nKKqSP5mQgVcqRTBxEy2wdpdDwyi5oarmkQ15HUwxVbez/9j/CNaNpXWdLErchbyG\nl+ZVkLhntqRr9kdq8jTNVfbLcSNzlk0CO24PFOLc4blbakkC1e7HRw9KNDJmBXEF\nqPsHZnGg4frYBiDWNNOV3znf8F9rJXl2rbwjpFTQo1rwfz/dFouHOSeyBFEiw4HQ\n+bgN4T6t0JAoHzIFgLvCuKZki/XgEUqjcNCOt/nNUIYBI3TI9Hs847K2v/FWzZLS\nfUiabw7cHkp2Mr72SkeUSOWdBFX7LigASRMB78oo+QKBgQDA6CW/4kIa1WMiMcRP\n3RgB2zVTHL5iPB80fbPEIHPEwfWVoSFQACOkowi/wLDfIwcpoCtFhGfu00PvQLRe\nuwkSlI9FiciOKB1rhmwP5OjZm6Wh4/CktVbI+ZBFhjFzEIRM8gHleRFwXKnMloET\n64/KKhhJne1vlCJTftvsJGIvkwKBgQC3sm9E1nrCBkEmuQpASKkZAxNw4UbP4BeQ\nitNhveBoQaXsukiZgh9G8J+hM1gmyHkakdaq6zjuzOx8uN2wReeFBx1zwMbxVUKQ\n6TIB8wjSv8D9+933v5v09Zf5jB9AUe4ismaRB5NsfQrgEXs43/FsyVv5rGfzSc/m\nQJv6QVgB+QKBgD/Mx6dlwm0zg9zsTrwHKIh8om9Bg2nj7oIizNCh1wgNChcZunXG\nBgPOc/dPWHAEGrtWoNkWCHXBY6d+Y+ksvLxra9MY1b7GX6yPQbAkCirmQmp/g7hF\nzVUczO1hi3s9zDPSmnP1jaH206W5ZSlccCrxrySx2bRcbtnkjAHWqq6HAoGAZclP\ncltN5hjFHQnHLluUpzFXImMRc7n+FK939V7a66oEoKmP9M9vOUW3jgD/RW4r/Jb2\n1fpEr72JBIsC+9ugL8wDe9JD6hGOMvGkLgRWzUBHVfSrx826Qv+a2EHWRzOeukcU\nIiSKgcC/t+y31InyIo9okW4Ao4Qw2KrQQtjWRTECgYB1/8UU5QW9T7nrLUbkmc3m\nnEs5DX4kd8usc9aO7hLID2+7opxdy8E3H1PckpOkn2wvZnTx5vXoijOJ+RVjfGcq\n/pAFc3gQkqP4ECDy2MoJPCDPwDsLqnd4+2Vd/LMzkQZvZIYmxr2uVZkGJthJctl6\nuA9eB2azSEHPRCvL0+A7nw==\n-----END PRIVATE KEY-----\n";
            var userName = "admin@evolveaccess.com";
       
            ServiceAccountCredential? credential;

            using (var stream = new FileStream("serviceAccount.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(
                 new string[] { @"https://www.googleapis.com/auth/admin.directory.user" })
                            .CreateWithUser(userName)
                            .UnderlyingCredential as ServiceAccountCredential;
            }

            List<GUser> _users = new List<GUser>();
            using (var directoryService = DirectoryServiceFactory.CreateDirectoryService(credential))
            {
                var request = directoryService.Users.List();
                request.Domain = "evolveaccess.com";
                request.ViewType = Google.Apis.Admin.Directory.directory_v1.UsersResource.ListRequest.ViewTypeEnum.AdminView;
                var response = request.Execute();

                BulkUser usr;
                dynamic varJson;

                foreach (var user in response.UsersValue)
                {
                    varJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);

                    MongoDB.Bson.BsonDocument document
                            = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<MongoDB.Bson.BsonDocument>(varJson);
                    GUser gRec = new GUser();
                    gRec.Email = user.PrimaryEmail ?? "";
                    gRec.UserDocument = document;
                    _users.Add(gRec);
                }
            }

            _gRepository.UpsertManyAsync(_users);
            return true;
        }

/*
        private void SetUpNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "backupclientlogfile_helperservice.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;

            _logger = LogManager.GetCurrentClassLogger();
        }*/
    }
}
