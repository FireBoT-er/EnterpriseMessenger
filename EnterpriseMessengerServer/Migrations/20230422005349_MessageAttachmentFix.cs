using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class MessageAttachmentFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Messages_MessageId",
                table: "Attachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments");

            migrationBuilder.RenameTable(
                name: "Attachments",
                newName: "MessageAttachments");

            migrationBuilder.RenameIndex(
                name: "IX_Attachments_MessageId",
                table: "MessageAttachments",
                newName: "IX_MessageAttachments_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageAttachments",
                table: "MessageAttachments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageAttachments_Messages_MessageId",
                table: "MessageAttachments",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageAttachments_Messages_MessageId",
                table: "MessageAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageAttachments",
                table: "MessageAttachments");

            migrationBuilder.RenameTable(
                name: "MessageAttachments",
                newName: "Attachments");

            migrationBuilder.RenameIndex(
                name: "IX_MessageAttachments_MessageId",
                table: "Attachments",
                newName: "IX_Attachments_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Messages_MessageId",
                table: "Attachments",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id");
        }
    }
}
