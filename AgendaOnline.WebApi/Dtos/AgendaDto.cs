using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.WebApi.Dtos
{
    public class AgendaDto
    {

        public int Id { get; set; }

        [Required (ErrorMessage="Campo Nome é obrigatório")]
        [StringLength (100, MinimumLength=10, ErrorMessage="Preencha seu nome completo")]
        public string Nome { get; set; }
        
        [EmailAddress]
        [Required (ErrorMessage="Campo Email é obrigatório")]
        public string Email { get; set; }

        [Required (ErrorMessage="Campo Data é obrigatório")]
        public DateTime DataHora { get; set; }

        [Phone]
        [Required (ErrorMessage="Campo Celular é obrigatório")]
        public string Celular { get; set; }

        public TimeSpan Duracao { get; set; }

        public int? AdmId { get; set; }
    }
}