using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DhcpReservationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    RequestedIpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DhcpReservationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DhcpReservationRequests_RequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DhcpReservationRequests_SystemUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DhcpReservationRequests_SystemUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DnsRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RecordValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DnsRequests_RequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DnsRequests_SystemUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DnsRequests_SystemUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembershipRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembershipRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembershipRequests_RequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembershipRequests_SystemUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMembershipRequests_SystemUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_RequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_SystemUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_SystemUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DhcpReservationRequests_ApprovedById",
                table: "DhcpReservationRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_DhcpReservationRequests_RequestedById",
                table: "DhcpReservationRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_DhcpReservationRequests_StatusId",
                table: "DhcpReservationRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DnsRequests_ApprovedById",
                table: "DnsRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_DnsRequests_RequestedById",
                table: "DnsRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_DnsRequests_StatusId",
                table: "DnsRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembershipRequests_ApprovedById",
                table: "GroupMembershipRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembershipRequests_RequestedById",
                table: "GroupMembershipRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembershipRequests_StatusId",
                table: "GroupMembershipRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_ApprovedById",
                table: "PasswordResetRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_RequestedById",
                table: "PasswordResetRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_StatusId",
                table: "PasswordResetRequests",
                column: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DhcpReservationRequests");

            migrationBuilder.DropTable(
                name: "DnsRequests");

            migrationBuilder.DropTable(
                name: "GroupMembershipRequests");

            migrationBuilder.DropTable(
                name: "PasswordResetRequests");
        }
    }
}
