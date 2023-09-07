
using Pursuit.Model;
using Pursuit.Service;
using Microsoft.AspNetCore.Mvc;

namespace Pursuit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationsService _orgsService;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(OrganizationsService orgsService, ILogger<OrganizationsController> logger)
    {
        _orgsService = orgsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<List<Organization>> Get() =>
        await _orgsService.GetAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> Get(string id)
    {
        var book = await _orgsService.GetAsync(id);

        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public async Task<IActionResult> Post(Organization nOrg)
    {
        await _orgsService.CreateAsync(nOrg);

        return CreatedAtAction(nameof(Get), new { id = nOrg.Id }, nOrg);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Organization uOrg)
    {
        var book = await _orgsService.GetAsync(id);

        if (book is null)
        {
            return NotFound();
        }

        uOrg.Id = book.Id;

        await _orgsService.UpdateAsync(id, uOrg);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var dOrg = await _orgsService.GetAsync(id);

        if (dOrg is null)
        {
            return NotFound();
        }

        await _orgsService.RemoveAsync(id);

        return NoContent();
    }
}