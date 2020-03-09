using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgendaOnline.Repository.Migrations
{
    public partial class CriandoTabelaAdmeUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abertura",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Duracao",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Fechamento",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "MarketSegment",
                table: "AspNetUsers",
                newName: "Celular");

            migrationBuilder.AddColumn<int>(
                name: "AdmId",
                table: "AspNetUserRoles",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Adms",
                columns: table => new
                {
                    UserName = table.Column<string>(nullable: true),
                    NormalizedUserName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    NormalizedEmail = table.Column<string>(nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FullName = table.Column<string>(type: "nvarchar(150)", nullable: true),
                    Company = table.Column<string>(nullable: true),
                    MarketSegment = table.Column<string>(nullable: true),
                    ImagemPerfil = table.Column<string>(nullable: true),
                    Cidade = table.Column<string>(nullable: true),
                    Abertura = table.Column<TimeSpan>(nullable: false),
                    Fechamento = table.Column<TimeSpan>(nullable: false),
                    Duracao = table.Column<TimeSpan>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_AdmId",
                table: "AspNetUserRoles",
                column: "AdmId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Adms_AdmId",
                table: "AspNetUserRoles",
                column: "AdmId",
                principalTable: "Adms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Adms_AdmId",
                table: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "Adms");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_AdmId",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "AdmId",
                table: "AspNetUserRoles");

            migrationBuilder.RenameColumn(
                name: "Celular",
                table: "AspNetUsers",
                newName: "MarketSegment");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Abertura",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duracao",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Fechamento",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
