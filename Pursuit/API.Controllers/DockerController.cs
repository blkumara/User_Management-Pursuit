using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Docker.DotNet;
using Docker.DotNet.Models;
using Pursuit.Model;
using Azure;
using System;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]

    public class DockerController : ControllerBase
    {
        /* private readonly DockerClient _dockerClient;

         public DockerController()
         {
             // Configure Docker daemon connection
             var dockerUri = new Uri("https://demo.evolveaccess.com:9443");
             var dockerConfig = new DockerClientConfiguration(dockerUri);
             _dockerClient = dockerConfig.CreateClient();
         }

         [HttpGet("containers")]
         // [Route("containers")]
         public async Task<IActionResult> GetContainers()
         {
             try
             {
                 // Make Docker daemon API call to list containers
                 var containers = await _dockerClient.Containers.ListContainersAsync(
                     new ContainersListParameters { All = true });

                 return Ok(containers);
             }
             catch (Exception ex)
             {
                 // Handle any exceptions
                 return StatusCode(500, $"Error retrieving containers: {ex.Message}");
             }
         }

         [HttpPost]
         [Route("containers")]
         public async Task<IActionResult> CreateContainer([FromBody] CreateContainerRequest request)
         {
             try
             {
                 // Create container configuration
                 var config = new CreateContainerParameters
                 {
                     Image = request.Image,
                     Cmd = request.Cmd,
                     Name = request.Name
                 };

                 // Make Docker daemon API call to create a container
                 var response = await _dockerClient.Containers.CreateContainerAsync(config);

                 return Ok(response);
             }
             catch (Exception ex)
             {
                 // Handle any exceptions
                 return StatusCode(500, $"Error creating container: {ex.Message}");
             }
         }

         // Additional actions can be added for other Docker API calls as needed
     }
     public class CreateContainerRequest
     {
         public string Image { get; set; }
         public IList<string> Cmd { get; set; }
         public string Name { get; set; }
     }*/

        [HttpPost("getListOfImages")]
        public async Task<IActionResult> GetListOfImages([FromBody] DockerContainerRequest req)
        {
            try
            {

                // DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                DockerClient client = new DockerClientConfiguration(
          new Uri(req.Uri))
           .CreateClient();
                IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
          new ContainersListParameters()
          {
              Limit = 10,
          });
                return Ok(containers);
            }
            catch (Exception ex)
            {


                return Ok(ex.Message);

            }
        }

        [HttpPost("runContainer")]
        public async Task<IActionResult> RunContainer([FromBody] DockerContainerRequest req)
        {
            try
            {
                //DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

                DockerClient client = new DockerClientConfiguration(
        new Uri(req.Uri))
         .CreateClient();

                await client.Containers.StartContainerAsync(req.ContainerId, new ContainerStartParameters());

                return Ok(req.ContainerId + " Started");
            }

            catch (Exception ex)
            {


                return Ok(ex.Message);

            }
        }
        [HttpPost("stopContainer")]
        public async Task<IActionResult> StopContainer([FromBody] DockerContainerRequest req)
        {
            try
            {
                // DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                DockerClient client = new DockerClientConfiguration(
     new Uri(req.Uri))
      .CreateClient();
                await client.Containers.StopContainerAsync(req.ContainerId, new ContainerStopParameters());

                return Ok(req.ContainerId + " Stopped");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }
        [HttpPost("deleteContainer")]
        public async Task<IActionResult> deleteContainer([FromBody] DockerContainerRequest req)
        {
            try
            {
                // DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                DockerClient client = new DockerClientConfiguration(
     new Uri(req.Uri))
      .CreateClient();
                await client.Containers.RemoveContainerAsync(req.ContainerId, new ContainerRemoveParameters());

                return Ok(req.ContainerId + " Removed");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

    }
}
