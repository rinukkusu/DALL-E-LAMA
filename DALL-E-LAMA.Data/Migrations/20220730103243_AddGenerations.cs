using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DALL_E_LAMA.Data.Migrations
{
    public partial class AddGenerations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Generations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Generations_Id",
                table: "Generations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Generations_MessageId",
                table: "Generations",
                column: "MessageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Generations");
        }
    }
}
