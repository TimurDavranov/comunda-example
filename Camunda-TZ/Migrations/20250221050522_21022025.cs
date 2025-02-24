using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Camunda_TZ.Migrations
{
    /// <inheritdoc />
    public partial class _21022025 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assignee",
                table: "Tickets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assignee",
                table: "Tickets");
        }
    }
}
