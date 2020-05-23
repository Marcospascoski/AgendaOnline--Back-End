using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.WebApi.Dtos
{
    public class EventoDto
    {

        public int Id { get; set; }

        [Required (ErrorMessage="Campo Motivo é Obrigatório")]
        public string Motivo { get; set; }
        
        [Required (ErrorMessage="Campo Data é obrigatório")]
        public DateTime DataHora { get; set; }

        public int AdmId { get; set; }
    }
}