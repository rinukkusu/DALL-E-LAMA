using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DALL_E_LAMA.Data.Migrations
{
    public partial class AddTaskIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "Generations",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Generations");
        }
    }
}
