namespace EHS_PORTAL.Areas.CORD.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "CORD.AgreementFieldSignatures",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AgreementId = c.Int(nullable: false),
                        FieldId = c.Int(nullable: false),
                        SignatureData = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("CORD.Agreements", t => t.AgreementId, cascadeDelete: true)
                .ForeignKey("CORD.PdfSignatureFields", t => t.FieldId)
                .Index(t => t.AgreementId)
                .Index(t => t.FieldId);
            
            CreateTable(
                "CORD.Agreements",
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
                .ForeignKey("CORD.Vendors", t => t.VendorId, cascadeDelete: true)
                .ForeignKey("CORD.Briefings", t => t.BriefingId)
                .ForeignKey("CORD.EulaDocuments", t => t.DocumentId, cascadeDelete: true)
                .Index(t => t.DocumentId)
                .Index(t => t.VendorId)
                .Index(t => t.BriefingId);
            
            CreateTable(
                "CORD.Briefings",
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
                "CORD.BriefingAttendees",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BriefingId = c.Int(nullable: false),
                        VendorId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("CORD.Briefings", t => t.BriefingId, cascadeDelete: true)
                .ForeignKey("CORD.Vendors", t => t.VendorId, cascadeDelete: true)
                .Index(t => t.BriefingId)
                .Index(t => t.VendorId);
            
            CreateTable(
                "CORD.Vendors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CompanyName = c.String(nullable: false, maxLength: 100),
                        CreatedAt = c.DateTime(nullable: false),
                        CurrentPICId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("CORD.PICs", t => t.CurrentPICId)
                .Index(t => t.CurrentPICId);
            
            CreateTable(
                "CORD.PICs",
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
                .ForeignKey("CORD.Vendors", t => t.VendorId, cascadeDelete: true)
                .Index(t => t.VendorId);
            
            CreateTable(
                "CORD.OneTimeLinks",
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
                .ForeignKey("CORD.Vendors", t => t.VendorId, cascadeDelete: true)
                .Index(t => t.VendorId);
            
            CreateTable(
                "CORD.EulaDocuments",
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
                "CORD.PdfSignatureFields",
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
                .ForeignKey("CORD.EulaDocuments", t => t.DocumentId)
                .Index(t => t.DocumentId);
            
            CreateTable(
                "CORD.Registrations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PICId = c.Int(nullable: false),
                        BriefingId = c.Int(nullable: false),
                        Session = c.String(maxLength: 20),
                        RegisteredAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("CORD.Briefings", t => t.BriefingId, cascadeDelete: true)
                .ForeignKey("CORD.PICs", t => t.PICId, cascadeDelete: true)
                .Index(t => t.PICId)
                .Index(t => t.BriefingId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("CORD.Registrations", "PICId", "CORD.PICs");
            DropForeignKey("CORD.Registrations", "BriefingId", "CORD.Briefings");
            DropForeignKey("CORD.AgreementFieldSignatures", "FieldId", "CORD.PdfSignatureFields");
            DropForeignKey("CORD.AgreementFieldSignatures", "AgreementId", "CORD.Agreements");
            DropForeignKey("CORD.PdfSignatureFields", "DocumentId", "CORD.EulaDocuments");
            DropForeignKey("CORD.Agreements", "DocumentId", "CORD.EulaDocuments");
            DropForeignKey("CORD.Agreements", "BriefingId", "CORD.Briefings");
            DropForeignKey("CORD.BriefingAttendees", "VendorId", "CORD.Vendors");
            DropForeignKey("CORD.OneTimeLinks", "VendorId", "CORD.Vendors");
            DropForeignKey("CORD.Vendors", "CurrentPICId", "CORD.PICs");
            DropForeignKey("CORD.PICs", "VendorId", "CORD.Vendors");
            DropForeignKey("CORD.Agreements", "VendorId", "CORD.Vendors");
            DropForeignKey("CORD.BriefingAttendees", "BriefingId", "CORD.Briefings");
            DropIndex("CORD.Registrations", new[] { "BriefingId" });
            DropIndex("CORD.Registrations", new[] { "PICId" });
            DropIndex("CORD.PdfSignatureFields", new[] { "DocumentId" });
            DropIndex("CORD.OneTimeLinks", new[] { "VendorId" });
            DropIndex("CORD.PICs", new[] { "VendorId" });
            DropIndex("CORD.Vendors", new[] { "CurrentPICId" });
            DropIndex("CORD.BriefingAttendees", new[] { "VendorId" });
            DropIndex("CORD.BriefingAttendees", new[] { "BriefingId" });
            DropIndex("CORD.Agreements", new[] { "BriefingId" });
            DropIndex("CORD.Agreements", new[] { "VendorId" });
            DropIndex("CORD.Agreements", new[] { "DocumentId" });
            DropIndex("CORD.AgreementFieldSignatures", new[] { "FieldId" });
            DropIndex("CORD.AgreementFieldSignatures", new[] { "AgreementId" });
            DropTable("CORD.Registrations");
            DropTable("CORD.PdfSignatureFields");
            DropTable("CORD.EulaDocuments");
            DropTable("CORD.OneTimeLinks");
            DropTable("CORD.PICs");
            DropTable("CORD.Vendors");
            DropTable("CORD.BriefingAttendees");
            DropTable("CORD.Briefings");
            DropTable("CORD.Agreements");
            DropTable("CORD.AgreementFieldSignatures");
        }
    }
}
