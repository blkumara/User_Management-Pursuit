using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using MongoDB.Bson;
using Pursuit.Context;
using Pursuit.Model;

using Tavis.UriTemplates;
using Docker.DotNet;
using Docker.DotNet.Models;

using Newtonsoft.Json;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using Microsoft.Graph.Models;
using SharpCompress.Writers;
using SharpCompress.Archives;
using System.ComponentModel;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    public class LogController : ControllerBase
    {

        private readonly IPursuitRepository<Log> _logRepository;

        private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        private readonly ILogger<LogController> _logger;

        public LogController(ILogger<LogController>? logger, IPursuitRepository<Log> logRepository, IPursuitRepository<Admin_Configuration> adminconfigRepository)
        {
            _logRepository = logRepository;
            _adminconfigRepository = adminconfigRepository;
            _logger = logger;
        }


       
        [HttpPost("getLogByLevel")]

        public async Task<IActionResult> getLogByType([FromBody] PaginationParameters paginationParameters)
        {


            try
            {
                var Pnum = int.Parse(paginationParameters.PageNumber);
                var Psize = int.Parse(paginationParameters.PageSize);

                
                if (paginationParameters.Level == "")
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Please  Enter Level Value" });
                }
                if (paginationParameters.Level == "ALL")
                {
                    var log1 = await Task.Run(() => _logRepository.FindlogAsync());
                    var dotNetObjList1 = log1.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                    // Apply pagination

                    var totalCount1 = dotNetObjList1.Count();
                    var totalPages1 = (int)Math.Ceiling(totalCount1 / (double)Psize);
                    var logs1 = dotNetObjList1.Skip((Pnum - 1) * Psize)
                                     .Take(Psize)
                                     .ToList();

                    // Return the paginated result
                    var result1 = new
                    {
                        TotalCount = totalCount1,
                        TotalPages = totalPages1,
                        Logs = logs1
                    };

                    return Ok(result1);
                }
                var log = await Task.Run(() => _logRepository.FindloglevelAsync(paginationParameters.Level));
                var dotNetObjList = log.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                // Apply pagination
              
                var totalCount = dotNetObjList.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)Psize);
                var logs = dotNetObjList.Skip((Pnum - 1) * Psize)
                                 .Take(Psize)
                                 .ToList();

                // Return the paginated result
                var result = new
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    Logs = logs
                };

                return Ok(result);

               
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Logs" });

            }
        }
        [HttpGet("getLog")]

        public async Task<IActionResult> getLog()
        {


            try
            {

                var logs = await Task.Run(() => _logRepository.FindlogAsync());
                var dotNetObjList = logs.ConvertAll(BsonTypeMapper.MapToDotNetValue);

                return Ok(dotNetObjList);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Logs" });

            }
        }

        [HttpGet("deleteLog")]
        public async Task<IActionResult> deleteLog()
        {


            try
            {

                var result = await Task.Run(() => _logRepository.DeleteLogAsync());


                return Ok(result);
            }
            catch (Exception ex)
            {


                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Deleting Logs" });

            }
        }
        [HttpGet("archiveLog")]
        public async Task<IActionResult> archiveLog(string apiuri)
        {


            try
            {
                //get email settings from admin config
                var adminConfig = await Task.Run(() => _adminconfigRepository
                 .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                //get logs
                var logs = await Task.Run(() => _logRepository.FindlogAsync());
                var dotNetObjList = logs.ConvertAll(BsonTypeMapper.MapToDotNetValue);

                //create file formats
                string volumeName = adminConfig.ArchivalInfo.DockerVolume;
                DateTime today = DateTime.Today;
                string todayString = today.ToString("yyyy-MM-dd");
                string jsonData = JsonConvert.SerializeObject(dotNetObjList);
                string targetFilePath = adminConfig.ArchivalInfo.DockerPath+ "/logFile" + todayString + ".txt";
                string zipFilePath = "logArchive" + todayString + ".tar";

                //Store JSON to docker               
                string output = StoreJsonInDockerVolume(volumeName, jsonData, targetFilePath, zipFilePath, apiuri);

                //If log stored successfully delete them from DB
                if(output== "Success")
                {
                    var result = await Task.Run(() => _logRepository.DeleteLogAsync());
                }


                return Ok(output);
            }
            catch (Exception ex)
            {


                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        static string StoreJsonInDockerVolume(string containerId, string jsonData, string targetFilePath, string zipFilePath, string apiuri)
        {
                string tempFilePath = Path.GetTempFileName();
          
                try
                {

                var dockerUri = new Uri(apiuri);
                var dockerConfig = new DockerClientConfiguration(dockerUri);
                DockerClient client = dockerConfig.CreateClient();
                // Write JSON data to a text file

                System.IO.File.WriteAllText(tempFilePath, jsonData);
                    DateTime today = DateTime.Today;
                    string todayString = today.ToString("yyyy-MM-dd");

                    using (Stream tarFileStream = System.IO.File.Create(zipFilePath))
                    using (var tarArchive = TarArchive.Create())
                    {
                        string fileName = Path.GetFileName(targetFilePath);
                        tarArchive.AddEntry(fileName, tempFilePath);
                        tarArchive.SaveTo(tarFileStream, CompressionType.None);
                    }
                    // Copy the zip file to the Docker volume
                    using (FileStream fileStream = System.IO.File.OpenRead(zipFilePath))
                    {
                        var extractArchiveParameters = new ContainerPathStatParameters
                        {
                            Path = "/var/log/"
                        };
                    client.Containers.ExtractArchiveToContainerAsync(containerId, extractArchiveParameters, fileStream).GetAwaiter().GetResult();
                 
                }


                return "Success";


            
             }
            
            catch (Exception ex)
            {


                return ex.Message;

            }
            finally
            {
                // Clean up temporary files
                System.IO.File.Delete(tempFilePath);
                System.IO.File.Delete(zipFilePath);
            }
        }
    }
}
