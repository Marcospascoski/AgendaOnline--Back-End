using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Repository
{
    public class EventoRepository : IEventoRepository
    {

        private readonly EventoContext _context;

        public EventoRepository(EventoContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        ///GERAIS
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Update<T>(T entity) where T : class
        {
            _context.Update(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public void DeleteRange<T>(T[] entityArray) where T : class
        {
            _context.RemoveRange(entityArray);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync()) > 0;
        }

        public async Task<Evento[]> DataHorasUltrapassadas(Evento evento)
        {
            IQueryable<Evento> query = _context.Eventos.Where(e => e.AdmId == evento.AdmId && e.DataHora < DateTime.Now);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<Evento[]> ObterEventosPorAdmIdAsync(int AdmId)
        {
            IQueryable<Evento> query = _context.Eventos.Where( x => x.AdmId == AdmId).OrderByDescending(x => x.Id);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<bool> EventoRepetido(Evento evento)
        {

            TimeSpan diaTodoIndisponivel = new TimeSpan(0, 0, 0);
            var dataHoraAdm = _context.Eventos.Where(e => e.AdmId == evento.AdmId && e.DataHora > DateTime.Now).ToList();
            var diaEscolhido = dataHoraAdm.Where(x => x.DataHora.Date == evento.DataHora.Date).ToList();
            var diaIndisponibilizado = diaEscolhido.Where(x => x.DataHora.TimeOfDay == diaTodoIndisponivel).ToList();
            var dataHoraEscolhida = dataHoraAdm.Where(x => x.DataHora == evento.DataHora).ToList();

            if (dataHoraEscolhida.Count > 0)
            {
                return await Task.FromResult(true);
            }
            if (diaIndisponibilizado.Count > 0)
            {
                return await Task.FromResult(true);
            }
            else
            {
                return await Task.FromResult(false);
            }

        }

    }
}