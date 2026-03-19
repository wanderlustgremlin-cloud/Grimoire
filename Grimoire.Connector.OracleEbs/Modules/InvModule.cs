using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class InvModule : IEbsModule
{
    public string ModuleName => "INV";

    public void Register(EbsRegistry registry)
    {
        // Items Master
        registry.Table("MTL_SYSTEM_ITEMS_B", "MTL_SYSTEM_ITEMS_B", "INV", "INV")
            .Columns("INVENTORY_ITEM_ID", "ORGANIZATION_ID",
                "SEGMENT1", "DESCRIPTION", "LONG_DESCRIPTION",
                "PRIMARY_UOM_CODE", "PRIMARY_UNIT_OF_MEASURE",
                "ITEM_TYPE", "INVENTORY_ITEM_FLAG",
                "PURCHASING_ITEM_FLAG", "CUSTOMER_ORDER_FLAG",
                "INTERNAL_ORDER_FLAG", "BOM_ITEM_TYPE",
                "ENABLED_FLAG", "START_DATE_ACTIVE", "END_DATE_ACTIVE",
                "LIST_PRICE_PER_UNIT", "UNIT_WEIGHT", "WEIGHT_UOM_CODE",
                "INVENTORY_ITEM_STATUS_CODE")
            .Column("STYLE_ITEM_FLAG", since: EbsVersion.R122)
            .Column("STYLE_ITEM_ID", since: EbsVersion.R122)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("MTL_ITEM_CATEGORIES", "INVENTORY_ITEM_ID", "INVENTORY_ITEM_ID");

        // Item Categories
        registry.Table("MTL_ITEM_CATEGORIES", "MTL_ITEM_CATEGORIES", "INV", "INV")
            .Columns("INVENTORY_ITEM_ID", "ORGANIZATION_ID",
                "CATEGORY_SET_ID", "CATEGORY_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("MTL_CATEGORIES_B", "CATEGORY_ID", "CATEGORY_ID");

        // Categories
        registry.Table("MTL_CATEGORIES_B", "MTL_CATEGORIES_B", "INV", "INV")
            .Columns("CATEGORY_ID", "STRUCTURE_ID",
                "SEGMENT1", "SEGMENT2", "SEGMENT3", "SEGMENT4", "SEGMENT5",
                "ENABLED_FLAG", "START_DATE_ACTIVE", "END_DATE_ACTIVE",
                "DESCRIPTION")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // On-Hand Quantities
        registry.Table("MTL_ONHAND_QUANTITIES_DETAIL", "MTL_ONHAND_QUANTITIES_DETAIL", "INV", "INV")
            .Columns("INVENTORY_ITEM_ID", "ORGANIZATION_ID",
                "SUBINVENTORY_CODE", "LOCATOR_ID",
                "LOT_NUMBER", "REVISION",
                "PRIMARY_TRANSACTION_QUANTITY",
                "TRANSACTION_UOM_CODE",
                "DATE_RECEIVED", "CREATE_TRANSACTION_ID")
            .Column("SECONDARY_TRANSACTION_QUANTITY", since: EbsVersion.R12)
            .Column("SECONDARY_UOM_CODE", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("MTL_SYSTEM_ITEMS_B", "INVENTORY_ITEM_ID", "INVENTORY_ITEM_ID");

        // Material Transactions
        registry.Table("MTL_MATERIAL_TRANSACTIONS", "MTL_MATERIAL_TRANSACTIONS", "INV", "INV")
            .Columns("TRANSACTION_ID", "INVENTORY_ITEM_ID", "ORGANIZATION_ID",
                "SUBINVENTORY_CODE", "TRANSACTION_TYPE_ID",
                "TRANSACTION_ACTION_ID", "TRANSACTION_SOURCE_TYPE_ID",
                "TRANSACTION_DATE", "TRANSACTION_QUANTITY",
                "TRANSACTION_UOM", "PRIMARY_QUANTITY",
                "TRANSACTION_COST", "ACTUAL_COST",
                "TRANSFER_ORGANIZATION_ID", "TRANSFER_SUBINVENTORY",
                "SOURCE_CODE", "SOURCE_LINE_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("MTL_SYSTEM_ITEMS_B", "INVENTORY_ITEM_ID", "INVENTORY_ITEM_ID");

        // Organizations (Inventory Orgs)
        registry.Table("MTL_PARAMETERS", "MTL_PARAMETERS", "INV", "INV")
            .Columns("ORGANIZATION_ID", "ORGANIZATION_CODE",
                "MASTER_ORGANIZATION_ID",
                "PRIMARY_COST_METHOD", "COST_ORGANIZATION_ID",
                "DEFAULT_MATERIAL_COST_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Subinventories
        registry.Table("MTL_SECONDARY_INVENTORIES", "MTL_SECONDARY_INVENTORIES", "INV", "INV")
            .Columns("SECONDARY_INVENTORY_NAME", "ORGANIZATION_ID",
                "DESCRIPTION", "DISABLE_DATE",
                "INVENTORY_ATP_CODE", "AVAILABILITY_TYPE",
                "RESERVABLE_TYPE", "ASSET_INVENTORY",
                "QUANTITY_TRACKED", "LOCATOR_TYPE")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");
    }
}
