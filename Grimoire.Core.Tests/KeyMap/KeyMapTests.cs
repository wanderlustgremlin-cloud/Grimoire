using Grimoire.Core.KeyMap;

namespace Grimoire.Core.Tests.KeyMap;

public class KeyMapTests
{
    private readonly Core.KeyMap.KeyMap _keyMap = new();

    [Fact]
    public void Register_and_Resolve_returns_new_key()
    {
        _keyMap.Register<Customer>(legacyKey: 100, newKey: 1);

        var result = _keyMap.Resolve<Customer>(100);

        Assert.Equal(1, result);
    }

    [Fact]
    public void Resolve_returns_null_for_unregistered_key()
    {
        var result = _keyMap.Resolve<Customer>(999);

        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_returns_true_when_found()
    {
        _keyMap.Register<Customer>(legacyKey: 100, newKey: 1);

        var found = _keyMap.TryResolve<Customer>(100, out var newKey);

        Assert.True(found);
        Assert.Equal(1, newKey);
    }

    [Fact]
    public void TryResolve_returns_false_when_not_found()
    {
        var found = _keyMap.TryResolve<Customer>(999, out var newKey);

        Assert.False(found);
    }

    [Fact]
    public void Keys_are_isolated_by_entity_type()
    {
        _keyMap.Register<Customer>(legacyKey: 1, newKey: 100);
        _keyMap.Register<Department>(legacyKey: 1, newKey: 200);

        Assert.Equal(100, _keyMap.Resolve<Customer>(1));
        Assert.Equal(200, _keyMap.Resolve<Department>(1));
    }

    [Fact]
    public void Register_overwrites_existing_key()
    {
        _keyMap.Register<Customer>(legacyKey: 1, newKey: 100);
        _keyMap.Register<Customer>(legacyKey: 1, newKey: 200);

        Assert.Equal(200, _keyMap.Resolve<Customer>(1));
    }

    [Fact]
    public void Clear_removes_all_entries()
    {
        _keyMap.Register<Customer>(legacyKey: 1, newKey: 100);
        _keyMap.Register<Department>(legacyKey: 2, newKey: 200);

        _keyMap.Clear();

        Assert.Null(_keyMap.Resolve<Customer>(1));
        Assert.Null(_keyMap.Resolve<Department>(2));
    }

    [Fact]
    public void Internal_Register_and_Resolve_work_by_type()
    {
        _keyMap.Register(typeof(Customer), legacyKey: 50, newKey: 500);

        var result = _keyMap.Resolve(typeof(Customer), 50);

        Assert.Equal(500, result);
    }

    [Fact]
    public void Generic_and_internal_methods_share_same_store()
    {
        _keyMap.Register<Customer>(legacyKey: 1, newKey: 100);

        var result = _keyMap.Resolve(typeof(Customer), 1);

        Assert.Equal(100, result);
    }

    [Fact]
    public void Supports_string_keys()
    {
        _keyMap.Register<Customer>(legacyKey: "OLD-001", newKey: "NEW-001");

        Assert.Equal("NEW-001", _keyMap.Resolve<Customer>("OLD-001"));
    }
}
