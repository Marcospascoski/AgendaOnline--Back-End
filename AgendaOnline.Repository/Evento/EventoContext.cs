using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;
using Microsoft.EntityFrameworkCore.Design;

namespace AgendaOnline.Repository
{
    public class EventoContext : DbContext   
    {
         public EventoContext(DbContextOptions<EventoContext> options): base (options){}
         
         public DbSet<Evento> Eventos { get; set; }    
                    
    } 
}