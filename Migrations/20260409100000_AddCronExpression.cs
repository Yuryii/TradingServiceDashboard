using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingServiceDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddCronExpression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "NotificationConfigs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "NotificationConfigs");
        }
    }
}
