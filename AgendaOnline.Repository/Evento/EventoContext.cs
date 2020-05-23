using Microsoft.EntityFrameworkCore;
using AgendaOnline.Domain;

namespace AgendaOnline.Repository
{
    public class EventoContext : DbContext   
    {
         public EventoContext(DbContextOptions<EventoContext> options): base (options){}
         
         public DbSet<Evento> Eventos { get; set; }

    }
}