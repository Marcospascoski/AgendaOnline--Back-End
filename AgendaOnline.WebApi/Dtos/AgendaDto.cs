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
        
        [Required (ErrorMessage="Campo Data é obrigatório")]
        public DateTime DataHora { get; set; }

        [Phone]
        [Required (ErrorMessage="Campo Celular é obrigatório")]
        public string CelularCliente { get; set; }

        public string CelularAdm { get; set; }

        public string Observacao { get; set; }
        
        public string Endereco { get; set; }

        public TimeSpan Duracao { get; set; }

        public string Segmento { get; set; }

        public string Empresa { get; set; }

        public string Cidade { get; set; }

        public string ImagemPerfilCliente { get; set; }

        public string ImagemPerfilPrestador { get; set; }

        public int? UsuarioId { get; set; }

        public int? AdmId { get; set; }
    }
}