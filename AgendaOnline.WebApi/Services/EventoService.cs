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
        private readonly AgendaService _agendaService;
        private readonly AgendaContext _agendaContext;
        private readonly IMapper _mapper;

        public EventoService(IEventoRepository repo, IMapper mapper,
            AgendaService agendaService,
            AgendaContext agendaContext)
        {
            _agendaService = agendaService;
            _agendaContext = agendaContext;
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
            TimeSpan diaTodo = new TimeSpan(0, 0, 0);
            eventoDto.DataHora = eventoDto.DataHora.TimeOfDay != diaTodo ? eventoDto.DataHora.AddHours(-3) : eventoDto.DataHora.Date;

            var eventoModel = _mapper.Map<Evento>(eventoDto);
            var eventoBase = await _repo.EventoExistente(eventoModel);

            List<Agenda> agendas;
            if (eventoBase.Length > 0)
            {
                try
                {
                    agendas = _agendaContext.Agendas.Where(x => x.DataHora.Date == eventoBase[0].DataHora.Date
                    && eventoBase[0].DataHora.TimeOfDay == diaTodo
                    && x.AdmId == eventoBase[0].AdmId).ToList();
                    foreach (var agenda in agendas)
                    {
                        agenda.Observacao = "Agendamento Disponível Novamente";
                        await _agendaService.AtualizarObservacaoAgenda(agenda);
                    }

                    agendas = _agendaContext.Agendas.Where(x => x.DataHora == eventoBase[0].DataHora
                    && eventoBase[0].DataHora.TimeOfDay != diaTodo
                    && x.AdmId == eventoBase[0].AdmId).ToList();
                    foreach (var agenda in agendas)
                    {
                        agenda.Observacao = "Agendamento Disponível Novamente";
                        await _agendaService.AtualizarObservacaoAgenda(agenda);
                    }


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
        //Arrumar
        public async Task<List<string>> ListaDeDatasExcluidas(int admId)
        {
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
            if (eventosUltrapassadosOutroDia.Length > 0)
            {
                if (eventosUltrapassadosOutroDia.Length > 0)
                    await ExcluirEventos(eventosUltrapassadosOutroDia);
            }

            var eventosMesmaData = eventosPorPrestador.Select(x => x.DataHora.Date).ToList();
            var dataMaisDeUma = eventosMesmaData.GroupBy(x => x)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();


            List<DateTime> eventosMesmoDia = new List<DateTime>();
            List<DateTime> eventosDistinct = new List<DateTime>();
            List<string> result = new List<string>();
            if (dataMaisDeUma.Count == 0)
            {
                var datasSemEventosDiaTodo = eventosPorPrestador.Where(x => x.DataHora.Date >= DateTime.Now.Date).Select(x => x.DataHora).ToList();
                datasSemEventosDiaTodo.RemoveAll(x => x.Date == DateTime.Today && x.TimeOfDay < DateTime.Now.TimeOfDay && x.TimeOfDay != diaTodo);

                datasSemEventosDiaTodo.OrderBy(x => x.Date);
                datasSemEventosDiaTodo.OrderBy(x => x.TimeOfDay);
                var datas = datasSemEventosDiaTodo.Where(x => x >= DateTime.Now.Date)
                            .Select(x => x.TimeOfDay == diaTodo ? x.Day + "/" + x.Month + "/" + x.Year : x.ToString()).ToList();

                foreach (var item in datas)
                {
                    result.Add(item);
                }
            }
            else
            {
                var datasSemEventosDiaTodo = eventosPorPrestador.Where(x => x.DataHora.Date >= DateTime.Now.Date).Select(x => x.DataHora.Date).ToArray();

                List<DateTime> datasConvertidas = new List<DateTime>();
                foreach (var item in datasSemEventosDiaTodo)
                {
                    datasConvertidas.Add(item);
                }

                var dataRepetida = datasConvertidas.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

                var dataHoras = eventosPorPrestador.Where(x => x.DataHora.Date >= DateTime.Now.Date).Select(x => x.DataHora).ToList();
                List<DateTime> datasTratadas = new List<DateTime>();
                foreach (var item in dataRepetida)
                {

                    var data = dataHoras.Where(x => x.Date == item.Date).ToList();
                    if (data.Any(x => x.TimeOfDay == diaTodo))
                    {
                        data.RemoveAll(x => x.TimeOfDay != diaTodo);
                    }
                    if (data.Count > 0)
                    {
                        foreach (var dt in data)
                        {
                            datasTratadas.Add(dt);
                        }

                    }

                }

                foreach (var item in datasTratadas)
                {
                    eventosMesmoDia.Add(item);
                }

                var dataHorasDiversas = eventosPorPrestador.Where(x => x.DataHora.Date >= DateTime.Now.Date).Select(x => x.DataHora).ToList();
                List<DateTime> datasRefinadas = new List<DateTime>();
                foreach (var item in dataRepetida)
                {
                    dataHorasDiversas.RemoveAll(x => x.Date == item.Date);
                }


                foreach (var item in dataHorasDiversas)
                {
                    eventosDistinct.Add(item);
                }

                datasTratadas.RemoveAll(x => x.Date == DateTime.Today && x.TimeOfDay < DateTime.Now.TimeOfDay && x.TimeOfDay != diaTodo);
                dataHorasDiversas.RemoveAll(x => x.Date == DateTime.Today && x.TimeOfDay < DateTime.Now.TimeOfDay && x.TimeOfDay != diaTodo);

                List<DateTime> datasUnidas = new List<DateTime>();
                foreach (var item in datasTratadas)
                {
                    datasUnidas.Add(item);
                }
                foreach (var item in dataHorasDiversas)
                {
                    datasUnidas.Add(item);
                }

                datasUnidas.OrderBy(x => x);
                var datas = datasUnidas.Where(x => x >= DateTime.Now.Date)
                            .Select(x => x.TimeOfDay == diaTodo ? x.Day + "/" + x.Month + "/" + x.Year : x.ToString()).ToList();


                foreach (var item in datas)
                {
                    result.Add(item.ToString());
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
