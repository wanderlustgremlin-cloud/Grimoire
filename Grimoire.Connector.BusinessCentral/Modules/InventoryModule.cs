using Grimoire.Connector.BusinessCentral.Registry;

namespace Grimoire.Connector.BusinessCentral.Modules;

internal sealed class InventoryModule : IBcModule
{
    public string ModuleName => "Inventory";

    public void Register(BcRegistry registry)
    {
        registry.Entity("items", "items", "Inventory")
            .Fields("id", "number", "displayName", "type",
                "itemCategoryId", "itemCategoryCode",
                "blocked",
                "gtin",
                "inventory",
                "unitPrice", "unitCost",
                "priceIncludesTax",
                "taxGroupId", "taxGroupCode",
                "baseUnitOfMeasureId", "baseUnitOfMeasureCode",
                "lastModifiedDateTime")
            .Field("generalProductPostingGroupId", since: BcApiVersion.V2_0)
            .Field("generalProductPostingGroupCode", since: BcApiVersion.V2_0)
            .Field("inventoryPostingGroupId", since: BcApiVersion.V2_0)
            .Field("inventoryPostingGroupCode", since: BcApiVersion.V2_0);

        registry.Entity("itemCategories", "itemCategories", "Inventory")
            .Fields("id", "code", "displayName",
                "lastModifiedDateTime")
            .Field("parentCategory", since: BcApiVersion.V2_0);

        registry.Entity("itemVariants", "itemVariants", "Inventory", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "itemId", "itemNumber",
                "code", "description");

        registry.Entity("unitsOfMeasure", "unitsOfMeasure", "Inventory")
            .Fields("id", "code", "displayName",
                "internationalStandardCode",
                "lastModifiedDateTime");

        registry.Entity("locations", "locations", "Inventory", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "code", "displayName",
                "contact",
                "addressLine1", "addressLine2",
                "city", "state", "country", "postalCode",
                "phoneNumber", "faxNumber", "email",
                "websiteUrl");
    }
}
