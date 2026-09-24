using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'CvPlatformFtCatalog')
        BEGIN
            CREATE FULLTEXT CATALOG CvPlatformFtCatalog AS DEFAULT;
        END
    ", suppressTransaction: true);

            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.Positions'))
        BEGIN
            CREATE FULLTEXT INDEX ON dbo.Positions(Title, ShortDescription)
            KEY INDEX PK_Positions
            ON CvPlatformFtCatalog
            WITH CHANGE_TRACKING AUTO;
        END
    ", suppressTransaction: true);

            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.DiscussionPosts'))
        BEGIN
            CREATE FULLTEXT INDEX ON dbo.DiscussionPosts(ContentMarkdown)
            KEY INDEX PK_DiscussionPosts
            ON CvPlatformFtCatalog
            WITH CHANGE_TRACKING AUTO;
        END
    ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FULLTEXT INDEX ON dbo.DiscussionPosts;", suppressTransaction: true);
            migrationBuilder.Sql("DROP FULLTEXT INDEX ON dbo.Positions;", suppressTransaction: true);
            migrationBuilder.Sql("DROP FULLTEXT CATALOG CvPlatformFtCatalog;", suppressTransaction: true);
        }
    }
}