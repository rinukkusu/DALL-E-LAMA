using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALL_E_LAMA.Data.Models
{
    [Table("Generations")]
    [Index(nameof(Id))]
    [Index(nameof(MessageId))]
    public class Generation
    {
        [StringLength(64)]
        public string Id { get; set; }

        public int MessageId { get; set; }
    }
}