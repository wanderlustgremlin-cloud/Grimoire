using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class PoModule : IEbsModule
{
    public string ModuleName => "PO";

    public void Register(EbsRegistry registry)
    {
        // Purchase Order Headers
        registry.Table("PO_HEADERS_ALL", "PO_HEADERS_ALL", "PO", "PO")
            .Columns("PO_HEADER_ID", "SEGMENT1", "TYPE_LOOKUP_CODE",
                "VENDOR_ID", "VENDOR_SITE_ID", "VENDOR_CONTACT_ID",
                "AGENT_ID", "CREATION_DATE",
                "CURRENCY_CODE", "RATE", "RATE_TYPE", "RATE_DATE",
                "AUTHORIZATION_STATUS", "APPROVED_FLAG", "APPROVED_DATE",
                "CANCEL_FLAG", "CLOSED_CODE", "CLOSED_DATE",
                "TERMS_ID", "SHIP_TO_LOCATION_ID", "BILL_TO_LOCATION_ID",
                "ORG_ID", "COMMENTS")
            .Column("STYLE_ID", since: EbsVersion.R122)
            .Columns("CREATED_BY", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_LINES_ALL", "PO_HEADER_ID", "PO_HEADER_ID");

        // Purchase Order Lines
        registry.Table("PO_LINES_ALL", "PO_LINES_ALL", "PO", "PO")
            .Columns("PO_LINE_ID", "PO_HEADER_ID", "LINE_NUM",
                "LINE_TYPE_ID", "ITEM_ID", "ITEM_DESCRIPTION",
                "CATEGORY_ID", "UNIT_MEAS_LOOKUP_CODE",
                "QUANTITY", "UNIT_PRICE", "AMOUNT",
                "CANCEL_FLAG", "CLOSED_CODE",
                "ORG_ID")
            .Column("ORDER_TYPE_LOOKUP_CODE", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_HEADERS_ALL", "PO_HEADER_ID", "PO_HEADER_ID")
            .JoinTo("PO_LINE_LOCATIONS_ALL", "PO_LINE_ID", "PO_LINE_ID");

        // Line Locations (Shipments)
        registry.Table("PO_LINE_LOCATIONS_ALL", "PO_LINE_LOCATIONS_ALL", "PO", "PO")
            .Columns("LINE_LOCATION_ID", "PO_HEADER_ID", "PO_LINE_ID",
                "QUANTITY", "QUANTITY_RECEIVED", "QUANTITY_ACCEPTED", "QUANTITY_REJECTED",
                "QUANTITY_BILLED", "QUANTITY_CANCELLED",
                "SHIP_TO_LOCATION_ID", "SHIP_TO_ORGANIZATION_ID",
                "NEED_BY_DATE", "PROMISED_DATE",
                "CANCEL_FLAG", "CLOSED_CODE",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_LINES_ALL", "PO_LINE_ID", "PO_LINE_ID")
            .JoinTo("PO_DISTRIBUTIONS_ALL", "LINE_LOCATION_ID", "LINE_LOCATION_ID");

        // Distributions
        registry.Table("PO_DISTRIBUTIONS_ALL", "PO_DISTRIBUTIONS_ALL", "PO", "PO")
            .Columns("PO_DISTRIBUTION_ID", "PO_HEADER_ID", "PO_LINE_ID", "LINE_LOCATION_ID",
                "DISTRIBUTION_NUM", "QUANTITY_ORDERED", "QUANTITY_DELIVERED",
                "QUANTITY_BILLED", "QUANTITY_CANCELLED",
                "CODE_COMBINATION_ID", "BUDGET_ACCOUNT_ID",
                "DELIVER_TO_LOCATION_ID", "DELIVER_TO_PERSON_ID",
                "SET_OF_BOOKS_ID", "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_LINE_LOCATIONS_ALL", "LINE_LOCATION_ID", "LINE_LOCATION_ID");

        // Requisition Headers
        registry.Table("PO_REQUISITION_HEADERS_ALL", "PO_REQUISITION_HEADERS_ALL", "PO", "PO")
            .Columns("REQUISITION_HEADER_ID", "SEGMENT1",
                "PREPARER_ID", "DESCRIPTION",
                "AUTHORIZATION_STATUS", "TYPE_LOOKUP_CODE",
                "TRANSFERRED_TO_OE_FLAG",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_REQUISITION_LINES_ALL", "REQUISITION_HEADER_ID", "REQUISITION_HEADER_ID");

        // Requisition Lines
        registry.Table("PO_REQUISITION_LINES_ALL", "PO_REQUISITION_LINES_ALL", "PO", "PO")
            .Columns("REQUISITION_LINE_ID", "REQUISITION_HEADER_ID",
                "LINE_NUM", "ITEM_ID", "ITEM_DESCRIPTION",
                "CATEGORY_ID", "UNIT_MEAS_LOOKUP_CODE",
                "QUANTITY", "UNIT_PRICE", "AMOUNT",
                "NEED_BY_DATE", "CANCEL_FLAG",
                "SUGGESTED_VENDOR_NAME", "SUGGESTED_VENDOR_SITE",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_REQUISITION_HEADERS_ALL", "REQUISITION_HEADER_ID", "REQUISITION_HEADER_ID");
    }
}
