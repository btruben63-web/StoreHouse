using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Product.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateSequence<int>(
                name: "ProductIdSequence",
                startValue: 100011L,
                minValue: 100000L,
                maxValue: 999999L);

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"ProductIdSequence\"')"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.CheckConstraint("CK_Product_Price", "\"Price\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Stock",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock", x => x.ProductId);
                    table.CheckConstraint("CK_Stock_Quantity", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_Stock_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovement",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'11', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityAfter = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovement", x => x.Id);
                    table.CheckConstraint("CK_StockMovement_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_StockMovement_Type", "\"Type\" IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_StockMovement_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Product",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "DeletedBy", "Description", "Name", "Price", "UpdatedAt" },
                values: new object[,]
                {
                    { 100001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Martelo com cabo de madeira e cabeça de aço forjado.", "Martelo de Carpinteiro", 12.90m, null },
                    { 100002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Berbequim com bateria de lítio e duas velocidades.", "Berbequim sem Fios 18V", 89.99m, null },
                    { 100003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Fita métrica com travão e caixa antichoque.", "Fita Métrica 5m", 6.50m, null },
                    { 100004, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Alicate de 180 mm com punhos isolados.", "Alicate Universal", 9.75m, null },
                    { 100005, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Serrote para cortes de precisão em madeira.", "Serrote de Costas", 14.20m, null },
                    { 100006, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Nível em alumínio com três bolhas.", "Nível de Bolha 60cm", 11.40m, null },
                    { 100007, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Conjunto de seis chaves planas e Phillips.", "Jogo de Chaves de Fendas", 18.90m, null },
                    { 100008, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Parafusos para madeira de vários tamanhos.", "Caixa de Parafusos 200un", 7.30m, null },
                    { 100009, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Folha de lixa de grão médio.", "Lixa para Madeira P120", 1.20m, null },
                    { 100010, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Cola vinílica de secagem rápida.", "Cola de Madeira 750ml", 8.60m, null }
                });

            migrationBuilder.InsertData(
                table: "Stock",
                columns: new[] { "ProductId", "Quantity", "UpdatedAt" },
                values: new object[,]
                {
                    { 100001, 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100002, 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100003, 120, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100004, 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100005, 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100006, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100007, 45, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100008, 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100009, 500, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 100010, 80, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "StockMovement",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "ProductId", "Quantity", "QuantityAfter", "Type" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100001, 40, 40, (short)1 },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100002, 15, 15, (short)1 },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100003, 120, 120, (short)1 },
                    { 4L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100004, 60, 60, (short)1 },
                    { 5L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100005, 25, 25, (short)1 },
                    { 6L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100006, 30, 30, (short)1 },
                    { 7L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100007, 45, 45, (short)1 },
                    { 8L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100008, 200, 200, (short)1 },
                    { 9L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100009, 500, 500, (short)1 },
                    { 10L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 100010, 80, 80, (short)1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Product_Name_Trgm",
                table: "Product",
                column: "Name",
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Quantity",
                table: "Stock",
                column: "Quantity");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_ProductId_CreatedAt",
                table: "StockMovement",
                columns: new[] { "ProductId", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stock");

            migrationBuilder.DropTable(
                name: "StockMovement");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropSequence(
                name: "ProductIdSequence");
        }
    }
}
