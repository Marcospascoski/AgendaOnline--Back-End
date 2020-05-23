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

        public string Email { get; set; }

        public DateTime DataHora { get; set; }
        
        public string Celular { get; set; }

        public int? AdmId { get; set; }

        public virtual User User { get;}
    }
}