using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Api.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseServer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Action = table.Column<int>(type: "int", nullable: false),
                    AdminPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    PublicFQDN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublicPort = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseServer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseServer_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Action = table.Column<int>(type: "int", nullable: false),
                    DatabasePassword = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatabaseServerId = table.Column<int>(type: "int", nullable: false),
                    DatabaseUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseInstance_DatabaseServer_DatabaseServerId",
                        column: x => x.DatabaseServerId,
                        principalTable: "DatabaseServer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseInstance_DatabaseServerId",
                table: "DatabaseInstance",
                column: "DatabaseServerId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseInstance_Name",
                table: "DatabaseInstance",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseServer_Name",
                table: "DatabaseServer",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseServer_TenantId",
                table: "DatabaseServer",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Name",
                table: "Tenant",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatabaseInstance");

            migrationBuilder.DropTable(
                name: "DatabaseServer");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}
