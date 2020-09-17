using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Repository
{
    public interface IAgendaRepository
    {
        void Add<T>(T entity) where T : class;
        void Update<T>(T entity) where T : class;
        void Delete<T>(T entity) where T : class;
        void DeleteRange<T>(T[] entity) where T : class;

        Task<bool> SaveChangesAsync();
        ///EVENTOS
        Task<Agenda[]> ObterAgenda(Agenda agenda);
        Task<List<Agenda>> ObterTodosAgendamentosPorUsuarioAsync(int usuarioId);
        Task<Agenda> ObterAgendamentoPorIdAsync(int agendamentoId);
        Task<List<User>> ObterTodosUsuariosAsync();
        Task<User> ObterUsuarioPorIdAsync(int usuarioId);
        Task<Agenda[]> ObterClientesAgendadosMesmaDataAsync(Agenda agenda);
        Task<Agenda[]> ObterDiasAgendadosAsync(int AdmId);
        Task<List<TimeSpan>> ObterHorariosAtendimento(Agenda agenda);
        Task<List<TimeSpan>> ObterInicioFim(Agenda agenda);
        Task<string> VerificarIndisponibilidade(Agenda agenda);
        Task<List<TimeSpan>> ObterHorariosDisponiveis(string empresa, DateTime data);
        Agenda[] ObterServicosFinalizadosAsync(int UserId);
        Agenda[] ObterServicosVencidosAsync(int UserId);
        Task<User[]> EmpresaCadastradaAsync(User user);
        Task<bool> TemEmpresa(string empresa);
        Task<List<string>> FiltrarEmpresas(string textEmpresa, string segmento, string cidade);
        Task<List<string>> FiltrarSegmentos(string textSegmento, string cidade);
        Task<List<string>> FiltrarCidades(string textCidade, string segmento);
        Task<List<string>> FiltrarClientes(string textClientes);
        Task VerificarExistenciaRoles();

    }
}