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
        
        [HttpPost("DeclararMotivo")]
        [AllowAnonymous]
        public async Task<IActionResult> DeclararMotivo(EventoDto eventoDto)
        {
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
            var eventoModel = _mapper.Map<Evento>(eventoDto);
            try
            {
                 _repo.Add(eventoModel);
                 if (await _repo.SaveChangesAsync())
                 {
                    return Created($"/api/evento/{eventoDto.Id}", _mapper.Map<EventoDto>(eventoModel));
                 }
                 else{
                    return BadRequest(); 
                 }
            }
            catch (System.Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e);
            }
        }
        
        [HttpDelete("{AdmId}")]
        [AllowAnonymous]
        public async Task<IActionResult> IndisponibilizarData(int AdmId)
        {
            try
            {
                // var evento = await _repo.ObterAgendamentoPorIdAsync(AdmId);
                // if (agendamento == null) return NotFound();
                
                // _repo.Delete(agendamento);

                // if (await _repo.SaveChangesAsync())
                // {
                //     return Ok();
                // }
            }
            catch (System.Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Banco de dados Falhou");
            }
            return BadRequest();
        }
    }
}