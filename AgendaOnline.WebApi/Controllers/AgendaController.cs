using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AgendaOnline.Domain;
using AgendaOnline.Repository;
using AutoMapper;
using AgendaOnline.WebApi.Dtos;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using AgendaOnline.WebApi.Services;
using AgendaOnline.WebApi.Services.Exceptions;

namespace AgendaOnline.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgendaController : ControllerBase
    {
        private readonly IAgendaRepository _repo;
        private readonly IMapper _mapper;
        private readonly AgendaService _service;

        public AgendaController(IAgendaRepository repo, IMapper mapper, AgendaService service)
        {
            _service = service;
            _mapper = mapper;
            _repo = repo;
        }


        [HttpGet("ListaAgendamentosPorUsuario/{UserId}")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaAgendamentosPorUsuario(int UserId)
        {
            try
            {
                var agendaAtual = await _repo.ObterTodosAgendamentosPorUsuarioAsync(UserId);
                if (agendaAtual.Length <= 0)
                {
                    return Ok(agendaAtual);
                }
                //var results = _mapper.Map<AgendaDto>(agendamentoAtual);

                return Ok(agendaAtual);
            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("HorariosDisponiveis")]
        [AllowAnonymous]
        public async Task<ActionResult> HorariosDisponiveis(string empresa, DateTime data)
        {
            try
            {
                var serviceHorariosDisponiveis = await _service.ListarHorariosDisponiveis(empresa, data);
                return Ok(serviceHorariosDisponiveis);
            }
            catch(BusinessException e)
            {   
                switch (e.Message)
                {
                    case "diaVencido": return Ok("diaVencido");
                    case "indisponível" : return Ok("indisponível");
                    case "duracaoNaoEstipulada" : return Ok("duracaoNaoEstipulada");
                    case "empresainvalida" : return Ok("empresainvalida");
                    default  : return BadRequest();  
                }
            }
            catch(DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }

        }

        [HttpGet("BuscarEmpresas")]
        [AllowAnonymous]
        public async Task<ActionResult> BuscarEmpresas(string text)
        {
            try
            {
                var resultadosFiltro = await _service.FiltrarEmpresas(text);
                return Ok(resultadosFiltro);
            }
            catch (BusinessException e)
            {
                if(e.Message.Equals("Não encontrado"))
                    return Ok("diaVencido");

                return BadRequest();
               
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpGet("ListaAdminsPorAgenda")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaAdminsPorAgenda()
        {
            try
            {
                var usuarios = await _repo.ObterTodosAdminsAsync();
                var results = _mapper.Map<AdmDto[]>(usuarios);

                return Ok(results);
            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("ListaDiasAgendados/{AdmId}")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaDiasAgendados(int AdmId)
        {
            List<string> diasAgendadosService = new List<string>();
            try
            {
                diasAgendadosService = await _service.ListaDiasAgendados(AdmId);
                return Ok(diasAgendadosService);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("vazio"))
                    return Ok(diasAgendadosService);

                return BadRequest();

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpPost("AgendarCliente")]
        [AllowAnonymous]
        public async Task<ActionResult> AgendarCliente(AgendaDto agendaDto)
        {
            try
            {
                var salvamentoService = await _service.SalvarAlteracoes(agendaDto, "post");
                return Created($"/api/agenda/{agendaDto.Id}", _mapper.Map<AgendaDto>(salvamentoService));
            }
            catch (BusinessException e)
            {
                switch (e.Message)
                {
                    case "empresainvalida": return Ok("empresainvalida");
                    case "momento": return Ok("momento");
                    case "dataCerta": return Ok("dataCerta");
                    case "horarioImproprio": return Ok("horarioImproprio");
                    case "valido": return Ok("valido");
                    case "-": return NotFound();
                    default: return Ok(e.Message);
                }
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }

        }

        [HttpDelete("MotorRemocao/{UserId}")]
        [AllowAnonymous]
        public async Task<ActionResult> MotorRemocao(int UserId)
        {
            try
            {
                await _service.MotorRemocao(UserId);
                return Ok();
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }

        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload()
        {
            try
            {
                var file = Request.Form.Files[0];
                var folderName = Path.Combine("Resources", "Images");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (file.Length > 0)
                {
                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName;
                    var fullPath = Path.Combine(pathToSave, fileName.Replace("\"", " ").Trim());

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                }

                return Ok();
            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e);
            }

            return BadRequest("Erro ao tentar realizar Upload");
        }

        [HttpPut("AtualizarAgenda")]
        [AllowAnonymous]
        public async Task<IActionResult> Put(AgendaDto agendaDto)
        {
            try
            {
                var salvamentoService = await _service.SalvarAlteracoes(agendaDto, "put");
                return Created($"/api/agenda/{agendaDto.Id}", _mapper.Map<AgendaDto>(salvamentoService));
            }
            catch (BusinessException e)
            {
                switch (e.Message)
                {
                    case "empresainvalida": return Ok("empresainvalida");
                    case "momento": return Ok("momento");
                    case "dataCerta": return Ok("dataCerta");
                    case "horarioImproprio": return Ok("horarioImproprio");
                    case "valido": return Ok("valido");
                    case "-": return NotFound();
                    default: return Ok(e.Message);
                }
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }

        }

        [HttpDelete("{AgendaId}")]
        public async Task<IActionResult> Delete(int AgendaId)
        {
            try
            {
                var result = await _service.DeletarAgendamentos(AgendaId);
                return Ok();
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("naoEncontrado"))
                    return NotFound();

                return BadRequest();
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }
    }
}