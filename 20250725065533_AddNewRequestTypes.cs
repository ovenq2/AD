using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddNewRequestTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAttributeChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAttributeChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAttributeChangeRequests_RequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAttributeChangeRequests_SystemUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAttributeChangeRequests_SystemUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeChangeRequests_ApprovedById",
                table: "UserAttributeChangeRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeChangeRequests_RequestedById",
                table: "UserAttributeChangeRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeChangeRequests_StatusId",
                table: "UserAttributeChangeRequests",
                column: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAttributeChangeRequests");
        }
    }
}
