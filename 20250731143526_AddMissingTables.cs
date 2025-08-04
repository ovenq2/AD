using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PasswordResetRequests tablosu zaten var mı kontrol et, yoksa oluştur
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PasswordResetRequests' AND xtype='U')
                BEGIN
                    CREATE TABLE [PasswordResetRequests] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [RequestNumber] nvarchar(max) NOT NULL,
                        [Username] nvarchar(50) NOT NULL,
                        [UserEmail] nvarchar(100) NOT NULL,
                        [Reason] nvarchar(500) NULL,
                        [RequestedById] int NOT NULL,
                        [RequestedDate] datetime2 NOT NULL,
                        [StatusId] int NOT NULL,
                        [ApprovedById] int NULL,
                        [ApprovedDate] datetime2 NULL,
                        [RejectionReason] nvarchar(max) NULL,
                        CONSTRAINT [PK_PasswordResetRequests] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PasswordResetRequests_RequestStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [RequestStatuses] ([Id]),
                        CONSTRAINT [FK_PasswordResetRequests_SystemUsers_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [SystemUsers] ([Id]),
                        CONSTRAINT [FK_PasswordResetRequests_SystemUsers_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [SystemUsers] ([Id])
                    );

                    CREATE INDEX [IX_PasswordResetRequests_ApprovedById] ON [PasswordResetRequests] ([ApprovedById]);
                    CREATE INDEX [IX_PasswordResetRequests_RequestedById] ON [PasswordResetRequests] ([RequestedById]);
                    CREATE INDEX [IX_PasswordResetRequests_StatusId] ON [PasswordResetRequests] ([StatusId]);
                END
            ");

            // GroupMembershipRequests tablosunu oluştur
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

            // DnsRequests tablosunu oluştur
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

            // DhcpReservationRequests tablosunu oluştur
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

            // İndeksleri oluştur
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GroupMembershipRequests");
            migrationBuilder.DropTable(name: "DnsRequests");
            migrationBuilder.DropTable(name: "DhcpReservationRequests");
        }
    }
}