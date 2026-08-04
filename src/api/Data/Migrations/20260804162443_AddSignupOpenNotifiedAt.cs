using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectThor.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignupOpenNotifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignupOpenNotifiedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignupOpenNotifiedAt",
                table: "Games");
        }
    }
}
