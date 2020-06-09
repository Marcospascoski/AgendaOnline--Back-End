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


        [HttpGet("ListaAgendamentosPorUsuario/{UserId}")]
        [AllowAnonymous]
        public async Task<ActionResult> ListaAgendamentosPorUsuario(int UserId)
        {
            try
            {
                var agendaAtual = await _repo.ObterTodosAgendamentosPorUsuarioAsync(UserId);
                if (agendaAtual.Length <= 0)
                {
                    return Ok("nao agendamento");
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
                //Excluir Horários Excluidos da query de horários disponíveis
                var dataFormatada = data.ToString("dd/MM/yyyy");
                var dataTipada = DateTime.Parse(dataFormatada);
                var temEmpresa = await _repo.TemEmpresa(empresa);
                TimeSpan semDuracao = new TimeSpan(0, 0, 0);
                TimeSpan duracaoEmpresaNaoEstipulada = new TimeSpan(1, 0, 0);
                if (temEmpresa)
                {
                    if(data >= DateTime.Now.Date)
                    {
                        var horariosDisponiveis = await _repo.ObterHorariosDisponiveis(empresa, dataTipada.Date);
                        var semDisponibilidade = horariosDisponiveis.FirstOrDefault();
                        if(semDisponibilidade != duracaoEmpresaNaoEstipulada)
                        {
                            if (horariosDisponiveis.Count > 0)
                            {
                                return Ok(horariosDisponiveis);
                            }
                            else
                            {
                                return Ok("indisponível");
                            }
                        }
                        else
                        {
                            return Ok("duracaoNaoEstipulada");
                        }
                        

                    }
                    else
                    {
                        return Ok("diaVencido");
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
            try
            {
                var dias = await _repo.ObterDiasAgendadosAsync(AdmId);
                var diasDto = _mapper.Map<AgendaDto[]>(dias);
                var results = diasDto.ToArray().Select(x => x.DataHora.Day + "/" + x.DataHora.Month + "/" + x.DataHora.Year).Distinct().ToList();

                if (results.Count > 0)
                {
                    return Ok(results);
                }
                else
                {
                    return Ok("vazio");
                }

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
            TimeSpan semDuracao = new TimeSpan(0, 0, 0);

            var clientesAgendados = await _repo.ObterClientesAgendadosMesmaDataAsync(agendamentoModel);
            var horariosAtendimento = await _repo.ObterHorariosAtendimento(agendamentoModel);
            var horarioInicioFim = await _repo.ObterInicioFim(agendamentoModel);
            var agendamentoIndisponivel = await _repo.VerificarIndisponibilidade(agendamentoModel);

            TimeSpan horarioAgendado = TimeSpan.Parse(agendaDto.DataHora.ToString("HH:mm:ss"));
            try
            {
                if (agendamentoIndisponivel.ToString() == "")
                {
                    if (agendamentoModel.DataHora > DateTime.Now)
                    {
                        if (clientesAgendados.Length <= 0)
                        {
                            if (horariosAtendimento.Count > 1 && horariosAtendimento[0] != semDuracao)
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
                            // deixar horarioInicioFim.Count == 1 
                            else if (horarioInicioFim.Count == 1 && horarioInicioFim[0] == semDuracao)
                            {
                                
                                return Ok("horarioImproprio");
                                
                            }
                            else
                            {
                                _repo.Add(agendamentoModel);
                                if (await _repo.SaveChangesAsync())
                                {
                                    return Created($"/api/agenda/{agendaDto.Id}", _mapper.Map<AgendaDto>(agendamentoModel));
                                }
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

        [HttpDelete("MotorRemocao/{UserId}")]
        [AllowAnonymous]
        public async Task<IActionResult> MotorRemocao(int UserId)
        {
            try
            {
                var horaAtual = DateTime.Now.ToString("HH:mm:ss");
                var idDataServicoFinalizado = _repo.ObterServicosFinalizadosAsync(UserId);
                var idDataServicosVencidos = _repo.ObterServicosVencidosAsync(UserId);

                if (idDataServicoFinalizado.Length > 0)
                {
                    //Chamar Delete
                    _repo.DeleteRange(idDataServicoFinalizado);
                    if (await _repo.SaveChangesAsync())
                    {
                        return Ok();
                    }
                }
                else if (idDataServicosVencidos.Length > 0)
                {
                    //Chamar Delete
                    _repo.DeleteRange(idDataServicosVencidos);
                    if (await _repo.SaveChangesAsync())
                    {
                        return Ok();
                    }
                }
                else
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