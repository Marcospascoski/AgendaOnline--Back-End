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
    public class EventoController : ControllerBase
    {
        private readonly IEventoRepository _repo;
        private readonly IMapper _mapper;

        public EventoController(IEventoRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        [AllowAnonymous]
        public async Task ExluirEventos(Evento[] eventos)
        {
            //Chamar Delete
            if(eventos.Length == 1)
            {
                _repo.Delete(eventos[0]);
                await _repo.SaveChangesAsync();
            }
            else
            {
                _repo.DeleteRange(eventos);
                await _repo.SaveChangesAsync();
            }
            
        }

        [HttpGet("ListaDeDatasExcluidas/{admId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ListaDeDatasExcluidas(int admId)
        {
            try
            {
                TimeSpan diaTodo = new TimeSpan(0, 0, 0);
                var eventosPorPrestador = await _repo.ObterEventosPorAdmIdAsync(admId);
                var eventosDatasFormatadas = eventosPorPrestador.Select(x => x.DataHora.TimeOfDay == diaTodo ? x.DataHora.Day+"/"+x.DataHora.Month+"/"+x.DataHora.Year : x.DataHora.ToString()).ToList();

                if (eventosDatasFormatadas.Count > 0)
                {
                    return Ok(eventosDatasFormatadas);
                }
                else
                {
                    return Ok("naoEncontrado");
                }

            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e);
            }
        }


        [HttpPost("DisponibilizarEvento")]
        [AllowAnonymous]
        public async Task<IActionResult> DisponibilizarEvento(EventoDto eventoDto)
        {

            try
            {
                eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
                
                var eventoModel = _mapper.Map<Evento>(eventoDto);
                var eventoBase = await _repo.EventoExistente(eventoModel);
                if(eventoBase.Length == 1)
                {
                    await ExluirEventos(eventoBase);
                    return Ok();
                }
                else
                {
                    return Ok("eventoInexistente");
                }

            }
            catch(Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e);
            }
        }

        [HttpPost("DeclararMotivo")]
        [AllowAnonymous]
        public async Task<IActionResult> DeclararMotivo(EventoDto eventoDto)
        {
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
            var eventoModel = _mapper.Map<Evento>(eventoDto);

            var eventoDesatualizado = await _repo.DataHorasUltrapassadas(eventoModel);
            if(eventoDesatualizado.Length > 0)
               await ExluirEventos(eventoDesatualizado);

            if (eventoModel.DataHora > DateTime.Now)
            {
                var eventoRepetido = await _repo.EventoRepetido(eventoModel);
                if (eventoRepetido == false)
                {
                    try
                    {
                        _repo.Add(eventoModel);
                        if (await _repo.SaveChangesAsync())
                        {
                            return Created($"/api/evento/{eventoDto.Id}", _mapper.Map<EventoDto>(eventoModel));
                        }
                        else
                        {
                            return BadRequest();
                        }

                    }
                    catch (System.Exception e)
                    {
                        return this.StatusCode(StatusCodes.Status500InternalServerError, e);
                    }
                }
                else
                {
                    return Ok("indisponível");
                }
            }
            else
            {
                return Ok("DataHora Ultrapassada");
            }
            
        }
        
    }
}