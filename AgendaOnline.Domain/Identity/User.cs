using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AgendaOnline.Domain.Identity
{
    public class User : IdentityUser<int>
    {
        public override int Id { get; set; }
        
        [Column(TypeName = "nvarchar(150)")]
        public string FullName { get; set; }
        public string Password { get; set; }
        public string ImagemPerfil { get; set; }
        public string Company { get; set; }
        public string MarketSegment { get; set; }
        public string Cidade { get; set; }
        public TimeSpan Abertura { get; set; }
        public TimeSpan AlmocoIni { get; set; }
        public TimeSpan AlmocoFim { get; set; }
        public int Fds { get; set; }     
        public TimeSpan Fechamento { get; set; }
        public TimeSpan Duracao { get; set; }
        public string Celular { get; set; }
        public string Endereco { get; set; }    
        public List<UserRole> UserRoles { get; set; }
        public string Role { get; set; }
    }
}