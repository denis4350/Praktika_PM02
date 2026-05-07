using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using SZR_Production_API.Models;

namespace SZR_Production_API.Data
{
    public class SZR_ProductionEntities1 : DbContext
    {
        public SZR_ProductionEntities1() : base("name=DefaultConnection")
        {
            // ОТКЛЮЧАЕМ ЛЕНИВУЮ ЗАГРУЗКУ (это важно!)
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;
            this.Configuration.AutoDetectChangesEnabled = false;
        }

        // ТОЛЬКО ТЕ ТАБЛИЦЫ, КОТОРЫЕ РЕАЛЬНО СУЩЕСТВУЮТ В БД
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Products> Products { get; set; }        // ← Products (с S)
        public DbSet<RawMaterials> RawMaterials { get; set; } // ← RawMaterials (с S)
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Recipes> Recipes { get; set; }           // ← добавить
        public DbSet<RecipeComponents> RecipeComponents { get; set; } // ← добавить
        public DbSet<TechCards> TechCards { get; set; }       // ← добавить
        public DbSet<TechSteps> TechSteps { get; set; }       // ← добавить
        public DbSet<ProductionOrders> ProductionOrders { get; set; } // ← добавить
        public DbSet<ProductionBatches> ProductionBatches { get; set; } // ← добавить
        public DbSet<BatchStepExecutions> BatchStepExecutions { get; set; }
        public DbSet<DeviationEvents> DeviationEvents { get; set; } // ← добавить
        public DbSet<RawMaterialBatches> RawMaterialBatches { get; set; } // ← добавить
        public DbSet<LabTests> LabTests { get; set; }         // ← добавить
        public DbSet<LabTestParameters> LabTestParameters { get; set; } // ← добавить
        public DbSet<Notifications> Notifications { get; set; } // ← добавить

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Явно указываем имена таблиц в БД
            modelBuilder.Entity<Users>().ToTable("Users");
            modelBuilder.Entity<Roles>().ToTable("Roles");
            modelBuilder.Entity<Products>().ToTable("Products");
            modelBuilder.Entity<RawMaterials>().ToTable("RawMaterials");
            modelBuilder.Entity<Equipment>().ToTable("Equipment");
            modelBuilder.Entity<Recipes>().ToTable("Recipes");
            modelBuilder.Entity<RecipeComponents>().ToTable("RecipeComponents");
            modelBuilder.Entity<TechCards>().ToTable("TechCards");
            modelBuilder.Entity<TechSteps>().ToTable("TechSteps");
            modelBuilder.Entity<ProductionOrders>().ToTable("ProductionOrders");
            modelBuilder.Entity<ProductionBatches>().ToTable("ProductionBatches");
            modelBuilder.Entity<BatchStepExecutions>().ToTable("BatchStepExecutions");
            modelBuilder.Entity<DeviationEvents>().ToTable("DeviationEvents");
            modelBuilder.Entity<RawMaterialBatches>().ToTable("RawMaterialBatches");
            modelBuilder.Entity<LabTests>().ToTable("LabTests");
            modelBuilder.Entity<LabTestParameters>().ToTable("LabTestParameters");
            modelBuilder.Entity<Notifications>().ToTable("Notifications");
        }
    }
}