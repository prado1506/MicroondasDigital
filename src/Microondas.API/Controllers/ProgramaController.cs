using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microondas.Application.Services;
using Microondas.Application.DTOs;

namespace Microondas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgramaController : ControllerBase
{
    private readonly ProgramaService _service;

    public ProgramaController(ProgramaService service) => _service = service;

    [HttpGet]
    public ActionResult<IEnumerable<ProgramaDTO>> GetAll() => Ok(_service.ListarTodos());

    [HttpGet("predefinidos")]
    public ActionResult<IEnumerable<ProgramaDTO>> GetPreDefinidos() => Ok(_service.ListarProgramasPreDefinidos());

    [HttpGet("customizados")]
    public ActionResult<IEnumerable<ProgramaDTO>> GetCustomizados() => Ok(_service.ListarCustomizados());

    [HttpGet("{id}")]
    public ActionResult<ProgramaDTO?> Get(string id)
    {
        var p = _service.ObterPrograma(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public ActionResult<ProgramaDTO> Create([FromBody] CriarProgramaDTO dto)
    {
        var created = _service.CriarPrograma(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Identificador }, created);
    }

    [HttpPost("{id}/iniciar")]
    public ActionResult Iniciar(string id)
    {
        var aqu = _service.IniciarAquecimentoComPrograma(id);
        return Ok(new { aqu.Id });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        _service.DeletarPrograma(id);
        return NoContent();
    }
}