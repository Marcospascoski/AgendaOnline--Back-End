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
        public string ImagemPerfil { get; set; }
        public string Celular { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }   
    }
}