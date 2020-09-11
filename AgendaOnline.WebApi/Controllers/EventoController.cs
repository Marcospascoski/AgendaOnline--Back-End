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
    public class EventoController : ControllerBase
    {
        private readonly IEventoRepository _repo;
        private readonly IMapper _mapper;
        private readonly EventoService _service;
        private readonly AgendaService _serviceAgenda;

        public EventoController(IEventoRepository repo, IMapper mapper, EventoService service, AgendaService serviceAgenda)
        {
            _serviceAgenda = serviceAgenda;
            _service = service;
            _mapper = mapper;
            _repo = repo;
        }
        

        [HttpGet("ListaDeDatasExcluidas/{admId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ListaDeDatasExcluidas(int admId)
        {
            try
            {
                var diasExcluidosService = await _service.ListaDeDatasExcluidas(admId);
                return Ok(diasExcluidosService);
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("naoEncontrado"))
                    return Ok("naoEncontrado");

                return BadRequest();
            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }


        [HttpPost("DisponibilizarEvento")]
        [AllowAnonymous]
        public async Task<IActionResult> DisponibilizarEvento(EventoDto eventoDto)
        {
            DateTime data = eventoDto.DataHora;
            try
            {
                var disponibilizarEventoService = await _service.DisponibilizarEvento(eventoDto);
                if (disponibilizarEventoService != null)
                {
                    eventoDto.DataHora = data;
                    var enviarMotivoEmAgendamento = await _serviceAgenda.EnviarMotivo(eventoDto, "disponibilizar");
                }
                await _service.ExcluirEventos(disponibilizarEventoService);
                return Ok();
            }
            catch (BusinessException e)
            {
                if (e.Message.Equals("eventoInexistente"))
                    return Ok("eventoInexistente");

                return BadRequest();

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
        }

        [HttpPost("DeclararMotivo")]
        [AllowAnonymous]
        public async Task<IActionResult> DeclararMotivo(EventoDto eventoDto)
        {
            DateTime data = eventoDto.DataHora;
            try
            {
                var declararacaoMotivoService = await _service.DeclararMotivo(eventoDto);
                if(declararacaoMotivoService != null)
                {
                    eventoDto.DataHora = data;
                    var enviarMotivoEmAgendamento = await _serviceAgenda.EnviarMotivo(eventoDto, "indisponibilizar");
                }
                    
                return Created($"/api/evento/{eventoDto.Id}", _mapper.Map<EventoDto>(declararacaoMotivoService));
            }
            catch (BusinessException e)
            {
                switch (e.Message)
                {
                    case "naoEncontrado": return Ok("naoEncontrado");
                    case "indisponível" : return Ok("indisponível");
                    case "DataHora Ultrapassada" : return Ok("DataHora Ultrapassada");
                    default : return BadRequest(); 
                }

            }
            catch (DbConcurrencyException e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou, pelo motivo: {0}" + e);
            }
            
        }
        
    }
}