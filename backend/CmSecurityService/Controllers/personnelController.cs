namespace CmSecurityService.Controllers;
using CmSecurityService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/security/v1/personnel")]
public class personnelController : ControllerBase
{
    private readonly AppDbContext _context; //access to db through EF core

    public personnelController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPersonnel() //get request
    {
        var officers = await _context.Officers.ToListAsync(); //db query translated to sql by ef core
        return Ok(officers);
    }
}