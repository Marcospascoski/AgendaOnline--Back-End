using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AgendaOnline.Domain.Identity;
using AgendaOnline.WebApi.Dtos;
using System.Linq;
using AgendaOnline.Repository;

namespace AgendaOnline.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IAgendaRepository _repo;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;

        public UserController(IAgendaRepository repo,
                              IConfiguration config,
                              UserManager<User> userManager,
                              SignInManager<User> signInManager,
                              IMapper mapper)
        {
            _repo = repo;
            _signInManager = signInManager;
            _mapper = mapper;
            _config = config;
            _userManager = userManager;
        }

        [HttpGet("ListaDeClientes")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> ListaDeClientes()
        {
            return Ok(new UserDto());
        }

        [HttpPost("RegisterUser")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUser(UserDto userDto)
        {
            try
            {
                var user = _mapper.Map<User>(userDto);
                user.Role = "User";
                var result = await _userManager.CreateAsync(user, userDto.Password);
                await _userManager.AddToRoleAsync(user, user.Role);
                var userToReturn = _mapper.Map<UserDto>(user);
                if (result.Succeeded)
                {
                    return Created("GetUser", userToReturn);
                }
                return BadRequest(result.Errors);
            }
            catch (System.Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Banco de Dados Falhou {ex.Message}");
            }
        }

        [HttpPut("UpdateUser")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateUser(UserDto usuarioDto)
        {
            try
            {
                var senhaAtual = "";
                var senhaAntiga = "";
                var usuarioPorId = await _repo.ObterUsuarioPorIdAsync(usuarioDto.Id);

                if (usuarioPorId == null)
                    return BadRequest("User not found");

                senhaAntiga = usuarioPorId.Password;
                senhaAtual = usuarioDto.Password;

                var usuarioModel = _mapper.Map(usuarioDto, usuarioPorId);
                var atualizaDados = await _userManager.UpdateAsync(usuarioModel);
                var user = await _userManager.GetUserAsync(User);
                var userToReturn = _mapper.Map<UserDto>(usuarioModel);

                if (senhaAntiga.Equals(senhaAtual))
                {
                    if (!atualizaDados.Succeeded)
                        return BadRequest("dados");

                    return Created("GetUser", userToReturn);
                }
                else
                {
                    var atualizaSenha = await _userManager.ChangePasswordAsync(usuarioModel, senhaAntiga, senhaAtual);
                    await _signInManager.RefreshSignInAsync(usuarioModel);
                    if (!atualizaSenha.Succeeded)
                        return BadRequest("senha");

                    return Created("GetUser", userToReturn);
                }

            }
            catch (System.Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Banco de Dados Falhou {ex.Message}");
            }
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(User userLogin)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userLogin.UserName);
                var result = await _signInManager.CheckPasswordSignInAsync(user, userLogin.Password, false);

                if (!result.Succeeded)
                    return NotFound(new { message = "Usu�rio ou senha incorretas" });

                if (result.Succeeded)
                {
                    var role = await _userManager.GetRolesAsync(user);
                    IdentityOptions _options = new IdentityOptions();

                    var key = new SymmetricSecurityKey(Encoding.ASCII
                    .GetBytes(_config.GetSection("AppSettings:Token").Value));

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim("UserId", user.Id.ToString()),
                            new Claim(_options.ClaimsIdentity.RoleClaimType, role.FirstOrDefault())
                        }),
                        Expires = DateTime.Now.AddDays(1),
                        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
                    };
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                    var token = tokenHandler.WriteToken(securityToken);
                    return Ok(new { token });
                }
                return Unauthorized();
            }
            catch (System.Exception ex)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Banco de Dados Falhou {ex.Message}");
            }
        }

        [HttpDelete("DeletarUsuario/{UsuarioId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeletarUsuario(int UsuarioId)
        {
            try
            {
                var usuario = await _repo.ObterUsuarioPorIdAsync(UsuarioId);

                if (usuario == null)
                {
                    return Ok("user not found");
                }
                else
                {
                    _repo.Delete(usuario);
                    await _repo.SaveChangesAsync();
                    return Ok();
                }
            }
            catch (System.Exception ex)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Banco de Dados Falhou {ex.Message}");
            }
        }
    }
}