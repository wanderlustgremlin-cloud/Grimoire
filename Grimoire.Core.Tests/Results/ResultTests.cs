using Grimoire.Core.Results;

namespace Grimoire.Core.Tests.Results;

public class ResultTests
{
    [Fact]
    public void EntityResult_RowsErrored_equals_error_count()
    {
        var result = new EntityResult { EntityName = "Test" };
        result.Errors.Add(new RowError("Test", RowErrorType.MappingError, "err1"));
        result.Errors.Add(new RowError("Test", RowErrorType.LoadError, "err2"));

        Assert.Equal(2, result.RowsErrored);
    }

    [Fact]
    public void EntityResult_Success_true_when_no_errors()
    {
        var result = new EntityResult { EntityName = "Test" };

        Assert.True(result.Success);
    }

    [Fact]
    public void EntityResult_Success_false_when_errors_exist()
    {
        var result = new EntityResult { EntityName = "Test" };
        result.Errors.Add(new RowError("Test", RowErrorType.MappingError, "err"));

        Assert.False(result.Success);
    }

    [Fact]
    public void EtlResult_Success_true_when_all_entities_succeed()
    {
        var result = new EtlResult();
        result.EntityResults.Add(new EntityResult { EntityName = "A" });
        result.EntityResults.Add(new EntityResult { EntityName = "B" });

        Assert.True(result.Success);
    }

    [Fact]
    public void EtlResult_Success_false_when_any_entity_has_errors()
    {
        var result = new EtlResult();
        result.EntityResults.Add(new EntityResult { EntityName = "A" });
        var failed = new EntityResult { EntityName = "B" };
        failed.Errors.Add(new RowError("B", RowErrorType.LoadError, "fail"));
        result.EntityResults.Add(failed);

        Assert.False(result.Success);
    }

    [Fact]
    public void EtlResult_aggregates_row_counts()
    {
        var result = new EtlResult();
        result.EntityResults.Add(new EntityResult { EntityName = "A", RowsInserted = 10, RowsUpdated = 5 });
        result.EntityResults.Add(new EntityResult { EntityName = "B", RowsInserted = 20, RowsUpdated = 3 });

        Assert.Equal(30, result.TotalRowsInserted);
        Assert.Equal(8, result.TotalRowsUpdated);
    }

    [Fact]
    public void EtlResult_aggregates_error_counts()
    {
        var result = new EtlResult();
        var a = new EntityResult { EntityName = "A" };
        a.Errors.Add(new RowError("A", RowErrorType.MappingError, "err"));
        var b = new EntityResult { EntityName = "B" };
        b.Errors.Add(new RowError("B", RowErrorType.LoadError, "err1"));
        b.Errors.Add(new RowError("B", RowErrorType.LoadError, "err2"));
        result.EntityResults.Add(a);
        result.EntityResults.Add(b);

        Assert.Equal(3, result.TotalRowsErrored);
    }

    [Fact]
    public void EtlResult_empty_is_success()
    {
        var result = new EtlResult();

        Assert.True(result.Success);
        Assert.Equal(0, result.TotalRowsInserted);
        Assert.Equal(0, result.TotalRowsUpdated);
        Assert.Equal(0, result.TotalRowsErrored);
    }

    [Fact]
    public void BatchResult_record_stores_all_fields()
    {
        var batch = new BatchResult("Customer", 3, 1000, 1000, 0, 0,
            TimeSpan.FromMilliseconds(150), 1000);

        Assert.Equal("Customer", batch.EntityName);
        Assert.Equal(3, batch.BatchNumber);
        Assert.Equal(1000, batch.RowsInBatch);
        Assert.Equal(1000, batch.RowsInserted);
        Assert.Equal(0, batch.RowsUpdated);
        Assert.Equal(0, batch.RowsSkipped);
        Assert.Equal(TimeSpan.FromMilliseconds(150), batch.Duration);
        Assert.Equal(1000, batch.BatchSize);
    }

    [Fact]
    public void RowError_record_stores_all_fields()
    {
        var ex = new Exception("inner");
        var data = new Dictionary<string, object?> { ["col1"] = "val1" };

        var error = new RowError("Customer", RowErrorType.TypeConversion, "bad value", data, ex);

        Assert.Equal("Customer", error.EntityName);
        Assert.Equal(RowErrorType.TypeConversion, error.ErrorType);
        Assert.Equal("bad value", error.Message);
        Assert.Same(data, error.SourceData);
        Assert.Same(ex, error.Exception);
    }

    [Fact]
    public void RowError_optional_fields_default_to_null()
    {
        var error = new RowError("Customer", RowErrorType.MappingError, "msg");

        Assert.Null(error.SourceData);
        Assert.Null(error.Exception);
    }
}
