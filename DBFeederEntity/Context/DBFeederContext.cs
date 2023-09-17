using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBFeederEntity.Context;

namespace DBFeederEntity.Context
{
    public class DBFeederContext : DbContext
    {
        public DBFeederContext() :
            base()
        {
        }

        public DBFeederContext(DbContextOptions options) :
            base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Resources.DB_ConnectionString);
        }
    }
}