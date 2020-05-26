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

namespace AgendaOnline.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgendaController : ControllerBase
    {
        private readonly IAgendaRepository _repo;
        private readonly IMapper _mapper;

        public AgendaController(IAgendaRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }
        
        [HttpGet("Get")]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            try
            {
                var agendamentoDesatualizado = await _repo.teste();
                agendamentoDesatualizado.OrderByDescending(c => c.DataHora);

                if (agendamentoDesatualizado.Length > 0)
                {
                    await MotorRemocao(agendamentoDesatualizado);

                    var agendamentoAtual = await _repo.teste();
                    var results = _mapper.Map<AgendaDto[]>(agendamentoAtual);
                    if (results != null)
                    {
                        return Ok(results);
                    }
                    else
                    {
                        return Ok(new AgendaDto());
                    }
                }
                else
                {
                    return Ok(new AgendaDto());
                }

            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e);
            }
        }

        [HttpGet("ListaAgendamentosPorUsuario/{UserId}")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaAgendamentosPorUsuario(int UserId)
        {
            try
            {
                var agendamentoDesatualizado = await _repo.ObterTodosAgendamentosPorUsuarioAsync(UserId);
                await MotorRemocao(agendamentoDesatualizado);
                
                var agendamentoAtual = await _repo.ObterTodosAgendamentosPorUsuarioAsync(UserId);
                if(agendamentoAtual.Length <= 0)
                {
                    return Ok("Não há agendamentos para este Usuário");
                }
                //var results = _mapper.Map<AgendaDto>(agendamentoAtual);

                return Ok(agendamentoAtual);
            }
            catch (System.Exception)
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
                //Excluir Horários Excluidos da query de horários disponíveis
                var dataFormatada = data.ToString("dd/MM/yyyy");
                var dataTipada = DateTime.Parse(dataFormatada);
                var temEmpresa = await _repo.TemEmpresa(empresa);
                if (temEmpresa)
                {
                    var horariosDisponiveis = await _repo.ObterHorariosDisponiveis(empresa, dataTipada.Date);

                    if (horariosDisponiveis.Count > 0)
                    {
                        return Ok(horariosDisponiveis);
                    }
                    else
                    {
                        return Ok("indisponível");
                    }
                }
                return Ok("empresainvalida");

            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("BuscarEmpresas")]
        [AllowAnonymous]
        public async Task<ActionResult> BuscarEmpresas(string text)
        {
            try
            {
                var resultadosFiltro = await _repo.FiltrarEmpresas(text);
                if (resultadosFiltro.Count > 0)
                {
                    return Ok(resultadosFiltro);
                }
                return Ok("Não encontrado");
            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("ListaAdminsPorAgenda")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaAdminsPorAgenda()
        {
            try
            {
                var usuarios = await _repo.ObterTodosAdminsAsync();
                //var dados = usuarios.Select(x => x.Company  x.MarketSegment);
                var results = _mapper.Map<AdmDto[]>(usuarios);

                return Ok(results);
            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpGet("ListaDiasAgendados")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaDiasAgendados()
        {
            try
            {
                var dias = await _repo.ObterDiasAgendadosAsync();
                var diasDto = _mapper.Map<AgendaDto[]>(dias);
                var results = diasDto.ToArray().Select(x => x.DataHora.Day + "/" + x.DataHora.Month + "/" + x.DataHora.Year).Distinct();

                return Ok(results);
            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
        }

        [HttpPost("AgendarCliente")]
        [AllowAnonymous]
        public async Task<IActionResult> AgendarCliente(AgendaDto agendaDto)
        {
            //Validações
            var agendamentoModel = _mapper.Map<Agenda>(agendaDto);

            var clientesAgendados = await _repo.ObterClientesAgendadosMesmaDataAsync(agendamentoModel);
            var horariosAtendimento = await _repo.ObterHorariosAtendimento(agendamentoModel);
            var agendamentoIndisponivel = await _repo.VerificarIndisponibilidade(agendamentoModel);
            
            TimeSpan horarioAgendado = TimeSpan.Parse(agendaDto.DataHora.ToString("HH:mm:ss"));
            TimeSpan horarioFimUsuario = TimeSpan.Parse(agendaDto.DataHora.ToString("HH:mm:ss"));
            TimeSpan horarioIniUsuario = TimeSpan.Parse(agendaDto.DataHora.ToString("HH:mm:ss"));
            try
            {
                if(agendamentoIndisponivel.ToString() == "")
                {
                    if (agendamentoModel.DataHora > DateTime.Now)
                    {
                        if (clientesAgendados.Length <= 0)
                        {
                            if (horariosAtendimento.Contains(horarioAgendado))
                            {
                                _repo.Add(agendamentoModel);
                                if (await _repo.SaveChangesAsync())
                                {
                                    return Created($"/api/agenda/{agendaDto.Id}", _mapper.Map<AgendaDto>(agendamentoModel));
                                }
                            }
                            else
                            {
                                return Ok("valido");
                            }
                        }
                        else
                        {
                            return Ok("dataCerta");
                        }
                    }
                    else
                    {
                        return Ok("momento");
                    }
                }
                else
                {
                    return Ok(agendamentoIndisponivel.ToString());
                }

            }
            catch (System.Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Banco de dados Falhou {ex.Message}");
            }
            return BadRequest();

        }

        [AllowAnonymous]
        public async Task MotorRemocao(Agenda[] agendamentos)
        {
            var horaAtual = DateTime.Now.ToString("HH:mm:ss");
            var idDataServicoFinalizado = _repo.ObterServicosFinalizadosAsync(agendamentos);
            var idDataServicosVencidos = _repo.ObterServicosVencidosAsync(agendamentos);

            if (idDataServicoFinalizado.Length > 0)
            {
                //Chamar Delete
                _repo.DeleteRange(idDataServicoFinalizado);
            }
            else if (idDataServicosVencidos.Length > 0)
            {
                //Chamar Delete
                _repo.DeleteRange(idDataServicosVencidos);
            }
            await _repo.SaveChangesAsync();
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

        [HttpPut("{AgendaId}")]
        public async Task<IActionResult> Put(int AgendaId, AgendaDto model)
        {
            try
            {
                var agendamento = await _repo.ObterAgendamentoPorIdAsync(AgendaId);
                if (agendamento == null) return NotFound();

                _mapper.Map(model, agendamento);

                _repo.Update(agendamento);

                if (await _repo.SaveChangesAsync())
                {
                    return Created($"/api/agenda/{model.Id}", _mapper.Map<AgendaDto>(agendamento));
                }
            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
            return BadRequest();
        }
        
        [HttpDelete("{AgendaId}")]
        public async Task<IActionResult> Delete(int AgendaId)
        {
            try
            {
                var agendamento = await _repo.ObterAgendamentoPorIdAsync(AgendaId);
                if (agendamento == null) return NotFound();

                _repo.Delete(agendamento);

                if (await _repo.SaveChangesAsync())
                {
                    return Ok();
                }
            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
            return BadRequest();
        }
    }
}