using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingLoanManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "Loans",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "Loans");
        }
    }
}
