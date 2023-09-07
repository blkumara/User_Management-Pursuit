using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Pursuit.Context;
using Pursuit.Model;
using System.Collections.ObjectModel;
using System.Text;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    [Authorize]
    public class ConnectionController : ControllerBase
    {
        private readonly IPursuitRepository<User> _userRepository;
        private readonly IPursuitRepository<Connection_Setting> _connRepository;
        private readonly ILogger<ConnectionController> _logger;

        public ConnectionController(ILogger<ConnectionController>? logger,IPursuitRepository<Connection_Setting> connRepository,IPursuitRepository<User> userRepository)
        {
            _userRepository = userRepository;
            _connRepository = connRepository;
            _logger = logger;
        }
        //Get Connection List by Type
        [HttpGet("getUserConnByType")]
        
        public async Task<IActionResult> getUserConnByType(string ctype)
        {


            try
            {

                var connections = await Task.Run(() => _connRepository
                       .FilterBy(x => x.Connection_Type == ctype).AsQueryable().FirstOrDefault());


                foreach (var con in connections.Connections)
                {
                    string input = con.DateCreated.ToString();
                    DateTime dateTime = DateTime.Parse(input);
                    string formattedDate = dateTime.ToString("dd-MMM-yyyy");
                    con.Date=formattedDate;
                }
                return Ok(connections);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Received While Fetching Connections" });

            }
        }

        //Get Connection by user Id
        [HttpGet("getUserConnections")]
     
        public async Task<IActionResult> getUserConnections(string id)
        {


            try
            {
              
                var conn_settings = await Task.Run(() => _connRepository
                       .FilterBy(x => x.Id!=null).AsQueryable());
                List<Connection> connections = new List<Connection>();
                foreach (var connection in conn_settings)
                {
                    foreach(var con in connection.Connections)
                    {
                        string input = con.DateCreated.ToString();
                        DateTime dateTime = DateTime.Parse(input);
                        string formattedDate = dateTime.ToString("dd-MMM-yyyy");
                        con.Date = formattedDate;
                        if (con.UserId != null)
                       // if (con.UserId != id)
                        {
                            connections.Add(con);
                        }
                    }
                }
                if (connections.Count > 0)
                {
                    return Ok(connections);
                }
                else { return Ok(new { ErrorCode = "40401", ErrorMessege = "Connection Not Found For User" }); }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Received While Fetching Connections" });

            }
        }
        //API for add or update user connection settings details
        [HttpPost("addUpdateConnSettings")]
      
        public async Task<IActionResult> addUpdateConnSettings( string ctype, Connection con)
        {
            var configBuilder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.env.json")
            .AddEnvironmentVariables();

            var configuration = configBuilder.Build();
            string apidomain = configuration.GetSection("APIUrl").Value;
            string azuredelta =apidomain + "/api/v1/DeltaManager/Azure/deltaInitiate";
            string addelta = apidomain + "/api/v1/DeltaManager/MSAD/deltaInitiate";
            
            try
            {
                if (ctype == "AD")
                {
                    try
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            // Serialize the request body to JSON using JObject
                            JObject requestBody = JObject.FromObject(con);
                            string requestBodyString = requestBody.ToString();

                            // Set the request body
                            HttpContent content = new StringContent(requestBodyString, Encoding.UTF8, "application/json");

                            // Send the POST request
                            HttpResponseMessage response = await httpClient.PostAsync(addelta, content);

                            // Read the response
                            string responseBody = await response.Content.ReadAsStringAsync();

                            var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseBody);


                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                    }
                }
                if (ctype == "Azure")
                {
                    try
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            // Serialize the request body to JSON using JObject
                            JObject requestBody = JObject.FromObject(con);
                            string requestBodyString = requestBody.ToString();

                            // Set the request body
                            HttpContent content = new StringContent(requestBodyString, Encoding.UTF8, "application/json");

                            // Send the POST request
                            HttpResponseMessage response = await httpClient.PostAsync(azuredelta, content);

                            // Read the response
                            string responseBody = await response.Content.ReadAsStringAsync();

                            var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseBody);


                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                    }
                }
               
                if (con.UserId==""||con.UserId==null)
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "UserId Can't Be Null" });
                }
                var connections = await Task.Run(() => _connRepository
                       .FilterBy(x => x.Connection_Type== ctype).AsQueryable().FirstOrDefault());
                if (connections == null)
                {

                    var Conn_Set = new Connection_Setting() { Id = ObjectId.GenerateNewId(), Connection_Type = ctype };
                    con.Id = ObjectId.GenerateNewId();
                    con.Connection_Type = ctype;
                    con.DateCreated = DateTime.Now;
                    Conn_Set.Connections = new Collection<Connection> { con };
                    await _connRepository.InsertOneAsync(Conn_Set);
                    return Ok(Conn_Set);

                }
                else
                {
                    foreach (Connection co in connections.Connections)
                    {
                        if (co.Connection_Name == con.Connection_Name)
                        {
                            return Ok(new { ErrorCode = "409", ErrorMessege = "Connection Name Can't Be Duplicate" });

                        }
                    }
                    con.Connection_Type = ctype;
                    con.DateCreated = DateTime.Now;
                    await _connRepository.UpdateConnectionAsync(ctype, con);
                    var updatedconn = await Task.Run(() => _connRepository
                       .FilterBy(x => x.Connection_Type == ctype).AsQueryable().FirstOrDefault());
                    return Ok(updatedconn);
                }
               

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Received While Adding Connection" });


            }


            return Ok();
        }
        //Update Connection
        [HttpPost("UpdateConnection")]
    
        public async Task<IActionResult> UpdateConnection(string ctype, Connection con)
        {
            try
            {
                con.Connection_Type = ctype;
                var connections = await Task.Run(() => _connRepository
                    .FilterBy(x => x.Connection_Type == ctype).AsQueryable().FirstOrDefault());
                foreach (Connection co in connections.Connections)
                {
                    if (co.Id != con.Id && co.Connection_Name == con.Connection_Name)
                    {
                        return Ok(new { ErrorCode = "409", ErrorMessege = "Connection Name Can't Be Duplicate" });

                    }
                    if(co.Id == con.Id)
                    {
                        con.DateCreated = co.DateCreated;
                    }
                }

                await _connRepository.UpdateConnectionAsync(ctype, con);
                var updatedconn = await Task.Run(() => _connRepository
                   .FilterBy(x => x.Connection_Type == ctype).AsQueryable().FirstOrDefault());
                return Ok(updatedconn);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Received While Updating Connection" });
            }
        }

        //Delete Connection
        [HttpPost("DeleteConnection")]
    
        public async Task<IActionResult> DeleteConnection(string ctype, string connectionId)
        {
            try
            {
                int flag = 0;
                var conn_settings = await Task.Run(() => _connRepository
                        .FilterBy(x => x.Connection_Type == ctype).AsQueryable().FirstOrDefault());


                foreach (Connection con in conn_settings.Connections)
                {
                    if (con.Id == new ObjectId(connectionId))
                        flag++;
                }
                if (flag == 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Connection Not Found" });

                }


                await Task.Run(() => _userRepository.DeleteConnAsync(ctype,connectionId));

                return Ok(new { ResponseCode = "200", ResponseMessege = "Connection Deleted Successfully" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Received While Deleting Connection" });

            }
        }

    }
}
