using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class GroupChatsAddAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "GroupChats",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupChats_AuthorId",
                table: "GroupChats",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupChats_AspNetUsers_AuthorId",
                table: "GroupChats",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupChats_AspNetUsers_AuthorId",
                table: "GroupChats");

            migrationBuilder.DropIndex(
                name: "IX_GroupChats_AuthorId",
                table: "GroupChats");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "GroupChats");
        }
    }
}
