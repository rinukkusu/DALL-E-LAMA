using DALL_E_LAMA.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALL_E_LAMA.Data
{
    public class DalleDbContext : DbContext
    {
        public DbSet<Generation> Generations { get; set; }

        public DalleDbContext()
            : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=dalle.sqlite;Cache=Shared");

            base.OnConfiguring(optionsBuilder);
        }
    }
}