using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationGenerator.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gadgets");

            migrationBuilder.RenameColumn(
                name: "GadgetId",
                table: "Repairs",
                newName: "CustomerId");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Repairs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "Repairs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Repairs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProblemDescription",
                table: "Repairs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Repairs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_CustomerId",
                table: "Repairs",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Customers_CustomerId",
                table: "Repairs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Customers_CustomerId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_CustomerId",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "ProblemDescription",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Repairs");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Repairs",
                newName: "GadgetId");

            migrationBuilder.CreateTable(
                name: "Gadgets",
                columns: table => new
                {
                    GadgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Brand = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProblemDescription = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gadgets", x => x.GadgetId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
