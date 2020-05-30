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
        public async Task ExluirEventosPassados(Evento[] eventos)
        {
            //Chamar Delete
            _repo.DeleteRange(eventos);
            await _repo.SaveChangesAsync();
        }

        //[HttpGet("ListaEventosPorAdm/{AdmId}")]
        //[AllowAnonymous]
        //public async Task<ActionResult> ListaEventosPorAdm(int AdmId)
        //{
        //    try
        //    {
        //        var eventos = await _repo.ObterEventosPorAdmIdAsync(AdmId);
        //        var diasDto = _mapper.Map<EventoDto[]>(eventos);
        //        var datasModel = diasDto.ToArray().Select(x => x.DataHora.Date).Distinct().ToList();

        //        List<DateTime> datasCorretas = new List<DateTime>();
        //        foreach (var data in datasModel)
        //        {
        //            datasCorretas.Add(DateTime.Parse(data.ToString("dd/MM/yyyy")));
        //        }

        //        return Ok(datasCorretas);
        //    }
        //    catch (System.Exception)
        //    {
        //        return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
        //    }
        //}

        [HttpPost("DeclararMotivo")]
        [AllowAnonymous]
        public async Task<IActionResult> DeclararMotivo(EventoDto eventoDto)
        {
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
            var eventoModel = _mapper.Map<Evento>(eventoDto);

            var eventoDesatualizado = await _repo.DataHorasUltrapassadas(eventoModel);
            if(eventoDesatualizado.Length > 0)
               await ExluirEventosPassados(eventoDesatualizado);

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