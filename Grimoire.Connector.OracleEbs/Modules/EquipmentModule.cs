using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class EquipmentModule : IEbsModule
{
    public string ModuleName => "Equipment";

    public void Register(EbsRegistry registry)
    {
        // Custom Equipment Master (R11i+ custom table pattern)
        registry.Table("CUSTOM_EQUIPMENT_MASTER", "CUSTOM_EQUIPMENT_MASTER", "Custom", "Equipment")
            .Columns("EQUIPMENT_ID", "EQUIPMENT_NUMBER", "DESCRIPTION",
                "EQUIPMENT_CLASS_CODE", "ASSET_ID",
                "MANUFACTURER_CODE", "MODEL_CODE", "SERIAL_NUMBER",
                "INSTALLATION_DATE", "CURRENT_LOCATION_ID",
                "CURRENT_OWNER_PERSON_ID",
                "STATUS_CODE",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("CUSTOM_MAINTENANCE_LOG", "EQUIPMENT_ID", "EQUIPMENT_ID")
            .JoinTo("CUSTOM_MAINTENANCE_SCHEDULE", "EQUIPMENT_ID", "EQUIPMENT_ID")
            .JoinTo("CUSTOM_EQUIPMENT_HISTORY", "EQUIPMENT_ID", "EQUIPMENT_ID")
            .JoinTo("FA_ADDITIONS", "ASSET_ID", "ASSET_ID");

        // Custom Maintenance Log (Transaction Ledger)
        registry.Table("CUSTOM_MAINTENANCE_LOG", "CUSTOM_MAINTENANCE_LOG", "Custom", "Equipment")
            .Columns("MAINTENANCE_ID", "EQUIPMENT_ID",
                "MAINTENANCE_TYPE_CODE",
                "SCHEDULED_START_DATE", "ACTUAL_START_DATE", "ACTUAL_END_DATE",
                "DESCRIPTION", "ACTION_TAKEN",
                "LABOR_HOURS", "LABOR_COST",
                "MATERIALS_COST", "TOTAL_MAINTENANCE_COST",
                "TECHNICIAN_PERSON_ID",
                "STATUS_CODE",
                "NEXT_MAINTENANCE_DUE",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("CUSTOM_EQUIPMENT_MASTER", "EQUIPMENT_ID", "EQUIPMENT_ID")
            .JoinTo("PER_ALL_PEOPLE_F", "TECHNICIAN_PERSON_ID", "PERSON_ID")
            .JoinTo("CUSTOM_MAINTENANCE_PARTS", "MAINTENANCE_ID", "MAINTENANCE_ID");

        // Custom Maintenance Parts (BOM Detail)
        registry.Table("CUSTOM_MAINTENANCE_PARTS", "CUSTOM_MAINTENANCE_PARTS", "Custom", "Equipment")
            .Columns("MAINTENANCE_PART_ID", "MAINTENANCE_ID", "INVENTORY_ITEM_ID",
                "PART_NUMBER", "DESCRIPTION",
                "QUANTITY_USED", "UOM_CODE",
                "UNIT_COST", "TOTAL_PART_COST",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("CUSTOM_MAINTENANCE_LOG", "MAINTENANCE_ID", "MAINTENANCE_ID")
            .JoinTo("MTL_SYSTEM_ITEMS_B", "INVENTORY_ITEM_ID", "INVENTORY_ITEM_ID");

        // Custom Maintenance Schedule (Preventive Maintenance Rules)
        registry.Table("CUSTOM_MAINTENANCE_SCHEDULE", "CUSTOM_MAINTENANCE_SCHEDULE", "Custom", "Equipment")
            .Columns("SCHEDULE_ID", "EQUIPMENT_ID",
                "SCHEDULE_NAME", "FREQUENCY_CODE",
                "FREQUENCY_VALUE",
                "LAST_MAINTENANCE_DATE", "NEXT_DUE_DATE",
                "TASK_DESCRIPTION", "ESTIMATED_HOURS",
                "ENABLED_FLAG",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("CUSTOM_EQUIPMENT_MASTER", "EQUIPMENT_ID", "EQUIPMENT_ID");

        // Custom Equipment History (Location/Status Changes)
        registry.Table("CUSTOM_EQUIPMENT_HISTORY", "CUSTOM_EQUIPMENT_HISTORY", "Custom", "Equipment")
            .Columns("HISTORY_ID", "EQUIPMENT_ID",
                "TRANSACTION_TYPE_CODE",
                "FROM_LOCATION_ID", "TO_LOCATION_ID",
                "FROM_OWNER_PERSON_ID", "TO_OWNER_PERSON_ID",
                "PREV_STATUS_CODE", "NEW_STATUS_CODE",
                "TRANSACTION_DATE",
                "DESCRIPTION",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("CUSTOM_EQUIPMENT_MASTER", "EQUIPMENT_ID", "EQUIPMENT_ID");
    }
}
