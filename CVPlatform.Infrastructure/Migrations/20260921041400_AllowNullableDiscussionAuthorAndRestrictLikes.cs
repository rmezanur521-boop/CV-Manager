using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullableDiscussionAuthorAndRestrictLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_AspNetUsers_AuthorId",
                table: "DiscussionPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionAttribute_Attributes_AttributeId",
                table: "PositionAttribute");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionAttribute_Positions_PositionId",
                table: "PositionAttribute");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PositionAttribute",
                table: "PositionAttribute");

            migrationBuilder.RenameTable(
                name: "PositionAttribute",
                newName: "PositionAttributes");

            migrationBuilder.RenameIndex(
                name: "IX_PositionAttribute_AttributeId",
                table: "PositionAttributes",
                newName: "IX_PositionAttributes_AttributeId");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                table: "DiscussionPosts",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PositionAttributes",
                table: "PositionAttributes",
                columns: new[] { "PositionId", "AttributeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_AspNetUsers_AuthorId",
                table: "DiscussionPosts",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionAttributes_Attributes_AttributeId",
                table: "PositionAttributes",
                column: "AttributeId",
                principalTable: "Attributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionAttributes_Positions_PositionId",
                table: "PositionAttributes",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_AspNetUsers_AuthorId",
                table: "DiscussionPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionAttributes_Attributes_AttributeId",
                table: "PositionAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionAttributes_Positions_PositionId",
                table: "PositionAttributes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PositionAttributes",
                table: "PositionAttributes");

            migrationBuilder.RenameTable(
                name: "PositionAttributes",
                newName: "PositionAttribute");

            migrationBuilder.RenameIndex(
                name: "IX_PositionAttributes_AttributeId",
                table: "PositionAttribute",
                newName: "IX_PositionAttribute_AttributeId");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                table: "DiscussionPosts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PositionAttribute",
                table: "PositionAttribute",
                columns: new[] { "PositionId", "AttributeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_AspNetUsers_AuthorId",
                table: "DiscussionPosts",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionAttribute_Attributes_AttributeId",
                table: "PositionAttribute",
                column: "AttributeId",
                principalTable: "Attributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionAttribute_Positions_PositionId",
                table: "PositionAttribute",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
