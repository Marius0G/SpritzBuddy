using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpritzBuddy.Migrations
{
    /// <inheritdoc />
    public partial class GroupsEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupId",
                table: "Events",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Groups_GroupId",
                table: "Events",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Groups_GroupId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_GroupId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Events");
        }
    }
}
