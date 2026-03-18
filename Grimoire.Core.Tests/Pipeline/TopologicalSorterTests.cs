using Grimoire.Core.Pipeline;

namespace Grimoire.Core.Tests.Pipeline;

public class TopologicalSorterTests
{
    private static EntityRegistration MakeEntity<T>(params Type[] dependencies)
    {
        var reg = new EntityRegistration
        {
            EntityType = typeof(T),
            EntityName = typeof(T).Name
        };
        reg.Dependencies.AddRange(dependencies);
        return reg;
    }

    [Fact]
    public void No_dependencies_preserves_order()
    {
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Customer>(),
            MakeEntity<Department>()
        };

        var sorted = TopologicalSorter.Sort(entities);

        Assert.Equal(2, sorted.Count);
    }

    [Fact]
    public void Dependency_comes_before_dependent()
    {
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Customer>(typeof(Department)), // Customer depends on Department
            MakeEntity<Department>()
        };

        var sorted = TopologicalSorter.Sort(entities);

        var deptIndex = sorted.FindIndex(e => e.EntityType == typeof(Department));
        var custIndex = sorted.FindIndex(e => e.EntityType == typeof(Customer));

        Assert.True(deptIndex < custIndex, "Department should come before Customer");
    }

    [Fact]
    public void Chain_dependency_orders_correctly()
    {
        // Order -> Customer -> Department
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Order>(typeof(Customer)),
            MakeEntity<Customer>(typeof(Department)),
            MakeEntity<Department>()
        };

        var sorted = TopologicalSorter.Sort(entities);

        var deptIndex = sorted.FindIndex(e => e.EntityType == typeof(Department));
        var custIndex = sorted.FindIndex(e => e.EntityType == typeof(Customer));
        var orderIndex = sorted.FindIndex(e => e.EntityType == typeof(Order));

        Assert.True(deptIndex < custIndex);
        Assert.True(custIndex < orderIndex);
    }

    [Fact]
    public void Diamond_dependency_works()
    {
        // Both Customer and Order depend on Department, Order also depends on Customer
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Order>(typeof(Customer), typeof(Department)),
            MakeEntity<Customer>(typeof(Department)),
            MakeEntity<Department>()
        };

        var sorted = TopologicalSorter.Sort(entities);

        var deptIndex = sorted.FindIndex(e => e.EntityType == typeof(Department));
        var custIndex = sorted.FindIndex(e => e.EntityType == typeof(Customer));
        var orderIndex = sorted.FindIndex(e => e.EntityType == typeof(Order));

        Assert.True(deptIndex < custIndex);
        Assert.True(deptIndex < orderIndex);
        Assert.True(custIndex < orderIndex);
    }

    [Fact]
    public void Circular_dependency_throws()
    {
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Customer>(typeof(Department)),
            MakeEntity<Department>(typeof(Customer))
        };

        var ex = Assert.Throws<InvalidOperationException>(() => TopologicalSorter.Sort(entities));
        Assert.Contains("Circular dependency", ex.Message);
    }

    [Fact]
    public void Missing_dependency_throws()
    {
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Customer>(typeof(Department)) // Department not registered
        };

        var ex = Assert.Throws<InvalidOperationException>(() => TopologicalSorter.Sort(entities));
        Assert.Contains("Department", ex.Message);
        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public void Single_entity_no_dependencies()
    {
        var entities = new List<EntityRegistration>
        {
            MakeEntity<Customer>()
        };

        var sorted = TopologicalSorter.Sort(entities);

        Assert.Single(sorted);
        Assert.Equal(typeof(Customer), sorted[0].EntityType);
    }

    [Fact]
    public void Empty_list_returns_empty()
    {
        var sorted = TopologicalSorter.Sort([]);

        Assert.Empty(sorted);
    }
}
