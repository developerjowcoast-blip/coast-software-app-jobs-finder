using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobFinder.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    WorkMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Salary = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompatibilityScore = table.Column<int>(type: "INTEGER", nullable: false),
                    CompatibilityReasonsJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiresSpanish = table.Column<bool>(type: "INTEGER", nullable: false),
                    InclusiveEmployer = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateActions_JobListings_JobListingId",
                        column: x => x.JobListingId,
                        principalTable: "JobListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateActions_JobListingId",
                table: "CandidateActions",
                column: "JobListingId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateActions_OccurredAt",
                table: "CandidateActions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_CompatibilityScore",
                table: "JobListings",
                column: "CompatibilityScore");

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_IsFavorite_IsHidden",
                table: "JobListings",
                columns: new[] { "IsFavorite", "IsHidden" });

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_PublishedAt",
                table: "JobListings",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_Source_ExternalId",
                table: "JobListings",
                columns: new[] { "Source", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateActions");

            migrationBuilder.DropTable(
                name: "JobListings");
        }
    }
}
