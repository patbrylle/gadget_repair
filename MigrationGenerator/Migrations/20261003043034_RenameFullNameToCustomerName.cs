using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationGenerator.Migrations
{
    /// <inheritdoc />
    public partial class RenameFullNameToCustomerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Customers",
                newName: "CustomerName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "Customers",
                newName: "FullName");
        }
    }
}
