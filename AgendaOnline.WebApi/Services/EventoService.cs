using AgendaOnline.Domain;
using AgendaOnline.Repository;
using AgendaOnline.WebApi.Dtos;
using AgendaOnline.WebApi.Services.Exceptions;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaOnline.WebApi.Services
{
    public class EventoService
    {
        private readonly IEventoRepository _repo;
        private readonly IMapper _mapper;

        public EventoService(IEventoRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        public async Task ExcluirEventos(Evento[] eventos)
        {
            if (eventos.Length == 1)
            {
                try
                {
                    _repo.Delete(eventos[0]);
                    await _repo.SaveChangesAsync();
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                try
                {
                    _repo.DeleteRange(eventos);
                    await _repo.SaveChangesAsync();
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
        }

        public async Task<Evento[]> DisponibilizarEvento(EventoDto eventoDto)
        {
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);

            var eventoModel = _mapper.Map<Evento>(eventoDto);
            var eventoBase = await _repo.EventoExistente(eventoModel);
            if (eventoBase.Length == 1)
            {
                try
                {
                    return eventoBase;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                throw new BusinessException("eventoInexistente");
            }
        }

        public async Task<Evento> DeclararMotivo(EventoDto eventoDto)
        {
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
            var eventoModel = _mapper.Map<Evento>(eventoDto);

            var eventoDesatualizado = await _repo.DataHorasUltrapassadas(eventoModel);
            if(eventoDesatualizado.Length > 0)
               await ExcluirEventos(eventoDesatualizado);

            if (eventoModel.DataHora > DateTime.Now)
            {
                var eventoRepetido = await _repo.EventoRepetido(eventoModel);
                if (eventoRepetido == false)
                {
                    try
                    {
                        _repo.Add(eventoModel);
                        await _repo.SaveChangesAsync();
                        return eventoModel;                        
                    }
                    catch (DbConcurrencyException e)
                    {
                        throw new DbConcurrencyException(e.Message);
                    }
                }
                else
                {
                    throw new BusinessException("indisponível");
                }
            }
            else
            {
                throw new BusinessException("DataHora Ultrapassada");
            }
        }

        public async Task<List<string>> ListaDeDatasExcluidas(int admId)
        {
            
                TimeSpan diaTodo = new TimeSpan(0, 0, 0);
                var eventosPorPrestador = await _repo.ObterEventosPorAdmIdAsync(admId);
                var eventosDatasFormatadas = eventosPorPrestador.Select(x => x.DataHora.TimeOfDay == diaTodo ? x.DataHora.Day+"/"+x.DataHora.Month+"/"+x.DataHora.Year : x.DataHora.ToString()).ToList();

                if (eventosDatasFormatadas.Count > 0)
                {
                    try
                    {
                        return eventosDatasFormatadas;
                    }
                    catch (DbConcurrencyException e)
                    {
                        throw new DbConcurrencyException(e.Message);
                    }
                }
                else
                {
                    throw new BusinessException("naoEncontrado");
                }

        }

    }
}
