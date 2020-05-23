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

        public async Task<bool> EventoRepetido(Evento evento)
        {
            bool result = true;
            IQueryable<Evento> query = _context.Eventos.Where(x => x.AdmId == evento.AdmId && x.DataHora == evento.DataHora);
            query = query.AsNoTracking();

            if(query.ToArrayAsync() != null)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

    }
}