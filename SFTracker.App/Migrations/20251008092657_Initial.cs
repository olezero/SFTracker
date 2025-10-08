using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFTracker.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Tier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SinkPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Tier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TimeSeconds = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsAlternate = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sorting = table.Column<double>(type: "REAL", nullable: false),
                    NetworkId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factories_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecipeInputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeInputs_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeInputs_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeOutputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeOutputs_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeOutputs_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactoryInputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartId = table.Column<int>(type: "INTEGER", nullable: false),
                    FactoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountPerMinute = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactoryInputs_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactoryInputs_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactoryOutputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartId = table.Column<int>(type: "INTEGER", nullable: false),
                    FactoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountPerMinute = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactoryOutputs_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactoryOutputs_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactoryRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Multiplier = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactoryRecipes_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactoryRecipes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactoryRecipeInputOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactoryRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeInputId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryRecipeInputOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactoryRecipeInputOverrides_FactoryRecipes_FactoryRecipeId",
                        column: x => x.FactoryRecipeId,
                        principalTable: "FactoryRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactoryRecipeInputOverrides_RecipeInputs_RecipeInputId",
                        column: x => x.RecipeInputId,
                        principalTable: "RecipeInputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactoryRecipeOutputOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactoryRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeOutputId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryRecipeOutputOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactoryRecipeOutputOverrides_FactoryRecipes_FactoryRecipeId",
                        column: x => x.FactoryRecipeId,
                        principalTable: "FactoryRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactoryRecipeOutputOverrides_RecipeOutputs_RecipeOutputId",
                        column: x => x.RecipeOutputId,
                        principalTable: "RecipeOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Factories_Name",
                table: "Factories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factories_NetworkId",
                table: "Factories",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Factories_Sorting",
                table: "Factories",
                column: "Sorting");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryInputs_FactoryId",
                table: "FactoryInputs",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryInputs_PartId",
                table: "FactoryInputs",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryOutputs_FactoryId",
                table: "FactoryOutputs",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryOutputs_PartId",
                table: "FactoryOutputs",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipeInputOverrides_FactoryRecipeId_RecipeInputId",
                table: "FactoryRecipeInputOverrides",
                columns: new[] { "FactoryRecipeId", "RecipeInputId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipeInputOverrides_RecipeInputId",
                table: "FactoryRecipeInputOverrides",
                column: "RecipeInputId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipeOutputOverrides_FactoryRecipeId_RecipeOutputId",
                table: "FactoryRecipeOutputOverrides",
                columns: new[] { "FactoryRecipeId", "RecipeOutputId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipeOutputOverrides_RecipeOutputId",
                table: "FactoryRecipeOutputOverrides",
                column: "RecipeOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipes_FactoryId",
                table: "FactoryRecipes",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipes_FactoryId_RecipeId",
                table: "FactoryRecipes",
                columns: new[] { "FactoryId", "RecipeId" });

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipes_IsActive",
                table: "FactoryRecipes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FactoryRecipes_RecipeId",
                table: "FactoryRecipes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Networks_Name",
                table: "Networks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parts_Name",
                table: "Parts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeInputs_PartId",
                table: "RecipeInputs",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeInputs_RecipeId_PartId",
                table: "RecipeInputs",
                columns: new[] { "RecipeId", "PartId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeOutputs_PartId",
                table: "RecipeOutputs",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeOutputs_RecipeId_PartId",
                table: "RecipeOutputs",
                columns: new[] { "RecipeId", "PartId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Name",
                table: "Recipes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactoryInputs");

            migrationBuilder.DropTable(
                name: "FactoryOutputs");

            migrationBuilder.DropTable(
                name: "FactoryRecipeInputOverrides");

            migrationBuilder.DropTable(
                name: "FactoryRecipeOutputOverrides");

            migrationBuilder.DropTable(
                name: "RecipeInputs");

            migrationBuilder.DropTable(
                name: "FactoryRecipes");

            migrationBuilder.DropTable(
                name: "RecipeOutputs");

            migrationBuilder.DropTable(
                name: "Factories");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Networks");
        }
    }
}
