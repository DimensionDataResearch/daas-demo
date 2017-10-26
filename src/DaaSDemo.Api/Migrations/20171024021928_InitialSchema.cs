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
                name: "DatabaseInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DatabasePassword = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatabaseServerId = table.Column<int>(type: "int", nullable: false),
                    DatabaseUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseInstance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DatabaseServerId = table.Column<int>(type: "int", nullable: true),
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
                    AdminPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IngressIP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IngressPort = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                name: "IX_Tenant_DatabaseServerId",
                table: "Tenant",
                column: "DatabaseServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Name",
                table: "Tenant",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_DatabaseInstance_DatabaseServer_DatabaseServerId",
                table: "DatabaseInstance",
                column: "DatabaseServerId",
                principalTable: "DatabaseServer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenant_DatabaseServer_DatabaseServerId",
                table: "Tenant",
                column: "DatabaseServerId",
                principalTable: "DatabaseServer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenant_DatabaseServer_DatabaseServerId",
                table: "Tenant");

            migrationBuilder.DropTable(
                name: "DatabaseInstance");

            migrationBuilder.DropTable(
                name: "DatabaseServer");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}
