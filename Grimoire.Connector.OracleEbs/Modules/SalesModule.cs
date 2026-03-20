using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class SalesModule : IEbsModule
{
    public string ModuleName => "Sales";

    public void Register(EbsRegistry registry)
    {
        // Order Headers
        registry.Table("OE_ORDER_HEADERS_ALL", "OE_ORDER_HEADERS_ALL", "ONT", "Sales")
            .Columns("ORDER_HEADER_ID", "ORDER_NUMBER", "ORDERED_DATE",
                "SOLD_TO_ORG_ID", "SHIP_TO_ORG_ID",
                "ORDER_TYPE_LOOKUP_CODE", "COMPLETE_FLAG", "BOOKED_FLAG",
                "TOTAL_BASE_VALUE", "TOTAL_FREIGHT", "TOTAL_TAX",
                "ORG_ID", "SET_OF_BOOKS_ID")
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("OE_ORDER_LINES_ALL", "ORDER_HEADER_ID", "ORDER_HEADER_ID")
            .JoinTo("HZ_PARTIES", "SOLD_TO_ORG_ID", "PARTY_ID");

        // Order Lines
        registry.Table("OE_ORDER_LINES_ALL", "OE_ORDER_LINES_ALL", "ONT", "Sales")
            .Columns("ORDER_HEADER_ID", "LINE_ID", "LINE_NUMBER",
                "INVENTORY_ITEM_ID", "ORDERED_QUANTITY", "UOM_CODE",
                "UNIT_SELLING_PRICE", "EXTENDED_AMOUNT",
                "SCHEDULE_SHIP_DATE", "ACTUAL_SHIPMENT_DATE",
                "LINE_STATUS_CODE", "FULFILLED_FLAG",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("OE_ORDER_HEADERS_ALL", "ORDER_HEADER_ID", "ORDER_HEADER_ID")
            .JoinTo("MTL_SYSTEM_ITEMS_B", "INVENTORY_ITEM_ID", "INVENTORY_ITEM_ID");

        // Quote Headers (R12+)
        registry.Table("OE_QUOTE_HEADERS_ALL", "OE_QUOTE_HEADERS_ALL", "ONT", "Sales")
            .Column("QUOTE_HEADER_ID", since: EbsVersion.R12)
            .Column("QUOTE_NUMBER", since: EbsVersion.R12)
            .Column("QUOTE_DATE", since: EbsVersion.R12)
            .Column("SOLD_TO_ORG_ID", since: EbsVersion.R12)
            .Column("SHIP_TO_ORG_ID", since: EbsVersion.R12)
            .Column("QUOTE_STATUS_CODE", since: EbsVersion.R12)
            .Column("QUOTE_EXPIRATION_DATE", since: EbsVersion.R12)
            .Column("QUOTE_TOTAL", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("OE_QUOTE_LINES_ALL", "QUOTE_HEADER_ID", "QUOTE_HEADER_ID", since: EbsVersion.R12);

        // Quote Lines (R12+)
        registry.Table("OE_QUOTE_LINES_ALL", "OE_QUOTE_LINES_ALL", "ONT", "Sales")
            .Column("QUOTE_HEADER_ID", since: EbsVersion.R12)
            .Column("QUOTE_LINE_ID", since: EbsVersion.R12)
            .Column("LINE_NUMBER", since: EbsVersion.R12)
            .Column("INVENTORY_ITEM_ID", since: EbsVersion.R12)
            .Column("ORDERED_QUANTITY", since: EbsVersion.R12)
            .Column("UOM_CODE", since: EbsVersion.R12)
            .Column("UNIT_SELLING_PRICE", since: EbsVersion.R12)
            .Column("EXTENDED_AMOUNT", since: EbsVersion.R12)
            .Column("LINE_STATUS_CODE", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("OE_QUOTE_HEADERS_ALL", "QUOTE_HEADER_ID", "QUOTE_HEADER_ID", since: EbsVersion.R12);

        // Pick Slips (R11i+ warehouse picking workflow before WSH module)
        registry.Table("OE_PICK_SLIPS", "OE_PICK_SLIPS", "ONT", "Sales")
            .Columns("PICK_SLIP_NUMBER", "ORDER_HEADER_ID", "ORGANIZATION_ID",
                "PICK_SLIP_TYPE", "STATUS_CODE", "PICK_RELEASE_ID",
                "PICK_SLIP_DATE", "ORG_ID")
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("OE_PICK_SLIP_LINES", "PICK_SLIP_NUMBER", "PICK_SLIP_NUMBER")
            .JoinTo("OE_ORDER_HEADERS_ALL", "ORDER_HEADER_ID", "ORDER_HEADER_ID");

        // Pick Slip Lines (R11i+ warehouse picking line detail)
        registry.Table("OE_PICK_SLIP_LINES", "OE_PICK_SLIP_LINES", "ONT", "Sales")
            .Columns("PICK_SLIP_LINE_ID", "PICK_SLIP_NUMBER", "ORDER_LINE_ID",
                "INVENTORY_ITEM_ID", "UOM_CODE",
                "ORDERED_QTY", "PICKED_QTY", "SHIPPED_QTY",
                "SUBINVENTORY_CODE", "LOCATOR_ID",
                "LOT_NUMBER", "SERIAL_NUMBER",
                "LINE_STATUS_CODE", "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("OE_PICK_SLIPS", "PICK_SLIP_NUMBER", "PICK_SLIP_NUMBER")
            .JoinTo("OE_ORDER_LINES_ALL", "ORDER_LINE_ID", "LINE_ID")
            .JoinTo("MTL_SYSTEM_ITEMS_B", "INVENTORY_ITEM_ID", "INVENTORY_ITEM_ID");
    }
}
