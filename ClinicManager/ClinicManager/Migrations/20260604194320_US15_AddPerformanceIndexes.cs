using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Migrations
{
    /// <inheritdoc />
    public partial class US15_AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_PatientId",
                table: "Visits");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PatientId_ScheduledAt",
                table: "Visits",
                columns: new[] { "PatientId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_InsuranceNumber",
                table: "Patients",
                column: "InsuranceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecordAccessLogs_UserId",
                table: "MedicalRecordAccessLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_PatientId_ScheduledAt",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Patients_InsuranceNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecordAccessLogs_UserId",
                table: "MedicalRecordAccessLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PatientId",
                table: "Visits",
                column: "PatientId");
        }
    }
}
