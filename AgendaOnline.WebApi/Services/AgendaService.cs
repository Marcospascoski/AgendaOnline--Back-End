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
            //Implementar no Service
            TimeSpan semDuracao = new TimeSpan(0, 0, 0);

            var agendamentoModel = _mapper.Map<Agenda>(agendaDto);
            var agenda = await _repo.ObterAgenda(agendamentoModel);
            var agendamento = await _repo.ObterAgendamentoPorIdAsync(agenda.Select(x => x.Id).FirstOrDefault());
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
                                            agendaDto.Id = agendamento.Id;
                                            _mapper.Map(agendaDto, agendamento);
                                            _repo.Update(agendamento);
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
                                        agendaDto.Id = agendamento.Id;
                                        _mapper.Map(agendaDto, agendamento);
                                        _repo.Update(agendamento);
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

        public async Task MotorRemocao(int UserId)
        {

            var idDataServicoFinalizado = _repo.ObterServicosFinalizadosAsync(UserId);
            var idDataServicosVencidos = _repo.ObterServicosVencidosAsync(UserId);

            if (idDataServicoFinalizado.Length > 0)
            {
                try
                {
                    _repo.DeleteRange(idDataServicoFinalizado);
                    await _repo.SaveChangesAsync();
                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }

            }
            else if (idDataServicosVencidos.Length > 0)
            {
                try
                {
                    _repo.DeleteRange(idDataServicoFinalizado);
                    await _repo.SaveChangesAsync();
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
            var admins = usuarios.Where(x => x.Role == "Adm").ToList();

            if (admins.Count > 0)
            {
                try
                {
                    return admins;
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

        public async Task<List<User>> ListaDeClientes()
        {
            var usuarios = await _repo.ObterTodosUsuariosAsync();
            var clientes = usuarios.Where(x => x.Role == "User").ToList();

            if (clientes.Count > 0)
            {
                try
                {
                    return clientes;
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

        public async Task<string> EnviarMotivo(EventoDto eventoDto, string decisao)
        {
            TimeSpan diaTodo = new TimeSpan(0, 0, 0);
            eventoDto.DataHora = eventoDto.DataHora.AddHours(-3);
            var eventoModel = _mapper.Map<Evento>(eventoDto);

            var agendamentoPorUsuario = await _repo.ObterTodosAgendamentosPorUsuarioAsync(eventoModel.AdmId);
            if (agendamentoPorUsuario.Count > 0)
            {

                try
                {
                    //encontrar o agendamento correto pelo AdmId e DataHora.
                    if (eventoModel.DataHora.TimeOfDay == diaTodo)
                    {
                        agendamentoPorUsuario = agendamentoPorUsuario.Where(x => x.DataHora.Date == eventoModel.DataHora.Date).ToList();
                    }
                    else
                    {
                        agendamentoPorUsuario = agendamentoPorUsuario.Where(x => x.DataHora == eventoModel.DataHora).ToList();
                    }
                    if (agendamentoPorUsuario.Count > 0)
                    {
                        if (decisao.Equals("indisponibilizar"))
                        {
                            agendamentoPorUsuario.FirstOrDefault().Observacao = eventoModel.Motivo;
                        }
                        else
                        {
                            agendamentoPorUsuario.FirstOrDefault().Observacao = "Agendamento Disponível Novamente";
                        }
                        _repo.Update(agendamentoPorUsuario.FirstOrDefault());
                        await _repo.SaveChangesAsync();
                        return await Task.FromResult("Atualizado com sucesso");
                    }
                    else
                    {
                        return "não necessário";
                    }

                }
                catch (DbConcurrencyException e)
                {
                    throw new DbConcurrencyException(e.Message);
                }
            }
            return "";

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

    }
}
