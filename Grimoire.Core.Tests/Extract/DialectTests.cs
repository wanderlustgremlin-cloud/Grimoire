using Grimoire.Core.Extract;
using Grimoire.Core.Extract.Dialects;

namespace Grimoire.Core.Tests.Extract;

public class DialectTests
{
    [Fact]
    public void SqlServer_quotes_with_brackets()
    {
        var dialect = new SqlServerDialect();
        Assert.Equal("[MyTable]", dialect.QuoteIdentifier("MyTable"));
    }

    [Fact]
    public void Postgres_quotes_with_double_quotes()
    {
        var dialect = new PostgresDialect();
        Assert.Equal("\"MyTable\"", dialect.QuoteIdentifier("MyTable"));
    }

    [Fact]
    public void Oracle_quotes_with_double_quotes()
    {
        var dialect = new OracleDialect();
        Assert.Equal("\"MyTable\"", dialect.QuoteIdentifier("MyTable"));
    }

    [Fact]
    public void MySql_quotes_with_backticks()
    {
        var dialect = new MySqlDialect();
        Assert.Equal("`MyTable`", dialect.QuoteIdentifier("MyTable"));
    }

    [Theory]
    [InlineData(typeof(SqlServerDialect), "[dbo].[Users]")]
    [InlineData(typeof(PostgresDialect), "\"public\".\"Users\"")]
    [InlineData(typeof(OracleDialect), "\"HR\".\"Users\"")]
    [InlineData(typeof(MySqlDialect), "`mydb`.`Users`")]
    public void QualifyTable_with_schema(Type dialectType, string expected)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;
        var schema = dialectType.Name switch
        {
            "SqlServerDialect" => "dbo",
            "PostgresDialect" => "public",
            "OracleDialect" => "HR",
            "MySqlDialect" => "mydb",
            _ => "schema"
        };
        Assert.Equal(expected, dialect.QualifyTable("Users", schema));
    }

    [Fact]
    public void QualifyTable_without_schema_uses_table_only()
    {
        var dialects = new ISqlDialect[]
        {
            new SqlServerDialect(),
            new PostgresDialect(),
            new OracleDialect(),
            new MySqlDialect()
        };

        var expected = new[] { "[Users]", "\"Users\"", "\"Users\"", "`Users`" };

        for (int i = 0; i < dialects.Length; i++)
        {
            Assert.Equal(expected[i], dialects[i].QualifyTable("Users"));
        }
    }

    [Fact]
    public void SqlServer_pagination_uses_offset_fetch()
    {
        var dialect = new SqlServerDialect();
        Assert.Equal(" ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", dialect.BuildPagination(10, null));
        Assert.Equal(" ORDER BY 1 OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY", dialect.BuildPagination(10, 5));
        Assert.Equal("", dialect.BuildPagination(null, null));
    }

    [Fact]
    public void Postgres_pagination_uses_limit_offset()
    {
        var dialect = new PostgresDialect();
        Assert.Equal(" LIMIT 10", dialect.BuildPagination(10, null));
        Assert.Equal(" LIMIT 10 OFFSET 5", dialect.BuildPagination(10, 5));
        Assert.Equal("", dialect.BuildPagination(null, null));
    }

    [Fact]
    public void Oracle_pagination_uses_fetch_first()
    {
        var dialect = new OracleDialect();
        Assert.Equal(" FETCH FIRST 10 ROWS ONLY", dialect.BuildPagination(10, null));
        Assert.Equal(" OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY", dialect.BuildPagination(10, 5));
        Assert.Equal("", dialect.BuildPagination(null, null));
    }

    [Fact]
    public void MySql_pagination_uses_limit_offset()
    {
        var dialect = new MySqlDialect();
        Assert.Equal(" LIMIT 10", dialect.BuildPagination(10, null));
        Assert.Equal(" LIMIT 10 OFFSET 5", dialect.BuildPagination(10, 5));
        Assert.Equal("", dialect.BuildPagination(null, null));
    }

    [Fact]
    public void DialectFactory_creates_correct_dialect()
    {
        Assert.IsType<SqlServerDialect>(DialectFactory.Create(DatabaseProvider.SqlServer));
        Assert.IsType<PostgresDialect>(DialectFactory.Create(DatabaseProvider.Postgres));
        Assert.IsType<OracleDialect>(DialectFactory.Create(DatabaseProvider.Oracle));
        Assert.IsType<MySqlDialect>(DialectFactory.Create(DatabaseProvider.MySql));
    }

    [Fact]
    public void Schema_property_on_TableSchema_defaults_to_null()
    {
        var builder = new SchemaBuilder();
        builder.Table("Users").Columns("Id").Done();

        var schemas = builder.Build();
        Assert.Null(schemas["Users"].Schema);
    }

    [Fact]
    public void Schema_property_on_TableSchema_is_set()
    {
        var builder = new SchemaBuilder();
        builder.Table("Users", schema: "legacy").Columns("Id").Done();

        var schemas = builder.Build();
        Assert.Equal("legacy", schemas["Users"].Schema);
    }
}
