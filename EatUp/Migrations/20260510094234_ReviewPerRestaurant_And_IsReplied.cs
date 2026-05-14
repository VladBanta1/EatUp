using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatUp.Migrations
{
    /// <inheritdoc />
    public partial class ReviewPerRestaurant_And_IsReplied : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @fk_exists = (
                    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME        = 'Reviews'
                      AND CONSTRAINT_NAME   = 'FK_Reviews_Orders_OrderId'
                      AND CONSTRAINT_TYPE   = 'FOREIGN KEY');
                SET @sql = IF(@fk_exists > 0,
                    'ALTER TABLE Reviews DROP FOREIGN KEY FK_Reviews_Orders_OrderId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @fk_exists = (
                    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME        = 'Reviews'
                      AND CONSTRAINT_NAME   = 'FK_Reviews_Users_CustomerId'
                      AND CONSTRAINT_TYPE   = 'FOREIGN KEY');
                SET @sql = IF(@fk_exists > 0,
                    'ALTER TABLE Reviews DROP FOREIGN KEY FK_Reviews_Users_CustomerId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @idx_exists = (
                    SELECT COUNT(*) FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME   = 'Reviews'
                      AND INDEX_NAME   = 'IX_Reviews_CustomerId');
                SET @sql = IF(@idx_exists > 0,
                    'ALTER TABLE Reviews DROP INDEX IX_Reviews_CustomerId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @idx_exists = (
                    SELECT COUNT(*) FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME   = 'Reviews'
                      AND INDEX_NAME   = 'IX_Reviews_OrderId');
                SET @sql = IF(@idx_exists > 0,
                    'ALTER TABLE Reviews DROP INDEX IX_Reviews_OrderId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "Reviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsReplied",
                table: "ContactMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Keep only the most recent review per (CustomerId, RestaurantId) pair
            migrationBuilder.Sql(@"
                DELETE r1 FROM Reviews r1
                INNER JOIN Reviews r2
                    ON r1.CustomerId   = r2.CustomerId
                   AND r1.RestaurantId = r2.RestaurantId
                   AND r1.CreatedAt    < r2.CreatedAt;
            ");

            // Recalculate restaurant rating and review count after deduplication
            migrationBuilder.Sql(@"
                UPDATE Restaurants r
                SET r.TotalReviews = (SELECT COUNT(*)                             FROM Reviews rv WHERE rv.RestaurantId = r.Id),
                    r.Rating       = IFNULL((SELECT ROUND(AVG(rv.Rating), 2) FROM Reviews rv WHERE rv.RestaurantId = r.Id), 0);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId_RestaurantId",
                table: "Reviews",
                columns: new[] { "CustomerId", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // MySQL can use IX_Reviews_CustomerId_RestaurantId as the backing index for this
            // FK because CustomerId is its leading column
            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_CustomerId",
                table: "Reviews",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @fk_exists = (
                    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME        = 'Reviews'
                      AND CONSTRAINT_NAME   = 'FK_Reviews_Orders_OrderId'
                      AND CONSTRAINT_TYPE   = 'FOREIGN KEY');
                SET @sql = IF(@fk_exists > 0,
                    'ALTER TABLE Reviews DROP FOREIGN KEY FK_Reviews_Orders_OrderId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @fk_exists = (
                    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME        = 'Reviews'
                      AND CONSTRAINT_NAME   = 'FK_Reviews_Users_CustomerId'
                      AND CONSTRAINT_TYPE   = 'FOREIGN KEY');
                SET @sql = IF(@fk_exists > 0,
                    'ALTER TABLE Reviews DROP FOREIGN KEY FK_Reviews_Users_CustomerId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @idx_exists = (
                    SELECT COUNT(*) FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME   = 'Reviews'
                      AND INDEX_NAME   = 'IX_Reviews_CustomerId_RestaurantId');
                SET @sql = IF(@idx_exists > 0,
                    'ALTER TABLE Reviews DROP INDEX IX_Reviews_CustomerId_RestaurantId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @idx_exists = (
                    SELECT COUNT(*) FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME   = 'Reviews'
                      AND INDEX_NAME   = 'IX_Reviews_OrderId');
                SET @sql = IF(@idx_exists > 0,
                    'ALTER TABLE Reviews DROP INDEX IX_Reviews_OrderId',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.DropColumn(
                name: "IsReplied",
                table: "ContactMessages");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_CustomerId",
                table: "Reviews",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
