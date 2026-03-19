using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class ApModule : IEbsModule
{
    public string ModuleName => "AP";

    public void Register(EbsRegistry registry)
    {
        // Suppliers (R11i: PO_VENDORS, R12+: AP_SUPPLIERS)
        registry.Table("PO_VENDORS", "PO_VENDORS", "PO", "AP")
            .Columns("VENDOR_ID", "VENDOR_NAME", "VENDOR_NAME_ALT", "SEGMENT1",
                "VENDOR_TYPE_LOOKUP_CODE", "ENABLED_FLAG", "START_DATE_ACTIVE", "END_DATE_ACTIVE",
                "PAYMENT_METHOD_LOOKUP_CODE", "PAYMENT_CURRENCY_CODE",
                "TERMS_ID", "SET_OF_BOOKS_ID")
            .Column("NUM_1099", since: EbsVersion.R11i)
            .Column("FEDERAL_REPORTABLE_FLAG", since: EbsVersion.R11i)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_VENDOR_SITES_ALL", "VENDOR_ID", "VENDOR_ID")
            .JoinTo("AP_TERMS", "TERMS_ID", "TERM_ID");

        registry.Table("AP_SUPPLIERS", "AP_SUPPLIERS", "AP", "AP")
            .Column("VENDOR_ID", since: EbsVersion.R12)
            .Column("VENDOR_NAME", since: EbsVersion.R12)
            .Column("VENDOR_NAME_ALT", since: EbsVersion.R12)
            .Column("SEGMENT1", since: EbsVersion.R12)
            .Column("VENDOR_TYPE_LOOKUP_CODE", since: EbsVersion.R12)
            .Column("ENABLED_FLAG", since: EbsVersion.R12)
            .Column("START_DATE_ACTIVE", since: EbsVersion.R12)
            .Column("END_DATE_ACTIVE", since: EbsVersion.R12)
            .Column("PARTY_ID", since: EbsVersion.R12)
            .Column("PAYMENT_METHOD_CODE", since: EbsVersion.R12)
            .Column("TERMS_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("AP_SUPPLIER_SITES_ALL", "VENDOR_ID", "VENDOR_ID", since: EbsVersion.R12)
            .JoinTo("AP_TERMS", "TERMS_ID", "TERM_ID", since: EbsVersion.R12);

        // Supplier Sites
        registry.Table("PO_VENDOR_SITES_ALL", "PO_VENDOR_SITES_ALL", "PO", "AP")
            .Columns("VENDOR_SITE_ID", "VENDOR_ID", "VENDOR_SITE_CODE",
                "ADDRESS_LINE1", "ADDRESS_LINE2", "ADDRESS_LINE3",
                "CITY", "STATE", "ZIP", "COUNTRY",
                "ORG_ID", "PAY_SITE_FLAG", "PURCHASING_SITE_FLAG",
                "INACTIVE_DATE")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PO_VENDORS", "VENDOR_ID", "VENDOR_ID");

        registry.Table("AP_SUPPLIER_SITES_ALL", "AP_SUPPLIER_SITES_ALL", "AP", "AP")
            .Column("VENDOR_SITE_ID", since: EbsVersion.R12)
            .Column("VENDOR_ID", since: EbsVersion.R12)
            .Column("VENDOR_SITE_CODE", since: EbsVersion.R12)
            .Column("ADDRESS_LINE1", since: EbsVersion.R12)
            .Column("ADDRESS_LINE2", since: EbsVersion.R12)
            .Column("ADDRESS_LINE3", since: EbsVersion.R12)
            .Column("CITY", since: EbsVersion.R12)
            .Column("STATE", since: EbsVersion.R12)
            .Column("ZIP", since: EbsVersion.R12)
            .Column("COUNTRY", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("PARTY_SITE_ID", since: EbsVersion.R12)
            .Column("PAY_SITE_FLAG", since: EbsVersion.R12)
            .Column("PURCHASING_SITE_FLAG", since: EbsVersion.R12)
            .Column("INACTIVE_DATE", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("AP_SUPPLIERS", "VENDOR_ID", "VENDOR_ID", since: EbsVersion.R12);

        // Invoices
        registry.Table("AP_INVOICES_ALL", "AP_INVOICES_ALL", "AP", "AP")
            .Columns("INVOICE_ID", "INVOICE_NUM", "INVOICE_TYPE_LOOKUP_CODE",
                "INVOICE_DATE", "VENDOR_ID", "VENDOR_SITE_ID",
                "INVOICE_AMOUNT", "INVOICE_CURRENCY_CODE", "EXCHANGE_RATE",
                "TERMS_ID", "DESCRIPTION", "SOURCE",
                "PAYMENT_STATUS_FLAG", "CANCELLED_DATE",
                "ORG_ID", "SET_OF_BOOKS_ID",
                "GL_DATE", "PAYMENT_METHOD_CODE")
            .Column("PARTY_ID", since: EbsVersion.R12)
            .Column("PARTY_SITE_ID", since: EbsVersion.R12)
            .Column("LEGAL_ENTITY_ID", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("AP_INVOICE_LINES_ALL", "INVOICE_ID", "INVOICE_ID", since: EbsVersion.R12)
            .JoinTo("AP_INVOICE_DISTRIBUTIONS_ALL", "INVOICE_ID", "INVOICE_ID");

        // Invoice Lines (R12+ only — R11i went directly to distributions)
        registry.Table("AP_INVOICE_LINES_ALL", "AP_INVOICE_LINES_ALL", "AP", "AP")
            .Column("INVOICE_ID", since: EbsVersion.R12)
            .Column("LINE_NUMBER", since: EbsVersion.R12)
            .Column("LINE_TYPE_LOOKUP_CODE", since: EbsVersion.R12)
            .Column("AMOUNT", since: EbsVersion.R12)
            .Column("DESCRIPTION", since: EbsVersion.R12)
            .Column("ACCOUNTING_DATE", since: EbsVersion.R12)
            .Column("QUANTITY_INVOICED", since: EbsVersion.R12)
            .Column("UNIT_PRICE", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("AP_INVOICES_ALL", "INVOICE_ID", "INVOICE_ID", since: EbsVersion.R12)
            .JoinTo("AP_INVOICE_DISTRIBUTIONS_ALL", "INVOICE_ID", "INVOICE_ID", since: EbsVersion.R12);

        // Invoice Distributions
        registry.Table("AP_INVOICE_DISTRIBUTIONS_ALL", "AP_INVOICE_DISTRIBUTIONS_ALL", "AP", "AP")
            .Columns("INVOICE_DISTRIBUTION_ID", "INVOICE_ID",
                "DISTRIBUTION_LINE_NUMBER", "LINE_TYPE_LOOKUP_CODE",
                "AMOUNT", "DIST_CODE_COMBINATION_ID",
                "ACCOUNTING_DATE", "PERIOD_NAME",
                "SET_OF_BOOKS_ID", "ORG_ID")
            .Column("INVOICE_LINE_NUMBER", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("AP_INVOICES_ALL", "INVOICE_ID", "INVOICE_ID");

        // Payment Terms
        registry.Table("AP_TERMS", "AP_TERMS", "AP", "AP")
            .Columns("TERM_ID", "NAME", "DESCRIPTION",
                "ENABLED_FLAG", "START_DATE_ACTIVE", "END_DATE_ACTIVE")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Payments
        registry.Table("AP_CHECKS_ALL", "AP_CHECKS_ALL", "AP", "AP")
            .Columns("CHECK_ID", "CHECK_NUMBER", "CHECK_DATE",
                "AMOUNT", "CURRENCY_CODE", "VENDOR_ID", "VENDOR_SITE_ID",
                "BANK_ACCOUNT_ID", "STATUS_LOOKUP_CODE",
                "ORG_ID")
            .Column("PAYMENT_METHOD_CODE", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");
    }
}
