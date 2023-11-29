﻿//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
// This code was generated by Entity Developer tool using EF Core template.
// Code is generated on: 11/29/2023 11:09:59 PM
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using DBFeederEntity;

namespace DBFeederEntity.Context
{

    public partial class SampleContext : DBFeederContext
    {

        public SampleContext() :
            base()
        {
            OnCreated();
        }

        public SampleContext(DbContextOptions options) :
            base(options)
        {
            OnCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured ||
                (!optionsBuilder.Options.Extensions.OfType<RelationalOptionsExtension>().Any(ext => !string.IsNullOrEmpty(ext.ConnectionString) || ext.Connection != null) &&
                 !optionsBuilder.Options.Extensions.Any(ext => !(ext is RelationalOptionsExtension) && !(ext is CoreOptionsExtension))))
            {
            }
            CustomizeConfiguration(ref optionsBuilder);
            base.OnConfiguring(optionsBuilder);
        }

        partial void CustomizeConfiguration(ref DbContextOptionsBuilder optionsBuilder);

        public virtual DbSet<TableA> TableAs
        {
            get;
            set;
        }

        public virtual DbSet<TableB> TableBs
        {
            get;
            set;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            this.TableAMapping(modelBuilder);
            this.CustomizeTableAMapping(modelBuilder);

            this.TableBMapping(modelBuilder);
            this.CustomizeTableBMapping(modelBuilder);

            RelationshipsMapping(modelBuilder);
            CustomizeMapping(ref modelBuilder);
        }

        #region TableA Mapping

        private void TableAMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TableA>().ToTable(@"TableA");
            modelBuilder.Entity<TableA>().Property<Guid>(@"ID").HasColumnName(@"ID").IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<TableA>().Property(x => x.WORD_TYPE).HasColumnName(@"WORD_TYPE").IsRequired().ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableA>().Property(x => x.WORD_VALUE).HasColumnName(@"WORD_VALUE").IsRequired().ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableA>().Property(x => x.SOURCE).HasColumnName(@"SOURCE").IsRequired().ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableA>().Property<DateTime>(@"DT_CREATION").HasColumnName(@"DT_CREATION").IsRequired().ValueGeneratedOnAdd().HasDefaultValueSql(@"getdate()");
            modelBuilder.Entity<TableA>().Property<DateTime>(@"DT_UPDATE").HasColumnName(@"DT_UPDATE").IsRequired().ValueGeneratedOnAddOrUpdate().HasDefaultValueSql(@"getdate()");
            modelBuilder.Entity<TableA>().HasKey(@"ID");
        }

        partial void CustomizeTableAMapping(ModelBuilder modelBuilder);

        #endregion

        #region TableB Mapping

        private void TableBMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TableB>().ToTable(@"TableB");
            modelBuilder.Entity<TableB>().Property<Guid>(@"ID").HasColumnName(@"ID").IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<TableB>().Property(x => x.TITLE).HasColumnName(@"TITLE").IsRequired().ValueGeneratedNever().HasMaxLength(300);
            modelBuilder.Entity<TableB>().Property(x => x.PLOT).HasColumnName(@"PLOT").IsRequired().ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableB>().Property(x => x.DIRECTOR).HasColumnName(@"DIRECTOR").IsRequired().ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableB>().Property(x => x.WRITERS).HasColumnName(@"WRITERS").ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableB>().Property(x => x.STARS).HasColumnName(@"STARS").ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableB>().Property<DateTime>(@"DT_CREATION").HasColumnName(@"DT_CREATION").IsRequired().ValueGeneratedOnAdd().HasDefaultValueSql(@"getdate()");
            modelBuilder.Entity<TableB>().Property<DateTime>(@"DT_UPDATE").HasColumnName(@"DT_UPDATE").IsRequired().ValueGeneratedOnAddOrUpdate().HasDefaultValueSql(@"getdate()");
            modelBuilder.Entity<TableB>().Property(x => x.MISC).HasColumnName(@"MISC").ValueGeneratedNever().HasMaxLength(600);
            modelBuilder.Entity<TableB>().HasKey(@"ID", @"TITLE");
        }

        partial void CustomizeTableBMapping(ModelBuilder modelBuilder);

        #endregion

        private void RelationshipsMapping(ModelBuilder modelBuilder)
        {
        }

        partial void CustomizeMapping(ref ModelBuilder modelBuilder);

        public bool HasChanges()
        {
            return ChangeTracker.Entries().Any(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || e.State == Microsoft.EntityFrameworkCore.EntityState.Modified || e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);
        }

        partial void OnCreated();
    }
}
