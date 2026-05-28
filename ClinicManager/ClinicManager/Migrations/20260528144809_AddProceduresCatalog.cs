using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Migrations
{
    /// <inheritdoc />
    public partial class AddProceduresCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcedureId",
                table: "ProceduresPerformed",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Procedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procedures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProceduresPerformed_ProcedureId",
                table: "ProceduresPerformed",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_Name",
                table: "Procedures",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProceduresPerformed_Procedures_ProcedureId",
                table: "ProceduresPerformed",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProceduresPerformed_Procedures_ProcedureId",
                table: "ProceduresPerformed");

            migrationBuilder.DropTable(
                name: "Procedures");

            migrationBuilder.DropIndex(
                name: "IX_ProceduresPerformed_ProcedureId",
                table: "ProceduresPerformed");

            migrationBuilder.DropColumn(
                name: "ProcedureId",
                table: "ProceduresPerformed");
        }
    }
}
