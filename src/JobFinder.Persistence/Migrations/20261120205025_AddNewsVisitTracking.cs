using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFinder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsVisitTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsViewed",
                table: "JobListings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreviousCompatibilityScore",
                table: "JobListings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScoreChangedAt",
                table: "JobListings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ViewedAt",
                table: "JobListings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserVisitStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastVisitAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVisitStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_FirstSeenAt",
                table: "JobListings",
                column: "FirstSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_ScoreChangedAt",
                table: "JobListings",
                column: "ScoreChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVisitStates");

            migrationBuilder.DropIndex(
                name: "IX_JobListings_FirstSeenAt",
                table: "JobListings");

            migrationBuilder.DropIndex(
                name: "IX_JobListings_ScoreChangedAt",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "IsViewed",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "PreviousCompatibilityScore",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "ScoreChangedAt",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "ViewedAt",
                table: "JobListings");
        }
    }
}
