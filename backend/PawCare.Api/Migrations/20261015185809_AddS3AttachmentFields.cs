using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddS3AttachmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "Treatments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "Treatments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Patients",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "Treatments");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Treatments");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Patients");
        }
    }
}
