using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AgendaOnline.Domain;

namespace AgendaOnline.WebApi.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Company { get; set; }
        public string ImagemPerfil { get; set; }
        public string Email { get; set; }
        public string MarketSegment { get; set; }
        public TimeSpan Abertura { get; set; }
        public TimeSpan Fechamento { get; set; }
        public TimeSpan? Duracao { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }   
    }
}