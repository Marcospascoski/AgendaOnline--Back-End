using Microsoft.EntityFrameworkCore.Migrations;

namespace AgendaOnline.Repository.Migrations
{
    public partial class DeletarAtributoStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Agendas");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Agendas",
                nullable: false,
                defaultValue: 0);
        }
    }
}
