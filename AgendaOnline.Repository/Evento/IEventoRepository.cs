using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Repository
{
    public interface IEventoRepository
    {
         void Add<T>( T entity) where T : class;
         void Update<T>( T entity) where T : class;
         void Delete<T>( T entity) where T : class;
         void DeleteRange<T>( T[] entity) where T : class;

         Task<bool> SaveChangesAsync();

        Task<Evento[]> EventoExistente(Evento evento);
        Task<Evento[]> DataHorasUltrapassadas(Evento evento);
        Task<Evento[]> ObterEventosPorAdmIdAsync(int AdmId);
        Task<bool> EventoRepetido(Evento evento);   
    }
}