using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Repository
{
    public interface IAgendaRepository
    {
         void Add<T>( T entity) where T : class;
         void Update<T>( T entity) where T : class;
         void Delete<T>( T entity) where T : class;
         void DeleteRange<T>( T[] entity) where T : class;

         Task<bool> SaveChangesAsync();
         ///EVENTOS
         Task<Agenda[]> teste();
         Task<Agenda[]> ObterTodosAgendamentosPorUsuarioAsync(int usuarioId);
         Task<Agenda> ObterAgendamentoPorIdAsync(int agendamentoId);
         Task<User[]> ObterTodosAdminsAsync();
         Task<Agenda[]> ObterClientesAgendadosMesmaDataAsync(Agenda agenda);
         Task<Agenda[]> ObterDiasAgendadosAsync();
         Task<List<TimeSpan>> ObterHorariosAtendimento(Agenda agenda);
         Task<List<TimeSpan>> ObterHorariosDisponiveis(string empresa, DateTime data);
         Agenda[] ObterServicosFinalizadosAsync(Agenda[] agendamentos);
         Agenda[] ObterServicosVencidosAsync(Agenda[] agendamentos);
         Task<User[]> EmpresaCadastradaAsync(User user);
         Task<bool> TemEmpresa (string empresa);
         Task<List<string>> FiltrarEmpresas(string textEmpresa);  
    }
}