using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class ArModule : IEbsModule
{
    public string ModuleName => "AR";

    public void Register(EbsRegistry registry)
    {
        // Customers (R11i: RA_CUSTOMERS, R12+: HZ_PARTIES/HZ_CUST_ACCOUNTS)
        registry.Table("RA_CUSTOMERS", "RA_CUSTOMERS", "AR", "AR")
            .Columns("CUSTOMER_ID", "CUSTOMER_NAME", "CUSTOMER_NUMBER",
                "CUSTOMER_TYPE", "STATUS",
                "CUSTOMER_CLASS_CODE", "CREDIT_HOLD")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // TCA Party model (R12+)
        registry.Table("HZ_PARTIES", "HZ_PARTIES", "AR", "AR")
            .Column("PARTY_ID", since: EbsVersion.R12)
            .Column("PARTY_NAME", since: EbsVersion.R12)
            .Column("PARTY_NUMBER", since: EbsVersion.R12)
            .Column("PARTY_TYPE", since: EbsVersion.R12)
            .Column("STATUS", since: EbsVersion.R12)
            .Column("EMAIL_ADDRESS", since: EbsVersion.R12)
            .Column("URL", since: EbsVersion.R12)
            .Column("COUNTRY", since: EbsVersion.R12)
            .Column("DUNS_NUMBER", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("HZ_CUST_ACCOUNTS", "PARTY_ID", "PARTY_ID", since: EbsVersion.R12);

        registry.Table("HZ_CUST_ACCOUNTS", "HZ_CUST_ACCOUNTS", "AR", "AR")
            .Column("CUST_ACCOUNT_ID", since: EbsVersion.R12)
            .Column("PARTY_ID", since: EbsVersion.R12)
            .Column("ACCOUNT_NUMBER", since: EbsVersion.R12)
            .Column("ACCOUNT_NAME", since: EbsVersion.R12)
            .Column("STATUS", since: EbsVersion.R12)
            .Column("CUSTOMER_TYPE", since: EbsVersion.R12)
            .Column("CUSTOMER_CLASS_CODE", since: EbsVersion.R12)
            .Column("CREDIT_HOLD", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("HZ_PARTIES", "PARTY_ID", "PARTY_ID", since: EbsVersion.R12)
            .JoinTo("HZ_CUST_SITE_USES_ALL", "CUST_ACCOUNT_ID", "CUST_ACCOUNT_ID", since: EbsVersion.R12);

        // Customer Site Uses
        registry.Table("HZ_CUST_SITE_USES_ALL", "HZ_CUST_SITE_USES_ALL", "AR", "AR")
            .Column("SITE_USE_ID", since: EbsVersion.R12)
            .Column("CUST_ACCT_SITE_ID", since: EbsVersion.R12)
            .Column("CUST_ACCOUNT_ID", since: EbsVersion.R12)
            .Column("SITE_USE_CODE", since: EbsVersion.R12)
            .Column("PRIMARY_FLAG", since: EbsVersion.R12)
            .Column("STATUS", since: EbsVersion.R12)
            .Column("LOCATION", since: EbsVersion.R12)
            .Column("ORG_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12);

        // Transactions
        registry.Table("RA_CUSTOMER_TRX_ALL", "RA_CUSTOMER_TRX_ALL", "RA", "AR")
            .Columns("CUSTOMER_TRX_ID", "TRX_NUMBER", "TRX_DATE",
                "CUST_TRX_TYPE_ID", "BILL_TO_CUSTOMER_ID",
                "BILL_TO_SITE_USE_ID", "SHIP_TO_CUSTOMER_ID",
                "INVOICE_CURRENCY_CODE", "EXCHANGE_RATE",
                "TERM_ID", "PRIMARY_SALESREP_ID",
                "COMPLETE_FLAG", "STATUS_TRX",
                "ORG_ID", "SET_OF_BOOKS_ID")
            .Column("BILL_TO_PARTY_ID", since: EbsVersion.R12)
            .Column("LEGAL_ENTITY_ID", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("RA_CUSTOMER_TRX_LINES_ALL", "CUSTOMER_TRX_ID", "CUSTOMER_TRX_ID");

        // Transaction Lines
        registry.Table("RA_CUSTOMER_TRX_LINES_ALL", "RA_CUSTOMER_TRX_LINES_ALL", "RA", "AR")
            .Columns("CUSTOMER_TRX_LINE_ID", "CUSTOMER_TRX_ID",
                "LINE_NUMBER", "LINE_TYPE",
                "QUANTITY_INVOICED", "UNIT_SELLING_PRICE",
                "EXTENDED_AMOUNT", "DESCRIPTION",
                "INVENTORY_ITEM_ID", "UOM_CODE",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("RA_CUSTOMER_TRX_ALL", "CUSTOMER_TRX_ID", "CUSTOMER_TRX_ID");

        // Receipts
        registry.Table("AR_CASH_RECEIPTS_ALL", "AR_CASH_RECEIPTS_ALL", "AR", "AR")
            .Columns("CASH_RECEIPT_ID", "RECEIPT_NUMBER", "RECEIPT_DATE",
                "AMOUNT", "CURRENCY_CODE",
                "PAY_FROM_CUSTOMER", "CUSTOMER_SITE_USE_ID",
                "STATUS", "TYPE",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");
    }
}
