using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSloopedToFactoryRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSlooped",
                table: "FactoryRecipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSlooped",
                table: "FactoryRecipes");
        }
    }
}
