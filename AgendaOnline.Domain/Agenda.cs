using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Domain
{
   public class Agenda
   {
        public int Id { get; set; }

        public string Nome { get; set; }

        public DateTime DataHora { get; set; }

        public TimeSpan Duracao { get; set; }

        public string Segmento { get; set; }

        public string Empresa { get; set; }

        public string Cidade { get; set; }

        public string CelularCliente { get; set; }

        public string CelularAdm { get; set; }
        
        public string Observacao { get; set; }
        
        public string Endereco { get; set; }

        public string ImagemPerfilCliente { get; set; }
        
        public string ImagemPerfilPrestador { get; set; }

        public int? UsuarioId { get; set; }

        public int? AdmId { get; set; }

        public virtual User User { get;}
    }
}