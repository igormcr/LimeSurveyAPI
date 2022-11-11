using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class Participante
    {
        public string emailParticipante { get; set; }
        public string nomeParticipante { get; set; }
        public string primeiroNome { get; set; }
        public string ultimoNome { get; set; }
        [Key, Column(Order = 0), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string cd_chave_externo { get; set; }
        public string cd_situacao { get; set; }
        [Key, Column(Order = 1)]
        public string id_envio_pesquisa { get; set; }
        public string id_pesquisa { get; set; }
        public string id_usuario_retorno { get; set; }
    }
}