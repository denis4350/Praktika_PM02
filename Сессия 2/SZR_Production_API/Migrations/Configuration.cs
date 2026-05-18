namespace SZR_Production_API.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using SZR_Production_API.Models;  // ← замени на правильный namespace

    internal sealed class Configuration : DbMigrationsConfiguration<SZR_ProductionEntities2>  // ← замени на EDMX контекст
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(SZR_ProductionEntities2 context)  // ← замени
        {
            // Seed data
        }
    }
}