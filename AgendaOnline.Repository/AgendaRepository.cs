using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Repository
{
    public class AgendaRepository : IAgendaRepository
    {

        private readonly AgendaContext _context;

        public AgendaRepository(AgendaContext context)
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

        public async Task<Agenda[]> teste()
        {
            IQueryable<Agenda> query = _context.Agendas;
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<Agenda[]> ObterTodosAgendamentosPorUsuarioAsync(int UserId)
        {
            IQueryable<Agenda> query = _context.Agendas.Where(x => x.AdmId == UserId);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<User[]> ObterTodosAdminsAsync()
        {
            IQueryable<User> query = _context.Users.OrderByDescending(x => x.Id);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<Agenda[]> ObterClientesAgendadosMesmaDataAsync(Agenda agenda)
        {
            IQueryable<Agenda> query = _context.Agendas.Where(a => a.DataHora == agenda.DataHora && a.AdmId == agenda.AdmId);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<Agenda[]> ObterDiasAgendadosAsync()
        {
            IQueryable<Agenda> query = _context.Agendas.OrderBy(x => x.DataHora);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<List<TimeSpan>> ObterHorariosDisponiveis(string empresa, DateTime data)
        {
            var idPorEmpresa = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Id).First();
            List<DateTime> datasPorId;
            //Horarios agendados pela data e nome da empresa
            List<TimeSpan> horasPorDataEmpresa = new List<TimeSpan>();
            if (idPorEmpresa > 0)
            {
                datasPorId = _context.Agendas.Where(x => x.AdmId == idPorEmpresa).Select(x => x.DataHora).ToList();
                if(datasPorId.Count > 0)
                {
                    horasPorDataEmpresa = _context.Agendas.Where(x => x.DataHora.Date == data.Date).Select(x => x.DataHora.TimeOfDay).ToList();
                }
                
            }

            var duracao = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Duracao).ToList().First();
            var abertura = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Abertura).ToList().First();
            var fechamento = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Fechamento).ToList().First();
            var almocoIni = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.AlmocoIni).ToList().First();
            var almocoFim = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.AlmocoFim).ToList().First();

            //Horarios que a empresa trabalha
            List<TimeSpan> horarios = new List<TimeSpan>();
            TimeSpan calc = new TimeSpan();
            calc = abertura;
            horarios.Add(calc);
            while (calc < fechamento)
            {
                calc = calc.Add(duracao);
                horarios.Add(calc);
            }

            if(horarios.Last() > fechamento)
            {
                horarios.Remove(horarios.Last());
            }

            foreach (var horariosAgendados in horasPorDataEmpresa)
            {
                if (horarios.Contains(horariosAgendados))
                {
                    horarios.Remove(horariosAgendados);
                }
            }

            horarios.RemoveAll(x => x >= almocoIni && x <= almocoFim);

            return horarios;
        }

        public async Task<List<TimeSpan>> ObterHorariosAtendimento(Agenda agenda)
        {
            var duracao = _context.Usuarios.Where(x => x.Id == agenda.AdmId).Select(x => x.Duracao).ToList().First();
            var abertura = _context.Usuarios.Where(x => x.Id == agenda.AdmId).Select(x => x.Abertura).ToList().First();
            var fechamento = _context.Usuarios.Where(x => x.Id == agenda.AdmId).Select(x => x.Fechamento).ToList().First();
            var almocoIni = _context.Usuarios.Where(x => x.Id == agenda.AdmId).Select(x => x.AlmocoIni).ToList().First();
            var almocoFim = _context.Usuarios.Where(x => x.Id == agenda.AdmId).Select(x => x.AlmocoFim).ToList().First();

            List<TimeSpan> horarios = new List<TimeSpan>();
            TimeSpan calc = new TimeSpan();
            calc = abertura;
            horarios.Add(calc);
            while (calc < fechamento)
            {
                calc = calc.Add(duracao);
                horarios.Add(calc);
            }
            
            horarios.RemoveAll(x => x >= almocoIni && x <= almocoFim);
            
            return horarios;
        }

        public Agenda[] ObterServicosFinalizadosAsync(Agenda[] agendamentos)
        {
            Agenda[] query = agendamentos.Where(a => a.DataHora <= DateTime.Now && a.DataHora.AddMinutes(50) <= DateTime.Now).ToArray();

            return query;
        }

        public Agenda[] ObterServicosVencidosAsync(Agenda[] agendamentos)
        {
            Agenda[] query = agendamentos.Where(a => a.DataHora < DateTime.Now).ToArray();

            return query;
        }
        public async Task<Agenda> ObterAgendamentoPorIdAsync(int AgendaId)
        {
            IQueryable<Agenda> query = _context.Agendas;
            query = query.AsNoTracking().OrderByDescending(c => c.DataHora)
                         .Where(c => c.Id == AgendaId);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<User[]> EmpresaCadastradaAsync(User user)
        {
            IQueryable<User> query = _context.Users.Where(x => x.Company == user.Company);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<bool> TemEmpresa(string empresa)
        {
            var lResult = true;
            var idPorEmpresa = _context.Usuarios.Where(x => x.Company == empresa).ToList();
            if (idPorEmpresa.Count > 0) lResult = true;
            else lResult = false;

            return lResult;
        }

        public async Task<List<string>> FiltrarEmpresas(string textEmpresa)
        {
            var todasEmpresas = _context.Usuarios.Where(x => x.Company.Contains(textEmpresa)).Select(x => x.Company).ToList();

            return todasEmpresas;
        }
    }
}