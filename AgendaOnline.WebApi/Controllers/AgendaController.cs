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
                List<Agenda> agendaAtual = await _repo.ObterTodosAgendamentosPorUsuarioAsync(UserId);
                if (agendaAtual.Count <= 0)
                {
                    return Ok(agendaAtual.OrderBy(x => x.DataHora));
                }

                return Ok(agendaAtual.OrderBy(x => x.DataHora));
            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("HorariosDisponiveis")]
        [AllowAnonymous]
        public async Task<ActionResult> HorariosDisponiveis(string empresa, string data)
        {
            DateTime dateValue = new DateTime();
            DateTime.TryParse(data.ToString(), out dateValue);
            try
            {
                var serviceHorariosDisponiveis = await _service.ListarHorariosDisponiveis(empresa, dateValue);
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
        public async Task<ActionResult> BuscarEmpresas(string text, string segmento, string cidade)
        {
            try
            {
                var resultadosFiltro = await _service.FiltrarEmpresas(text, segmento, cidade);
                return Ok(resultadosFiltro);
            }
            catch (BusinessException e)
            {
                if(e.Message.Equals("Não encontrado"))
                    return Ok("NotFound");

                return BadRequest();
               
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpGet("BuscarCidades")]
        [AllowAnonymous]
        public async Task<ActionResult> BuscarCidades(string text, string segmento)
        {
            try
            {
                var resultadosFiltro = await _service.FiltrarCidades(text, segmento);
                return Ok(resultadosFiltro);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("Não encontrado"))
                    return Ok("NotFound");

                return BadRequest();

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpGet("BuscarSegmentos")]
        [AllowAnonymous]
        public async Task<ActionResult> BuscarSegmentos(string text, string cidade)
        {
            try
            {
                var resultadosFiltro = await _service.FiltrarSegmentos(text, cidade);
                return Ok(resultadosFiltro);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("Não encontrado"))
                    return Ok("NotFound");

                return BadRequest();

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpGet("BuscarClientes")]
        [AllowAnonymous]
        public async Task<ActionResult> BuscarClientes(string text)
        {
            try
            {
                var resultadosFiltro = await _service.FiltrarClientes(text);
                return Ok(resultadosFiltro);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("Não encontrado"))
                    return Ok("NotFound");

                return BadRequest();

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpGet("ListaDeAdms")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaDeAdms()
        {
            try
            {
                var admins = await _service.ListaDeAdmins();
                var results = _mapper.Map<AdmDto[]>(admins);
                return Ok(results);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("adm not found"))
                    return Ok("adm not found");

                return BadRequest();

            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("ObterUsuario")]
        [AllowAnonymous]
        public async Task<ActionResult> ObterUsuario(string UserName)
        {
            try
            {
                var user = await _service.ObterUsuario(UserName);
                if(user.Role == "Adm")
                {
                    var results = _mapper.Map<AdmDto>(user);
                    return Ok(results);
                }
                else
                {
                    var results = _mapper.Map<UserDto>(user);
                    return Ok(results);
                }
                
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("user not found"))
                    return Ok("user not found");

                return BadRequest();

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("ListaDeClientes")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaDeClientes()
        {
            try
            {
                var clientes = await _service.ListaDeClientes();
                var results = _mapper.Map<UserDto[]>(clientes);
                return Ok(results);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("client not found"))
                    return Ok("client not found");

                return BadRequest();

            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("ListaDeUsuarios")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaDeUsuarios()
        {
            try
            {
                var usuarios = await _repo.ObterTodosUsuariosAsync();
                var results = _mapper.Map<AdmDto[]>(usuarios);
                return Ok(results);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("user not found"))
                    return Ok("user not found");

                return BadRequest();

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

        [HttpGet("ObterImagemDePerfil/{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult> ObterImagemDePerfil(int userId)
        {
            try
            {
                var imagemPerfil = await _service.ObterImagemDePerfil(userId);
                if(imagemPerfil.Length > 0)
                    return Ok(imagemPerfil);

            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("user not found"))
                {
                    return Ok("user not found");
                }
                else if (e.Message.Equals("user without image"))
                {
                    return Ok("user without image");
                }

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
            return BadRequest();
        }

        [HttpPost("upload")]
        [AllowAnonymous]
        public async Task<IActionResult> Upload()
        {
            try
            {
                var file = Request.Form.Files[0];
                List<int> idUser = new List<int>();
                Request.Form.Keys
                    .Where(n => n.StartsWith("idUser"))
                    .ToList()
                    .ForEach(x => idUser.Add(int.Parse(Request.Form[x])));

                if (file.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        string s = Convert.ToBase64String(fileBytes);
                        // obter Usuário
                        try
                        {
                            _service.SalvarImagemPerfil(idUser.FirstOrDefault(), s);
                            return Ok();
                        }
                        catch (BusinessException e)
                        {
                            if (e.Message.Equals("user not found"))
                            {
                                return Ok("user not found");
                            }
                            else if (e.Message.Equals("update failed"))
                            {
                                return BadRequest();
                            }

                        }
                        catch (DbConcurrencyException e)
                        {
                            return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
                        }
                    }
                }
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
        [AllowAnonymous]
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