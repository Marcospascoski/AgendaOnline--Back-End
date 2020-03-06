using Microsoft.EntityFrameworkCore.Migrations;

namespace AgendaOnline.Repository.Migrations
{
    public partial class AdcionandoPropriedadeCidade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "AspNetUsers");
        }
    }
}
