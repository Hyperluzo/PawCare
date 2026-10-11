using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "Treatments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Medication",
                table: "Treatments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Treatments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Treatment",
                table: "Treatments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "Treatments");

            migrationBuilder.DropColumn(
                name: "Medication",
                table: "Treatments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Treatments");

            migrationBuilder.DropColumn(
                name: "Treatment",
                table: "Treatments");
        }
    }
}
