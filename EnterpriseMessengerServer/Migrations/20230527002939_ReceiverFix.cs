using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class ReceiverFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecevierUserId",
                table: "Messages",
                newName: "ReceiverUserId");

            migrationBuilder.RenameColumn(
                name: "RecevierChatId",
                table: "Messages",
                newName: "ReceiverChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceiverUserId",
                table: "Messages",
                newName: "RecevierUserId");

            migrationBuilder.RenameColumn(
                name: "ReceiverChatId",
                table: "Messages",
                newName: "RecevierChatId");
        }
    }
}
