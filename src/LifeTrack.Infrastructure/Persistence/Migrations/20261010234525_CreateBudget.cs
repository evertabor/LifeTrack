using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeTrack.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CreateBudget : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Budgets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Year = table.Column<int>(type: "int", nullable: false),
                Month = table.Column<int>(type: "int", nullable: false),
                Limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Budgets", x => x.Id);
                table.ForeignKey(
                    name: "FK_Budgets_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Budgets_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_CategoryId",
            table: "Budgets",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_UserId",
            table: "Budgets",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_UserId_CategoryId_Year_Month",
            table: "Budgets",
            columns: new[] { "UserId", "CategoryId", "Year", "Month" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Budgets");
    }
}
