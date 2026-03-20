using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class ShippingModule : IEbsModule
{
    public string ModuleName => "Shipping";

    public void Register(EbsRegistry registry)
    {
        // Delivery Legs (R12+)
        registry.Table("WSH_DELIVERY_LEGS", "WSH_DELIVERY_LEGS", "WSH", "Shipping")
            .Column("DELIVERY_LEG_ID", since: EbsVersion.R12)
            .Column("DELIVERY_ID", since: EbsVersion.R12)
            .Column("TRIP_ID", since: EbsVersion.R12)
            .Column("TRIP_STOP_ID", since: EbsVersion.R12)
            .Column("FROM_LOCATION_ID", since: EbsVersion.R12)
            .Column("TO_LOCATION_ID", since: EbsVersion.R12)
            .Column("SCHEDULED_DELIVERY_DATE", since: EbsVersion.R12)
            .Column("ACTUAL_DELIVERY_DATE", since: EbsVersion.R12)
            .Column("DELIVERY_LEG_NUMBER", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("WSH_DELIVERY_DETAILS", "DELIVERY_LEG_ID", "DELIVERY_LEG_ID", since: EbsVersion.R12);

        // Delivery Details (R12+)
        registry.Table("WSH_DELIVERY_DETAILS", "WSH_DELIVERY_DETAILS", "WSH", "Shipping")
            .Column("DELIVERY_DETAIL_ID", since: EbsVersion.R12)
            .Column("DELIVERY_ID", since: EbsVersion.R12)
            .Column("DELIVERY_LEG_ID", since: EbsVersion.R12)
            .Column("SOURCE_LINE_ID", since: EbsVersion.R12)
            .Column("REQUESTED_QUANTITY", since: EbsVersion.R12)
            .Column("SHIPPED_QUANTITY", since: EbsVersion.R12)
            .Column("RECEIVED_QUANTITY", since: EbsVersion.R12)
            .Column("ITEM_ID", since: EbsVersion.R12)
            .Column("UOM_CODE", since: EbsVersion.R12)
            .Column("LINE_STATUS_CODE", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("WSH_DELIVERY_LEGS", "DELIVERY_LEG_ID", "DELIVERY_LEG_ID", since: EbsVersion.R12)
            .JoinTo("OE_ORDER_LINES_ALL", "SOURCE_LINE_ID", "LINE_ID", since: EbsVersion.R12)
            .JoinTo("MTL_SYSTEM_ITEMS_B", "ITEM_ID", "INVENTORY_ITEM_ID", since: EbsVersion.R12);

        // Trip Stops (R12+)
        registry.Table("WSH_TRIP_STOPS", "WSH_TRIP_STOPS", "WSH", "Shipping")
            .Column("TRIP_STOP_ID", since: EbsVersion.R12)
            .Column("TRIP_ID", since: EbsVersion.R12)
            .Column("STOP_SEQUENCE_NUMBER", since: EbsVersion.R12)
            .Column("STOP_LOCATION_ID", since: EbsVersion.R12)
            .Column("SCHEDULED_ARRIVAL_DATE", since: EbsVersion.R12)
            .Column("ACTUAL_ARRIVAL_DATE", since: EbsVersion.R12)
            .Column("STOP_STATUS_CODE", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("WSH_DELIVERY_LEGS", "TRIP_STOP_ID", "TRIP_STOP_ID", since: EbsVersion.R12);
    }
}
