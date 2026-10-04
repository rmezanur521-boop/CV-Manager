using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmSyncRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmSyncRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MarketingOptIn = table.Column<bool>(type: "bit", nullable: false),
                    SalesforceAccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SalesforceContactId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSyncRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmSyncRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmSyncRecords_UserId",
                table: "CrmSyncRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmSyncRecords");
        }
    }
}
