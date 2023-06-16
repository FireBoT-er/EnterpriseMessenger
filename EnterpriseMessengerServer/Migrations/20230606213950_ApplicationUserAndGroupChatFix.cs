using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserAndGroupChatFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserAndGroupChat_Messages_LastReadMessageId",
                table: "ApplicationUserAndGroupChat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationUserAndGroupChat",
                table: "ApplicationUserAndGroupChat");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserAndGroupChat_GroupChatId",
                table: "ApplicationUserAndGroupChat");

            migrationBuilder.AlterColumn<long>(
                name: "LastReadMessageId",
                table: "ApplicationUserAndGroupChat",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationUserAndGroupChat",
                table: "ApplicationUserAndGroupChat",
                columns: new[] { "GroupChatId", "ParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAndGroupChat_ParticipantId",
                table: "ApplicationUserAndGroupChat",
                column: "ParticipantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserAndGroupChat_Messages_LastReadMessageId",
                table: "ApplicationUserAndGroupChat",
                column: "LastReadMessageId",
                principalTable: "Messages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserAndGroupChat_Messages_LastReadMessageId",
                table: "ApplicationUserAndGroupChat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationUserAndGroupChat",
                table: "ApplicationUserAndGroupChat");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserAndGroupChat_ParticipantId",
                table: "ApplicationUserAndGroupChat");

            migrationBuilder.AlterColumn<long>(
                name: "LastReadMessageId",
                table: "ApplicationUserAndGroupChat",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationUserAndGroupChat",
                table: "ApplicationUserAndGroupChat",
                columns: new[] { "ParticipantId", "GroupChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAndGroupChat_GroupChatId",
                table: "ApplicationUserAndGroupChat",
                column: "GroupChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserAndGroupChat_Messages_LastReadMessageId",
                table: "ApplicationUserAndGroupChat",
                column: "LastReadMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
