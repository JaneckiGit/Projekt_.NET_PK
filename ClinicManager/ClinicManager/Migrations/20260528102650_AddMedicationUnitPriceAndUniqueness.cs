using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationUnitPriceAndUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medications_Name",
                table: "Medications");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Medications",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medications_Name",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Medications");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name");
        }
    }
}
