using Grimoire.Core.Extract;

namespace Grimoire.Core.Tests.Extract;

public class SourceRowTests
{
    [Fact]
    public void Indexer_sets_and_gets_value()
    {
        var row = new SourceRow();
        row["Name"] = "Alice";

        Assert.Equal("Alice", row["Name"]);
    }

    [Fact]
    public void Indexer_is_case_insensitive()
    {
        var row = new SourceRow();
        row["Name"] = "Alice";

        Assert.Equal("Alice", row["name"]);
        Assert.Equal("Alice", row["NAME"]);
    }

    [Fact]
    public void Indexer_returns_null_for_missing_column()
    {
        var row = new SourceRow();

        Assert.Null(row["DoesNotExist"]);
    }

    [Fact]
    public void ContainsColumn_returns_true_for_existing()
    {
        var row = new SourceRow();
        row["Name"] = "Alice";

        Assert.True(row.ContainsColumn("Name"));
        Assert.True(row.ContainsColumn("name"));
    }

    [Fact]
    public void ContainsColumn_returns_false_for_missing()
    {
        var row = new SourceRow();

        Assert.False(row.ContainsColumn("Missing"));
    }

    [Fact]
    public void Columns_returns_all_column_names()
    {
        var row = new SourceRow();
        row["Id"] = 1;
        row["Name"] = "Alice";

        var columns = row.Columns.ToList();

        Assert.Contains("Id", columns);
        Assert.Contains("Name", columns);
        Assert.Equal(2, columns.Count);
    }

    [Fact]
    public void ToDictionary_returns_copy_of_data()
    {
        var row = new SourceRow();
        row["Id"] = 1;
        row["Name"] = "Alice";

        var dict = row.ToDictionary();

        Assert.Equal(1, dict["Id"]);
        Assert.Equal("Alice", dict["Name"]);

        // Verify it's a copy
        dict["Id"] = 999;
        Assert.Equal(1, row["Id"]);
    }

    [Fact]
    public void FromDictionary_creates_row_from_dictionary()
    {
        var dict = new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "Alice" };

        var row = SourceRow.FromDictionary(dict);

        Assert.Equal(1, row["Id"]);
        Assert.Equal("Alice", row["Name"]);
    }

    [Fact]
    public void Stores_null_values()
    {
        var row = new SourceRow();
        row["Email"] = null;

        Assert.Null(row["Email"]);
        Assert.True(row.ContainsColumn("Email"));
    }
}
