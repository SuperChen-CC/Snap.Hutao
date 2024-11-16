﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_reliquaries");

            migrationBuilder.DropTable(
                name: "inventory_weapons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_reliquaries",
                columns: table => new
                {
                    InnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppendPropIdList = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    MainPropId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reliquaries", x => x.InnerId);
                    table.ForeignKey(
                        name: "FK_inventory_reliquaries_cultivate_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "cultivate_projects",
                        principalColumn: "InnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_weapons",
                columns: table => new
                {
                    InnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    PromoteLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_weapons", x => x.InnerId);
                    table.ForeignKey(
                        name: "FK_inventory_weapons_cultivate_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "cultivate_projects",
                        principalColumn: "InnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reliquaries_ProjectId",
                table: "inventory_reliquaries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_weapons_ProjectId",
                table: "inventory_weapons",
                column: "ProjectId");
        }
    }
}
