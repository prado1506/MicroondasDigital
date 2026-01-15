using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microondas.Application.Services;
using Microondas.Application.DTOs;

namespace Microondas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AquecimentoController : ControllerBase
{
    private readonly AquecimentoService _service;

    public AquecimentoController(AquecimentoService service) => _service = service;

    [HttpPost]
    [Route("criar")]
    public ActionResult<AquecimentoDTO> Criar([FromBody] CriarAquecimentoDTO dto)
    {
        var res = _service.CriarAquecimento(dto);
        return CreatedAtAction(nameof(Obter), new { id = res.Id }, res);
    }

    [HttpPost("criar-com-caractere")]
    public ActionResult<AquecimentoDTO> CriarComCaractere([FromBody] CriarAquecimentoDTO dto, [FromQuery] char caractere)
    {
        var res = _service.CriarAquecimentoComCaractere(dto, caractere);
        return CreatedAtAction(nameof(Obter), new { id = res.Id }, res);
    }

    [HttpGet("{id:int}")]
    public ActionResult<AquecimentoDTO?> Obter(int id)
    {
        var res = _service.ObterAquecimento(id);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost("{id:int}/simular")]
    public ActionResult<AquecimentoDTO?> SimularPassagemTempo(int id)
    {
        var res = _service.SimularPassagemTempo(id);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost("{id:int}/adicionar-tempo")]
    public ActionResult<AquecimentoDTO?> AdicionarTempo(int id)
    {
        var res = _service.AdicionarTempo(id);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost("{id:int}/iniciar")]
    public ActionResult<AquecimentoDTO?> Iniciar(int id)
    {
        var res = _service.IniciarAquecimento(id);
        return res == null ? NotFound() : Ok(res);
    }   

    [HttpPost("{id:int}/pausar")]
    public ActionResult<AquecimentoDTO?> Pausar(int id)
    {
        var res = _service.PausarAquecimento(id);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost("{id:int}/retomar")]
    public ActionResult<AquecimentoDTO?> Retomar(int id)
    {
        var res = _service.RetomarAquecimento(id);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost("{id:int}/cancelar")]
    public ActionResult<AquecimentoDTO?> Cancelar(int id)
    {
        var res = _service.CancelarAquecimento(id);
        return res == null ? NotFound() : Ok(res);
    }
}