using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserGroupChatRelate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserGroupChat");
        }
    }
}
