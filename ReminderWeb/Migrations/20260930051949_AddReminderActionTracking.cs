using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReminderWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderActionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletionProof",
                table: "Reminders",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmergencyPromptedAt",
                table: "Reminders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyResponse",
                table: "Reminders",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PixelEffectStartedAt",
                table: "Reminders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderZones_ReminderId",
                table: "ReminderZones",
                column: "ReminderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReminderZones_Reminders_ReminderId",
                table: "ReminderZones",
                column: "ReminderId",
                principalTable: "Reminders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReminderZones_Reminders_ReminderId",
                table: "ReminderZones");

            migrationBuilder.DropIndex(
                name: "IX_ReminderZones_ReminderId",
                table: "ReminderZones");

            migrationBuilder.DropColumn(
                name: "CompletionProof",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "EmergencyPromptedAt",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "EmergencyResponse",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "PixelEffectStartedAt",
                table: "Reminders");
        }
    }
}
