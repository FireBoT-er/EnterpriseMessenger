using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseMessengerServer.Migrations
{
    /// <inheritdoc />
    public partial class MessageAttachmentAdditionalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalData",
                table: "MessageAttachments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalData",
                table: "MessageAttachments");
        }
    }
}
