using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Patients_AdmissionStatus",
                table: "Patients",
                column: "AdmissionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicalStatus",
                table: "Patients",
                column: "ClinicalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Species",
                table: "Patients",
                column: "Species");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_AdmissionStatus",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_ClinicalStatus",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_Species",
                table: "Patients");
        }
    }
}
