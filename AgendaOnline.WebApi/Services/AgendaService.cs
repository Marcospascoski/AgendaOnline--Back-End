using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;
using AgendaOnline.Repository;
using AgendaOnline.WebApi.Dtos;
using AgendaOnline.WebApi.Services.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaOnline.WebApi.Services
{
    public class AgendaService
    {
        private readonly IAgendaRepository _repo;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;

        public AgendaService(IAgendaRepository repo, IMapper mapper, UserManager<User> userManager)
        {
            _userManager = userManager;
            _mapper = mapper;
            _repo = repo;
        }

        public async Task<List<TimeSpan>> ListarHorariosDisponiveis(string empresa, DateTime data)
        {

            var temEmpresa = await _repo.TemEmpresa(empresa);
            TimeSpan semDuracao = new TimeSpan(0, 0, 0);
            TimeSpan duracaoEmpresaNaoEstipulada = new TimeSpan(1, 0, 0);
            if (temEmpresa)
            {
                if (data >= DateTime.Now.Date)
                {
                    var horariosDisponiveis = await _repo.ObterHorariosDisponiveis(empresa, data.Date);
                    var semDisponibilidade = horariosDisponiveis.FirstOrDefault();
                    if (semDisponibilidade != duracaoEmpresaNaoEstipulada)
                    {
                        if (horariosDisponiveis.Count > 0)
                        {
                            try
                            {
                                return horariosDisponiveis;
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
                        throw new BusinessException("duracaoNaoEstipulada");
                    }

                }
                else
                {
                    throw new BusinessException("diaVencido");
                }

            }
            else
            {
                throw new BusinessException("empresainvalida");
            }

        }

        public async Task<List<string>> FiltrarEmpresas(string text, string segmento, string cidade)
        {
            var resultadosFiltro = await _repo.FiltrarEmpresas(text, segmento, cidade);
            if (resultadosFiltro.Count > 0)
            {
                try
                {
                    return resultadosFiltro;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                throw new BusinessException("Não encontrado");
            }
        }

        public async Task<List<string>> FiltrarCidades(string text, string segmento)
        {
            var resultadosFiltro = await _repo.FiltrarCidades(text, segmento);
            if (resultadosFiltro.Count > 0)
            {
                try
                {
                    return resultadosFiltro;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                throw new BusinessException("Não encontrado");
            }
        }

        public async Task<List<string>> FiltrarSegmentos(string text, string cidade)
        {
            var resultadosFiltro = await _repo.FiltrarSegmentos(text, cidade);
            if (resultadosFiltro.Count > 0)
            {
                try
                {
                    return resultadosFiltro;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                throw new BusinessException("Não encontrado");
            }
        }

        public async Task<List<string>> FiltrarClientes(string text)
        {
            var resultadosFiltro = await _repo.FiltrarClientes(text);
            if (resultadosFiltro.Count > 0)
            {
                try
                {
                    return resultadosFiltro;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                throw new BusinessException("Não encontrado");
            }
        }

        public async Task<List<string>> ListaDiasAgendados(int AdmId)
        {
            var dias = await _repo.ObterDiasAgendadosAsync(AdmId);
            var diasDto = _mapper.Map<AgendaDto[]>(dias);
            var results = diasDto.ToArray().Select(x => x.DataHora.Day + "/" + x.DataHora.Month + "/" + x.DataHora.Year).Distinct().ToList();

            if (results.Count > 0)
            {
                try
                {
                    return results;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            else
            {
                throw new BusinessException("vazio");
            }
        }

        public async Task<Agenda> SalvarAlteracoes(AgendaDto agendaDto, string verbo)
        {
            agendaDto.DataHora = agendaDto.DataHora.AddHours(-3);
            //string data = agendaDto.DataHora.Month + "/" + agendaDto.DataHora.Day + "/" + agendaDto.DataHora.Year + " "
            //    + agendaDto.DataHora.TimeOfDay;
            //agendaDto.DataHora = Convert.ToDateTime(data);
            //Implementar no Service
            TimeSpan semDuracao = new TimeSpan(0, 0, 0);

            var agendamentoModel = _mapper.Map<Agenda>(agendaDto);
            var agendamento = await _repo.ObterAgenda(agendamentoModel);
            var agenda = agendamento.FirstOrDefault();
            var admins = await _repo.ObterTodosUsuariosAsync();
            var empresa = admins.Where(x => x.Id == agendaDto.UsuarioId || x.Id == agendaDto.AdmId).Select(x => x.Company).FirstOrDefault();
            var temEmpresa = await _repo.TemEmpresa(empresa);
            if (agendamento == null && verbo.Equals("put")) throw new BusinessException("-");
            var clientesAgendados = await _repo.ObterClientesAgendadosMesmaDataAsync(agendamentoModel);
            var horariosAtendimento = await _repo.ObterHorariosAtendimento(agendamentoModel);
            var horarioInicioFim = await _repo.ObterInicioFim(agendamentoModel);
            var agendamentoIndisponivel = await _repo.VerificarIndisponibilidade(agendamentoModel);

            TimeSpan horarioAgendado = TimeSpan.Parse(agendaDto.DataHora.ToString("HH:mm:ss"));

            if (temEmpresa)
            {
                if (agendamentoIndisponivel.ToString() == "")
                {
                    if (agendamentoModel.DataHora > DateTime.Now)
                    {
                        if (clientesAgendados.Length <= 0)
                        {
                            if (horariosAtendimento.Count > 1 && horariosAtendimento[0] != semDuracao)
                            {
                                if (horariosAtendimento.Contains(horarioAgendado))
                                {

                                    if (verbo.Equals("put"))
                                    {
                                        try
                                        {
                                            _mapper.Map(agendaDto, agenda);
                                            _repo.Update(agenda);
                                            await _repo.SaveChangesAsync();
                                            return agendamentoModel;
                                        }
                                        catch (DbConcurrencyException e)
                                        {
                                            throw new DbConcurrencyException(e.Message);
                                        }

                                    }
                                    else if (verbo.Equals("post"))
                                    {
                                        try
                                        {
                                            _repo.Add(agendamentoModel);
                                            await _repo.SaveChangesAsync();
                                            return agendamentoModel;
                                        }
                                        catch (DbConcurrencyException e)
                                        {
                                            throw new DbConcurrencyException(e.Message);
                                        }

                                    }

                                }
                                else
                                {
                                    throw new BusinessException("valido");
                                }
                            }
                            // deixar horarioInicioFim.Count == 1 
                            else if (horarioInicioFim.Count == 1 && horarioInicioFim[0] == semDuracao)
                            {
                                throw new BusinessException("horarioImproprio");
                            }
                            else
                            {

                                if (verbo.Equals("put"))
                                {
                                    try
                                    {
                                        _mapper.Map(agendaDto, agenda);
                                        _repo.Update(agenda);
                                        await _repo.SaveChangesAsync();
                                        return agendamentoModel;
                                    }
                                    catch (DbConcurrencyException e)
                                    {
                                        throw new DbConcurrencyException(e.Message);
                                    }

                                }
                                else if (verbo.Equals("post"))
                                {
                                    try
                                    {
                                        _repo.Add(agendamentoModel);
                                        await _repo.SaveChangesAsync();
                                        return agendamentoModel;
                                    }
                                    catch (DbConcurrencyException e)
                                    {
                                        throw new DbConcurrencyException(e.Message);
                                    }

                                }
                            }
                        }
                        else
                        {
                            throw new BusinessException("dataCerta");
                        }
                    }
                    else
                    {
                        throw new BusinessException("momento");

                    }
                }
                else
                {
                    throw new BusinessException(agendamentoIndisponivel.ToString());
                }
            }
            else
            {
                throw new BusinessException("empresainvalida");
            }
            return agendamentoModel;

        }

        public void MotorRemocao(int UserId)
        {
            
            var idDataServicoFinalizado = _repo.ObterServicosFinalizadosAsync(UserId);
            var idDataServicosVencidos = _repo.ObterServicosVencidosAsync(UserId);
            

            foreach (var agendamento in idDataServicoFinalizado)
            {
                try
                {
                    _repo.Delete(agendamento);
                    _repo.SaveChangesAsync();
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }

            }
            foreach (var agendamento in idDataServicosVencidos)
            {
                try
                {
                    _repo.Delete(agendamento);
                    _repo.SaveChangesAsync();
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }

        }

        public async Task<Agenda> DeletarAgendamentos(int AgendaId)
        {
            var agendamento = await _repo.ObterAgendamentoPorIdAsync(AgendaId);
            if (agendamento == null)
            {
                throw new BusinessException("naoEncontrado");
            }
            else
            {
                try
                {
                    _repo.Delete(agendamento);
                    await _repo.SaveChangesAsync();
                    return agendamento;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }

            }

        }

        public async Task<List<User>> ListaDeAdmins()
        {
            var usuarios = await _repo.ObterTodosUsuariosAsync();
            var adms = usuarios.Where(x => x.Role == "Adm").ToList();
            List<User> admsSemImagem = new List<User>();
            if (adms.Count > 0)
            {
                try
                {
                    foreach (var adm in adms)
                    {
                        adm.ImagemPerfil = "";
                        admsSemImagem.Add(adm);

                    }
                    return admsSemImagem;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }

            }
            else
            {
                throw new BusinessException("adm not found");
            }
        }

        public async Task<List<User>> ListaDeUsuarios()
        {
            var usuarios = await _repo.ObterTodosUsuariosAsync();
            List<User> usersSemImagem = new List<User>();
            if (usuarios.Count > 0)
            {
                try
                {
                    foreach (var user in usuarios)
                    {
                        user.ImagemPerfil = "";
                        usersSemImagem.Add(user);

                    }
                    return usersSemImagem;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }

            }
            else
            {
                throw new BusinessException("user not found");
            }
        }

        public async Task<User> ObterUsuario(string userName)
        {
            var usuarios = await _repo.ObterUsuarioAsync(userName);

            try
            {
                if (usuarios != null)
                {
                    return usuarios;
                }
                else
                {
                    throw new BusinessException("user not found");
                }
            }
            catch (DbConcurrencyException e)
            {
                throw new DbConcurrencyException(e.Message);
            }
        }

        public async Task<List<User>> ListaDeClientes()
        {
            var usuarios = await _repo.ObterTodosUsuariosAsync();
            var clientes = usuarios.Where(x => x.Role == "User").ToList();
            List<User> clientesSemImagem = new List<User>();
            if (clientes.Count > 0)
            {
                try
                {
                    foreach (var cliente in clientes)
                    {
                        cliente.ImagemPerfil = "";
                        clientesSemImagem.Add(cliente);

                    }
                    return clientesSemImagem;
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }

            }
            else
            {
                throw new BusinessException("client not found");
            }
        }

        public void SalvarImagemPerfil(int usuarioId, string imagemToBase64)
        {
            try
            {
                var usuario = _repo.ObterUsuarioPorIdAsync(usuarioId).Result;
                usuario.ImagemPerfil = imagemToBase64;

                if (usuario != null)
                {
                    var atualizaDados = _userManager.UpdateAsync(usuario);
                    if (!atualizaDados.Result.Succeeded)
                        throw new BusinessException("update failed");

                }
                else
                {
                    throw new BusinessException("user not found");
                }
            }
            catch (DbConcurrencyException e)
            {

                throw new DbConcurrencyException(e.Message);
            }

        }

        public async Task<string> ObterImagemDePerfil(int usuarioId)
        {
            try
            {
                var usuario = await _repo.ObterUsuarioPorIdAsync(usuarioId);

                if (usuario != null)
                {
                    var imagemUsuario = usuario.ImagemPerfil;
                    if(imagemUsuario.Length == 0)
                        throw new BusinessException("user without image");

                    return imagemUsuario;
                }
                else
                {
                    throw new BusinessException("user not found");
                }
            }
            catch (DbConcurrencyException e)
            {

                throw new DbConcurrencyException(e.Message);
            }

        }

        public async Task AtualizarObservacaoAgenda(Agenda agenda)
        {
            try
            {
                _repo.Update(agenda);
                await _repo.SaveChangesAsync();
            }
            catch (DbConcurrencyException e)
            {
                throw new DbConcurrencyException(e.Message);
            }

        }

    }
}
