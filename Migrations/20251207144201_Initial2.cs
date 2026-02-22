using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEvenements.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commentaires_AspNetUsers_UserId",
                table: "Commentaires");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentaires_Events_EventId",
                table: "Commentaires");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Commentaires",
                table: "Commentaires");

            migrationBuilder.RenameTable(
                name: "Commentaires",
                newName: "Comments");

            migrationBuilder.RenameColumn(
                name: "Contenu",
                table: "Comments",
                newName: "Texte");

            migrationBuilder.RenameIndex(
                name: "IX_Commentaires_EventId",
                table: "Comments",
                newName: "IX_Comments_EventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Events_EventId",
                table: "Comments",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Events_EventId",
                table: "Comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "Commentaires");

            migrationBuilder.RenameColumn(
                name: "Texte",
                table: "Commentaires",
                newName: "Contenu");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_EventId",
                table: "Commentaires",
                newName: "IX_Commentaires_EventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Commentaires",
                table: "Commentaires",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Commentaires_AspNetUsers_UserId",
                table: "Commentaires",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentaires_Events_EventId",
                table: "Commentaires",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
