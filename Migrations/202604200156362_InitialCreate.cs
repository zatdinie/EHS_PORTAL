namespace EHS_PORTAL.Areas.CORD.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "EAS.AgreementFieldSignatures",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AgreementId = c.Int(nullable: false),
                        FieldId = c.Int(nullable: false),
                        SignatureData = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.Agreements", t => t.AgreementId, cascadeDelete: true)
                .ForeignKey("EAS.PdfSignatureFields", t => t.FieldId)
                .Index(t => t.AgreementId)
                .Index(t => t.FieldId);
            
            CreateTable(
                "EAS.Agreements",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DocumentId = c.Int(nullable: false),
                        VendorId = c.Int(nullable: false),
                        SignedAt = c.DateTime(nullable: false),
                        IpAddress = c.String(maxLength: 45),
                        SignatureData = c.String(),
                        PdfFilePath = c.String(maxLength: 500),
                        IsValid = c.Boolean(nullable: false),
                        BriefingId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.Vendors", t => t.VendorId, cascadeDelete: true)
                .ForeignKey("EAS.Briefings", t => t.BriefingId)
                .ForeignKey("EAS.EulaDocuments", t => t.DocumentId, cascadeDelete: true)
                .Index(t => t.DocumentId)
                .Index(t => t.VendorId)
                .Index(t => t.BriefingId);
            
            CreateTable(
                "EAS.Briefings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 200),
                        Location = c.String(nullable: false, maxLength: 200),
                        BriefingDate = c.DateTime(nullable: false),
                        Notes = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "EAS.BriefingAttendees",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BriefingId = c.Int(nullable: false),
                        VendorId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.Briefings", t => t.BriefingId, cascadeDelete: true)
                .ForeignKey("EAS.Vendors", t => t.VendorId, cascadeDelete: true)
                .Index(t => t.BriefingId)
                .Index(t => t.VendorId);
            
            CreateTable(
                "EAS.Vendors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CompanyName = c.String(nullable: false, maxLength: 100),
                        CreatedAt = c.DateTime(nullable: false),
                        CurrentPICId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.PICs", t => t.CurrentPICId)
                .Index(t => t.CurrentPICId);
            
            CreateTable(
                "EAS.PICs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 100),
                        Phone = c.String(nullable: false, maxLength: 20),
                        VendorId = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.Vendors", t => t.VendorId, cascadeDelete: true)
                .Index(t => t.VendorId);
            
            CreateTable(
                "EAS.OneTimeLinks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        VendorId = c.Int(nullable: false),
                        Token = c.String(nullable: false, maxLength: 100),
                        CreatedAt = c.DateTime(nullable: false),
                        ExpiresAt = c.DateTime(nullable: false),
                        UsedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.Vendors", t => t.VendorId, cascadeDelete: true)
                .Index(t => t.VendorId);
            
            CreateTable(
                "EAS.EulaDocuments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Version = c.String(nullable: false, maxLength: 40),
                        Title = c.String(nullable: false, maxLength: 200),
                        Content = c.String(),
                        FilePath = c.String(maxLength: 500),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "EAS.PdfSignatureFields",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DocumentId = c.Int(nullable: false),
                        Page = c.Int(nullable: false),
                        X = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Y = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Width = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Height = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Label = c.String(maxLength: 120),
                        IsRequired = c.Boolean(nullable: false),
                        SortOrder = c.Int(nullable: false),
                        FieldType = c.String(maxLength: 20),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.EulaDocuments", t => t.DocumentId)
                .Index(t => t.DocumentId);
            
            CreateTable(
                "EAS.Registrations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PICId = c.Int(nullable: false),
                        BriefingId = c.Int(nullable: false),
                        Session = c.String(maxLength: 20),
                        RegisteredAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("EAS.Briefings", t => t.BriefingId, cascadeDelete: true)
                .ForeignKey("EAS.PICs", t => t.PICId, cascadeDelete: true)
                .Index(t => t.PICId)
                .Index(t => t.BriefingId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("EAS.Registrations", "PICId", "EAS.PICs");
            DropForeignKey("EAS.Registrations", "BriefingId", "EAS.Briefings");
            DropForeignKey("EAS.AgreementFieldSignatures", "FieldId", "EAS.PdfSignatureFields");
            DropForeignKey("EAS.AgreementFieldSignatures", "AgreementId", "EAS.Agreements");
            DropForeignKey("EAS.PdfSignatureFields", "DocumentId", "EAS.EulaDocuments");
            DropForeignKey("EAS.Agreements", "DocumentId", "EAS.EulaDocuments");
            DropForeignKey("EAS.Agreements", "BriefingId", "EAS.Briefings");
            DropForeignKey("EAS.BriefingAttendees", "VendorId", "EAS.Vendors");
            DropForeignKey("EAS.OneTimeLinks", "VendorId", "EAS.Vendors");
            DropForeignKey("EAS.Vendors", "CurrentPICId", "EAS.PICs");
            DropForeignKey("EAS.PICs", "VendorId", "EAS.Vendors");
            DropForeignKey("EAS.Agreements", "VendorId", "EAS.Vendors");
            DropForeignKey("EAS.BriefingAttendees", "BriefingId", "EAS.Briefings");
            DropIndex("EAS.Registrations", new[] { "BriefingId" });
            DropIndex("EAS.Registrations", new[] { "PICId" });
            DropIndex("EAS.PdfSignatureFields", new[] { "DocumentId" });
            DropIndex("EAS.OneTimeLinks", new[] { "VendorId" });
            DropIndex("EAS.PICs", new[] { "VendorId" });
            DropIndex("EAS.Vendors", new[] { "CurrentPICId" });
            DropIndex("EAS.BriefingAttendees", new[] { "VendorId" });
            DropIndex("EAS.BriefingAttendees", new[] { "BriefingId" });
            DropIndex("EAS.Agreements", new[] { "BriefingId" });
            DropIndex("EAS.Agreements", new[] { "VendorId" });
            DropIndex("EAS.Agreements", new[] { "DocumentId" });
            DropIndex("EAS.AgreementFieldSignatures", new[] { "FieldId" });
            DropIndex("EAS.AgreementFieldSignatures", new[] { "AgreementId" });
            DropTable("EAS.Registrations");
            DropTable("EAS.PdfSignatureFields");
            DropTable("EAS.EulaDocuments");
            DropTable("EAS.OneTimeLinks");
            DropTable("EAS.PICs");
            DropTable("EAS.Vendors");
            DropTable("EAS.BriefingAttendees");
            DropTable("EAS.Briefings");
            DropTable("EAS.Agreements");
            DropTable("EAS.AgreementFieldSignatures");
        }
    }
}
