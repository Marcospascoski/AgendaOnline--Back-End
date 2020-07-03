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
    public class AdmController : ControllerBase
    {
        private readonly IAgendaRepository _repo;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;

        public AdmController(IAgendaRepository repo,
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

        [HttpGet("GetAdm")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAdm()
        {
            return Ok(new AdmDto());
        }

        [HttpPost("RegisterAdm")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAdm(AdmDto admDto)
        {
            try
            {
                var admUser = _mapper.Map<User>(admDto);
                var empresaCadastrada = await _repo.EmpresaCadastradaAsync(admUser);
                
                if (empresaCadastrada.Length > 0)
                {
                    return Ok("Empresa já cadastrada");
                }
                else{
                    admUser.Role = "Adm";
                    var result = await _userManager.CreateAsync(admUser, admDto.Password);
                    await _userManager.AddToRoleAsync(admUser, admUser.Role);

                    var userToReturn = _mapper.Map<AdmDto>(admUser);
                    if (result.Succeeded)
                    {
                        return Created("GetUser", userToReturn);
                    }
                    return BadRequest(result.Errors);
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
    }
}