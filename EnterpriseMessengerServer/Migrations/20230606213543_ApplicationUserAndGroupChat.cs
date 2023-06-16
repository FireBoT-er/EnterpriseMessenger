using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserAndGroupChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserGroupChat");

            migrationBuilder.CreateTable(
                name: "ApplicationUserAndGroupChat",
                columns: table => new
                {
                    ParticipantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupChatId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReadMessageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserAndGroupChat", x => new { x.ParticipantId, x.GroupChatId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserAndGroupChat_AspNetUsers_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserAndGroupChat_GroupChats_GroupChatId",
                        column: x => x.GroupChatId,
                        principalTable: "GroupChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserAndGroupChat_Messages_LastReadMessageId",
                        column: x => x.LastReadMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAndGroupChat_GroupChatId",
                table: "ApplicationUserAndGroupChat",
                column: "GroupChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAndGroupChat_LastReadMessageId",
                table: "ApplicationUserAndGroupChat",
                column: "LastReadMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserAndGroupChat");

            migrationBuilder.CreateTable(
                name: "ApplicationUserGroupChat",
                columns: table => new
                {
                    GroupChatsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParticipantsId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserGroupChat", x => new { x.GroupChatsId, x.ParticipantsId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserGroupChat_AspNetUsers_ParticipantsId",
                        column: x => x.ParticipantsId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserGroupChat_GroupChats_GroupChatsId",
                        column: x => x.GroupChatsId,
                        principalTable: "GroupChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserGroupChat_ParticipantsId",
                table: "ApplicationUserGroupChat",
                column: "ParticipantsId");
        }
    }
}
