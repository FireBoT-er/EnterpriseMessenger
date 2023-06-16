using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class MessageNamesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_UserId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_GroupChats_ChatId",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Messages",
                newName: "ReceiverUserId");

            migrationBuilder.RenameColumn(
                name: "ChatId",
                table: "Messages",
                newName: "ReceiverChatId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_UserId",
                table: "Messages",
                newName: "IX_Messages_ReceiverUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ChatId",
                table: "Messages",
                newName: "IX_Messages_ReceiverChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_AspNetUsers_ReceiverUserId",
                table: "Messages",
                column: "ReceiverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_GroupChats_ReceiverChatId",
                table: "Messages",
                column: "ReceiverChatId",
                principalTable: "GroupChats",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_ReceiverUserId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_GroupChats_ReceiverChatId",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "ReceiverUserId",
                table: "Messages",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ReceiverChatId",
                table: "Messages",
                newName: "ChatId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ReceiverUserId",
                table: "Messages",
                newName: "IX_Messages_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ReceiverChatId",
                table: "Messages",
                newName: "IX_Messages_ChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_AspNetUsers_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_GroupChats_ChatId",
                table: "Messages",
                column: "ChatId",
                principalTable: "GroupChats",
                principalColumn: "Id");
        }
    }
}
