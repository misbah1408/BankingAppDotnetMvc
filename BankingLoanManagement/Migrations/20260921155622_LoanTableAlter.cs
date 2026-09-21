using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingLoanManagement.Migrations
{
    /// <inheritdoc />
    public partial class LoanTableAlter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditScore",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DocumentPath",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "RiskRating",
                table: "Loans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditScore",
                table: "Loans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DocumentPath",
                table: "Loans",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskRating",
                table: "Loans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
