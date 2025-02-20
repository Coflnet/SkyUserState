using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Coflnet.Sky.PlayerState.Services;

namespace Coflnet.Sky.PlayerState.Controllers;
[ApiController]
[Route("[controller]")]
public class ArchiveController : ControllerBase
{
    private readonly IShenStorage storage;

    public ArchiveController(IShenStorage storage)
    {
        this.storage = storage;
    }
    [HttpGet]
    [Route("shen/{year}")]
    public async Task<IEnumerable<ShenHistory>> GetShen(int year)
    {
        return await storage.Get(year);
    }

}

