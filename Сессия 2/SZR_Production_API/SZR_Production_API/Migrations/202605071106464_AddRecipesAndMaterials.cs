namespace SZR_Production_API.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRecipesAndMaterials : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BatchStepExecutions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BatchId = c.Int(nullable: false),
                        StepId = c.Int(nullable: false),
                        StepNumber = c.Int(nullable: false),
                        Status = c.String(),
                        StartedAt = c.DateTime(),
                        FinishedAt = c.DateTime(),
                        StartedBy = c.Int(),
                        FinishedBy = c.Int(),
                        ActualParams = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.DeviationEvents",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BatchId = c.Int(nullable: false),
                        StepExecutionId = c.Int(),
                        EventType = c.String(),
                        ParameterName = c.String(),
                        PlannedValue = c.String(),
                        ActualValue = c.String(),
                        Severity = c.String(),
                        Description = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        CreatedBy = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ProductionBatches",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BatchNumber = c.String(nullable: false, maxLength: 50),
                        OrderId = c.Int(),
                        ProductId = c.Int(nullable: false),
                        RecipeId = c.Int(nullable: false),
                        TechCardId = c.Int(nullable: false),
                        Line = c.String(),
                        Status = c.String(),
                        LabStatus = c.String(),
                        StartedAt = c.DateTime(),
                        FinishedAt = c.DateTime(),
                        CreatedBy = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 50),
                        Name = c.String(nullable: false, maxLength: 200),
                        ProductType = c.String(),
                        Form = c.String(),
                        Status = c.String(),
                        CreatedBy = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.RawMaterials",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 50),
                        Name = c.String(nullable: false, maxLength: 200),
                        Category = c.String(maxLength: 50),
                        Unit = c.String(nullable: false, maxLength: 20),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.RecipeComponents",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        RecipeId = c.Int(nullable: false),
                        RawMaterialId = c.Int(nullable: false),
                        Percentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ToleranceMin = c.Decimal(precision: 18, scale: 2),
                        ToleranceMax = c.Decimal(precision: 18, scale: 2),
                        LoadOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.RawMaterials", t => t.RawMaterialId, cascadeDelete: true)
                .ForeignKey("dbo.Recipes", t => t.RecipeId, cascadeDelete: true)
                .Index(t => t.RecipeId)
                .Index(t => t.RawMaterialId);
            
            CreateTable(
                "dbo.Recipes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProductId = c.Int(nullable: false),
                        Version = c.String(nullable: false, maxLength: 10),
                        Status = c.String(nullable: false, maxLength: 50),
                        ApprovedAt = c.DateTime(),
                        ApprovedBy = c.Int(),
                        CreatedBy = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Login = c.String(nullable: false, maxLength: 50),
                        PasswordHash = c.String(nullable: false),
                        FullName = c.String(nullable: false, maxLength: 150),
                        RoleId = c.Int(nullable: false),
                        Department = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Recipes", "ProductId", "dbo.Products");
            DropForeignKey("dbo.RecipeComponents", "RecipeId", "dbo.Recipes");
            DropForeignKey("dbo.RecipeComponents", "RawMaterialId", "dbo.RawMaterials");
            DropIndex("dbo.Recipes", new[] { "ProductId" });
            DropIndex("dbo.RecipeComponents", new[] { "RawMaterialId" });
            DropIndex("dbo.RecipeComponents", new[] { "RecipeId" });
            DropTable("dbo.Users");
            DropTable("dbo.Roles");
            DropTable("dbo.Recipes");
            DropTable("dbo.RecipeComponents");
            DropTable("dbo.RawMaterials");
            DropTable("dbo.Products");
            DropTable("dbo.ProductionBatches");
            DropTable("dbo.DeviationEvents");
            DropTable("dbo.BatchStepExecutions");
        }
    }
}
