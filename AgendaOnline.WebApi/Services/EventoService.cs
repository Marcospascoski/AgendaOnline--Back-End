﻿using AgendaOnline.Domain;
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
            TimeSpan diaTodo = new TimeSpan(0, 0, 0);
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
            var eventoModel = _mapper.Map<Evento>(eventoDto);

            var eventoDesatualizado = await _repo.DataHorasUltrapassadas(eventoModel);
            if (eventoDesatualizado.Length > 0)
                await ExcluirEventos(eventoDesatualizado);

            if ((eventoModel.DataHora > DateTime.Now) || (eventoModel.DataHora.TimeOfDay == diaTodo && eventoModel.DataHora.Date == DateTime.Today))
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
            //Arrumar Retorno deste Método
            TimeSpan diaTodo = new TimeSpan(0, 0, 0);
            var eventosPorPrestador = await _repo.ObterEventosPorAdmIdAsync(admId);
            var eventosUltrapassados = eventosPorPrestador.Where(x => x.DataHora.Date <= DateTime.Now.Date).ToArray();
            var eventosUltrapassadosMesmoDia = eventosUltrapassados.Where(x => !x.DataHora.ToString().Contains("00:00:00") && x.DataHora.TimeOfDay < DateTime.Now.TimeOfDay).ToArray();
            var eventosUltrapassadosOutroDia = eventosUltrapassados.Where(x => x.DataHora.Date < DateTime.Now.Date).ToArray();
            if (eventosUltrapassadosMesmoDia.Length > 0)
            {
                if (eventosUltrapassadosMesmoDia.Length > 0)
                    await ExcluirEventos(eventosUltrapassadosMesmoDia);
            }
            if(eventosUltrapassadosOutroDia.Length > 0)
            {
                if (eventosUltrapassadosOutroDia.Length > 0)
                    await ExcluirEventos(eventosUltrapassadosOutroDia);
            }

            var eventosMesmaData = eventosPorPrestador.Select(x => x.DataHora.Date);
            var dataMaisDeUma = eventosMesmaData.GroupBy(x => x)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

            //Lista de Datas com Eventos dia Todo
            List<DateTime> eventosMesmoDia = new List<DateTime>();
            List<DateTime> eventosDistinct = new List<DateTime>();
            var datasAvançadas = eventosPorPrestador.Where(x => x.DataHora.Date >= DateTime.Now.Date).Select(x => x.DataHora).ToArray();
            List<string> result = new List<string>();
            if (dataMaisDeUma.Count > 0)
            {
                foreach (var data in dataMaisDeUma)
                {
                    var datasMesmoDia = eventosPorPrestador.Where(x => x.DataHora.Date == data && x.DataHora.ToString().Contains("00:00:00")).
                        Select(x => x.DataHora).ToList();
                    eventosMesmoDia.Add(datasMesmoDia.FirstOrDefault());
                }

                foreach (var item in eventosMesmoDia)
                {
                    List<DateTime> eventoQuery = eventosPorPrestador.Where(x => x.DataHora.Date != item.Date).Select(x => x.DataHora).ToList();
                    eventosDistinct.Add(eventoQuery.FirstOrDefault());
                }

                List<DateTime> eventosDatasFormatadas = new List<DateTime>();

                foreach (var item in eventosDistinct)
                {
                    eventosDatasFormatadas.Add(item);
                }
                foreach (var item in eventosMesmoDia)
                {
                    eventosDatasFormatadas.Add(item);
                }
                var datas = eventosDatasFormatadas.Where(x => x >= DateTime.Now.Date)
                        .Select(x => x.TimeOfDay == diaTodo ? x.Day + "/" + x.Month + "/" + x.Year : x.ToString());


                foreach (var item in datas)
                {
                    result.Add(item.ToString());
                }
            }
            else
            {
                var eventosHoje = datasAvançadas.Where(x => x == DateTime.Now.Date && !x.ToString().Contains("00:00:00")).ToList();
                var datas = datasAvançadas.Where(x => x >= DateTime.Now.Date).ToList();
                var datasOutroDia = datas.Where(x => x >= DateTime.Now.Date)
                    .Select(x => x.TimeOfDay == diaTodo ? x.Day + "/" + x.Month + "/" + x.Year : x.ToString()).ToList();
                var datasUltrapassadosMesmoDia = datas.Where(x => x.ToString().Contains("00:00:00") && x.TimeOfDay > DateTime.Now.TimeOfDay)
                    .Select(x => x.TimeOfDay == diaTodo ? x.Day + "/" + x.Month + "/" + x.Year : x.ToString()).ToArray();

                if (eventosHoje.Count > 0)
                {
                    foreach (var item in datasUltrapassadosMesmoDia)
                    {
                        result.Add(item.ToString());
                    }
                }
                else
                {
                    foreach (var item in datasOutroDia)
                    {
                        result.Add(item.ToString());
                    }
                }
                
            }
            

            if (result.Count > 0)
            {
                try
                {
                    return result;
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
