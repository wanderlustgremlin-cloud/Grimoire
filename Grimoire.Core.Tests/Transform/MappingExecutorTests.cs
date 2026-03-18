using Grimoire.Core.Extract;
using Grimoire.Core.Results;
using Grimoire.Core.Transform;

namespace Grimoire.Core.Tests.Transform;

public class MappingExecutorTests
{
    [Fact]
    public void Transforms_simple_row_to_entity()
    {
        var mapping = new CustomerMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, new Core.KeyMap.KeyMap(), "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "Alice";
        row["email"] = "alice@test.com";
        row["is_active"] = true;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("Alice", entity.Name);
        Assert.Equal("alice@test.com", entity.Email);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Applies_default_when_source_is_null()
    {
        var mapping = new CustomerWithDefaultsMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, new Core.KeyMap.KeyMap(), "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = null;
        row["is_active"] = null;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Equal("Unknown", entity.Name);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Applies_default_when_source_is_DBNull()
    {
        var mapping = new CustomerWithDefaultsMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, new Core.KeyMap.KeyMap(), "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = DBNull.Value;
        row["is_active"] = DBNull.Value;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Equal("Unknown", entity.Name);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Applies_converter()
    {
        var mapping = new CustomerWithConverterMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, new Core.KeyMap.KeyMap(), "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "alice";

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Equal("ALICE", entity.Name);
    }

    [Fact]
    public void Resolves_foreign_key_from_keymap()
    {
        var keyMap = new Core.KeyMap.KeyMap();
        keyMap.Register<Department>(legacyKey: 50, newKey: 7);

        var mapping = new CustomerWithForeignKeyMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, keyMap, "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "Alice";
        row["legacy_dept_id"] = 50;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Equal(7, entity.DepartmentId);
    }

    [Fact]
    public void Returns_error_when_foreign_key_not_found()
    {
        var keyMap = new Core.KeyMap.KeyMap(); // empty — no keys registered

        var mapping = new CustomerWithForeignKeyMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, keyMap, "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "Alice";
        row["legacy_dept_id"] = 999;

        var (entity, error) = executor.Transform(row);

        Assert.Null(entity);
        Assert.NotNull(error);
        Assert.Equal(RowErrorType.ForeignKeyNotFound, error.ErrorType);
        Assert.Contains("999", error.Message);
    }

    [Fact]
    public void Foreign_key_null_value_sets_null_without_error()
    {
        var keyMap = new Core.KeyMap.KeyMap();

        var mapping = new CustomerWithForeignKeyMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, keyMap, "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "Alice";
        row["legacy_dept_id"] = null;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Null(entity.DepartmentId);
    }

    [Fact]
    public void Converts_compatible_types()
    {
        var mapping = new CustomerMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, new Core.KeyMap.KeyMap(), "Customer");

        // Pass long instead of int — should convert
        var row = new SourceRow();
        row["customer_id"] = 1L;
        row["customer_name"] = "Alice";
        row["email"] = "a@b.com";
        row["is_active"] = true;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void Null_string_property_is_set_to_null()
    {
        var mapping = new CustomerMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, new Core.KeyMap.KeyMap(), "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "Alice";
        row["email"] = null;
        row["is_active"] = false;

        var (entity, error) = executor.Transform(row);

        Assert.Null(error);
        Assert.NotNull(entity);
        Assert.Null(entity.Email);
    }

    [Fact]
    public void Error_includes_source_data()
    {
        var keyMap = new Core.KeyMap.KeyMap();
        var mapping = new CustomerWithForeignKeyMapping();
        var built = mapping.Build();
        var executor = new MappingExecutor<Customer>(built.Mappings, keyMap, "Customer");

        var row = new SourceRow();
        row["customer_id"] = 1;
        row["customer_name"] = "Alice";
        row["legacy_dept_id"] = 999;

        var (_, error) = executor.Transform(row);

        Assert.NotNull(error);
        Assert.NotNull(error.SourceData);
        Assert.Equal(1, error.SourceData["customer_id"]);
    }
}
