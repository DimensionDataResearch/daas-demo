using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Api.Migrations
{
    public partial class AddressMappings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPAddressMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ExternalIP = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InternalIP = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPAddressMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IPAddressMappings_ExternalIP",
                table: "IPAddressMappings",
                column: "ExternalIP");

            migrationBuilder.CreateIndex(
                name: "IX_IPAddressMappings_InternalIP",
                table: "IPAddressMappings",
                column: "InternalIP");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPAddressMappings");
        }
    }
}
