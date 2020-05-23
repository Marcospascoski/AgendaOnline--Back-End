using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaOnline.Domain.Identity;

namespace AgendaOnline.Domain
{
    public class Evento
    {
        public int Id { get; set; }
        public string Motivo { get; set; }
        public DateTime DataHora { get; set; }
        public int AdmId { get; set; }
    }
}