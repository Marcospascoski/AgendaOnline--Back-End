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

        public async Task<Agenda[]> ObterAgenda(Agenda agenda)
        {
            IQueryable<Agenda> query = _context.Agendas.Where(x => x.UsuarioId == agenda.UsuarioId && x.DataHora.Date == agenda.DataHora.Date);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<Agenda[]> ObterTodosAgendamentosPorUsuarioAsync(int UserId)
        {
            var duracaoAdm = _context.Users.Where(x => x.Id == UserId).Select(x => x.Duracao).FirstOrDefault();
            TimeSpan semDuracao = new TimeSpan(0, 0, 0);
            IQueryable<Agenda> query = _context.Agendas.Where(x => x.UsuarioId == UserId);
            query = query.AsNoTracking();

            for (int i = 0; i < query.ToArray().Length; i++)
            {
                if(query.ToArray()[i].Duracao == semDuracao)
                {
                    query.ToArray()[i].Duracao = duracaoAdm;
                }
            }

            return await query.ToArrayAsync();
        }

        public async Task<List<User>> ObterTodosUsuariosAsync()
        {
            List<User> query = await _context.Users.OrderByDescending(x => x.Id).ToListAsync();

            return query;
        }

        public async Task<User> ObterUsuarioPorIdAsync(int usuarioId)
        {
            List<User> query = await _context.Users.Where(x => x.Id == usuarioId).OrderByDescending(x => x.Id).ToListAsync();

            return query.FirstOrDefault();
        }

        public async Task<Agenda[]> ObterClientesAgendadosMesmaDataAsync(Agenda agenda)
        {
            IQueryable<Agenda> query = _context.Agendas.Where(a => a.DataHora == agenda.DataHora && a.UsuarioId == agenda.UsuarioId);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }
        
        public async Task<Agenda[]> ObterDiasAgendadosAsync(int UsuarioId)
        {
            IQueryable<Agenda> query = _context.Agendas.Where(x => x.UsuarioId == UsuarioId).OrderBy(x => x.DataHora);
            query = query.AsNoTracking();

            return await query.ToArrayAsync();
        }

        public async Task<List<TimeSpan>> ObterHorariosDisponiveis(string empresa, DateTime data)
        {
            var idPorEmpresa = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Id).First();
            List<DateTime> datasPorId;
            //Horarios agendados pela data e nome da empresa
            var duracao = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Duracao).ToList().First();
            var abertura = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Abertura).ToList().First();
            var fechamento = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.Fechamento).ToList().First();
            var almocoIni = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.AlmocoIni).ToList().First();
            var almocoFim = _context.Usuarios.Where(x => x.Company == empresa).Select(x => x.AlmocoFim).ToList().First();
            var datasIndisponiveis = _context.Eventos.Where(x => x.AdmId == idPorEmpresa && x.DataHora.Date == data.Date).Select(x => x.DataHora).ToList();
            var horariosAgendados = _context.Agendas.Where(x => x.UsuarioId == idPorEmpresa).Select(x => x.DataHora.TimeOfDay).ToList();

            //Horarios que a empresa trabalha
            List<TimeSpan> horarios = new List<TimeSpan>();
            List<TimeSpan> horariosIndisponiveis = new List<TimeSpan>();
            TimeSpan diaIndisponivel = new TimeSpan(0,0,0);
            TimeSpan duracaoEmpresaNaoEstipulada = new TimeSpan(1, 0, 0);

            TimeSpan calc = new TimeSpan();
            calc = abertura;
            horarios.Add(calc);

            if(duracao == diaIndisponivel)
            {
                horarios.RemoveAll(x => x.Equals(x));
                horarios.Add(duracaoEmpresaNaoEstipulada);
                return horarios;
            }
            else
            {
                while (calc < fechamento)
                {
                    calc = calc.Add(duracao);
                    horarios.Add(calc);
                }

                foreach (var horIndis in datasIndisponiveis)
                {
                    horariosIndisponiveis.Add(horIndis.TimeOfDay);
                }

                if (horarios.Last() > fechamento)
                {
                    horarios.Remove(horarios.Last());
                }

                foreach (var horInd in horariosIndisponiveis)
                {
                    if (horInd == diaIndisponivel)
                    {
                        horarios.RemoveAll(x => x.Equals(x));
                    }
                    else
                    {
                        horarios.Remove(horInd);
                    }

                }
                horarios.RemoveAll(x => x >= almocoIni && x < almocoFim);

                if(data == DateTime.Today)
                {
                    horarios.RemoveAll(x => x < DateTime.Now.TimeOfDay);
                }

                foreach (var horariosMarcados in horariosAgendados)
                {
                    horarios.Remove(horariosMarcados);
                }
            }

            return horarios;
        }

        public async Task<List<TimeSpan>> ObterHorariosAtendimento(Agenda agenda)
        {
            var duracao = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.Duracao).ToList().First();
            var abertura = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.Abertura).ToList().First();
            var fechamento = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.Fechamento).ToList().First();
            var almocoIni = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.AlmocoIni).ToList().First();
            var almocoFim = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.AlmocoFim).ToList().First();
            
            
            TimeSpan semDuracao = new TimeSpan(0, 0, 0);
            List<TimeSpan> horarios = new List<TimeSpan>();

            if (duracao == semDuracao)
            {
                horarios.Add(semDuracao);
            }
            else
            {
                TimeSpan calc = new TimeSpan();
                calc = abertura;
                horarios.Add(calc);
                while (calc < fechamento)
                {
                    calc = calc.Add(duracao);
                    horarios.Add(calc);
                }

                horarios.RemoveAll(x => x >= almocoIni && x < almocoFim);
            }
            
            return horarios;
        }

        public async Task<List<TimeSpan>> ObterInicioFim(Agenda agenda)
        {
            var duracao = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.Duracao).ToList().First();
            var abertura = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.Abertura).ToList().First();
            var fechamento = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.Fechamento).ToList().First();
            var almocoIni = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.AlmocoIni).ToList().First();
            var almocoFim = _context.Usuarios.Where(x => x.Id == agenda.UsuarioId).Select(x => x.AlmocoFim).ToList().First();

            var horaMarcada = agenda.DataHora.TimeOfDay;

            TimeSpan semDuracao = new TimeSpan(0, 0, 0);

            List<TimeSpan> horarios = new List<TimeSpan>();

            horarios.Add(abertura);
            horarios.Add(almocoIni);
            horarios.Add(almocoFim);
            horarios.Add(fechamento);
            
            //Arrumar if
            if((horaMarcada > horarios[0] && horaMarcada < horarios[1]) || 
               (horaMarcada < horarios[3] && horaMarcada > horarios[2]))
            {
               for (int hora = 0; hora < horarios.Count; hora++)
               {
                   if(hora % 2 == 1){
                      if(horaMarcada >= horarios[hora] && horaMarcada < horarios[hora + 1])
                      {
                         horarios.RemoveAll(x => x.Equals(x));   
                         horarios.Add(semDuracao); 
                      }               
                   }
               }
            }
            else
            {
                horarios.RemoveAll(x => x.Equals(x));
                horarios.Add(semDuracao);
            }

            return horarios;
        }

        public async Task<string> VerificarIndisponibilidade(Agenda agenda)
        {

            TimeSpan diaTodoIndisponivel = new TimeSpan();
            var dataHoraAdm = _context.Eventos.Where(x => x.AdmId == agenda.UsuarioId && x.DataHora == agenda.DataHora).ToList();
            var dataAdm = _context.Eventos.Where(x => x.AdmId == agenda.UsuarioId && x.DataHora.Date == agenda.DataHora.Date).ToList();

            var agendamentosIndisponiveis = dataHoraAdm.Select(x => x.Motivo).ToList();
            var diaIndisponibilizado = dataAdm.Where(x => x.DataHora.TimeOfDay == diaTodoIndisponivel).Select(x => x.Motivo).ToList();

            if(diaIndisponibilizado.Count > 0)
            {
                return await Task.FromResult(diaIndisponibilizado.FirstOrDefault());
            }
            if (agendamentosIndisponiveis.Count > 0)
            {
                return await Task.FromResult(agendamentosIndisponiveis.FirstOrDefault());
            }

            return await Task.FromResult("");
        }

        public Agenda[] ObterServicosFinalizadosAsync(int UserId)
        {
            TimeSpan semDuracao = new TimeSpan(0,0,0);

            var agendamentos = _context.Agendas.Where(x => x.UsuarioId == UserId).ToList();
            List<Agenda> agendamentoList = new List<Agenda>();
            User adm;
            foreach (var agenda in agendamentos)
            {
                adm = _context.Users.Where(x => x.Id == agenda.UsuarioId).FirstOrDefault();   

                if(adm.Duracao == semDuracao)
                {
                    List<Agenda> agendasServicosRealizados = agendamentos.Where(a => a.UsuarioId == adm.Id &&
                    a.DataHora <= DateTime.Now && a.DataHora.AddMinutes(a.Duracao.Minutes) <= DateTime.Now &&
                    a.DataHora.AddHours(a.Duracao.Hours) <= DateTime.Now).ToList();
                    
                    if(agendasServicosRealizados != null)
                    {
                        foreach (var ag in agendasServicosRealizados)
                        {
                            agendamentoList.Add(ag);
                        }
                    }
                }
                else
                {
                    List<Agenda> agendasVencidas = agendamentos.Where(a => a.UsuarioId == adm.Id && a.DataHora <= DateTime.Now 
                    && a.DataHora.AddMinutes(adm.Duracao.Minutes) <= DateTime.Now && 
                    a.DataHora.AddHours(adm.Duracao.Hours) <= DateTime.Now).ToList();
                    if(agendasVencidas != null)
                    {
                        foreach (var ag in agendasVencidas)
                        {
                            agendamentoList.Add(ag);
                        }
                    }
                     
                }
            }

            var agendamentosDistinct = agendamentoList.Distinct().ToList();
            Agenda[] agendamentosVencidos = new Agenda[agendamentosDistinct.Count];
            for (int i = 0; i < agendamentosDistinct.Count; i++)
            {
                agendamentosVencidos[i] = agendamentosDistinct[i];
            }
            

            return agendamentosVencidos;
        }

        public Agenda[] ObterServicosVencidosAsync(int UserId)
        {
            Agenda[] query = _context.Agendas.Where(a => a.UsuarioId == UserId && a.DataHora.Date < DateTime.Now.Date).ToArray();

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