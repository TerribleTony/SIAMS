using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIAMS.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdminRequestedToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdminRequested",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdminRequested",
                table: "Users");
        }
    }
}
