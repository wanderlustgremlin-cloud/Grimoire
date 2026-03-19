namespace Grimoire.Connector.OracleEbs;

public sealed class EbsModuleSelector
{
    internal HashSet<string> SelectedModules { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal HashSet<string>? SelectedTables { get; private set; }

    public EbsModuleSelector HR(Action<EbsTableSelector>? configure = null)
        => SelectModule("HR", configure);

    public EbsModuleSelector AP(Action<EbsTableSelector>? configure = null)
        => SelectModule("AP", configure);

    public EbsModuleSelector AR(Action<EbsTableSelector>? configure = null)
        => SelectModule("AR", configure);

    public EbsModuleSelector GL(Action<EbsTableSelector>? configure = null)
        => SelectModule("GL", configure);

    public EbsModuleSelector PO(Action<EbsTableSelector>? configure = null)
        => SelectModule("PO", configure);

    public EbsModuleSelector INV(Action<EbsTableSelector>? configure = null)
        => SelectModule("INV", configure);

    private EbsModuleSelector SelectModule(string module, Action<EbsTableSelector>? configure)
    {
        SelectedModules.Add(module);

        if (configure is not null)
        {
            SelectedTables ??= new(StringComparer.OrdinalIgnoreCase);
            var tableSelector = new EbsTableSelector(module);
            configure(tableSelector);
            foreach (var table in tableSelector.Tables)
                SelectedTables.Add(table);
        }

        return this;
    }
}

public sealed class EbsTableSelector
{
    private readonly string _module;
    internal HashSet<string> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal EbsTableSelector(string module)
    {
        _module = module;
    }

    public EbsTableSelector Table(string appsViewName)
    {
        Tables.Add(appsViewName);
        return this;
    }

    // HR convenience methods
    public EbsTableSelector People() => Table("PER_ALL_PEOPLE_F");
    public EbsTableSelector Assignments() => Table("PER_ALL_ASSIGNMENTS_F");
    public EbsTableSelector Organizations() => Table("HR_ALL_ORGANIZATION_UNITS");
    public EbsTableSelector Locations() => Table("HR_LOCATIONS_ALL");
    public EbsTableSelector Jobs() => Table("PER_JOBS");
    public EbsTableSelector Positions() => Table("PER_ALL_POSITIONS").Table("HR_ALL_POSITIONS_F");
    public EbsTableSelector Grades() => Table("PER_GRADES");
    public EbsTableSelector PersonTypes() => Table("PER_PERSON_TYPES");

    // AP convenience methods
    public EbsTableSelector Suppliers() => Table("PO_VENDORS").Table("AP_SUPPLIERS");
    public EbsTableSelector SupplierSites() => Table("PO_VENDOR_SITES_ALL").Table("AP_SUPPLIER_SITES_ALL");
    public EbsTableSelector Invoices() => Table("AP_INVOICES_ALL");
    public EbsTableSelector InvoiceLines() => Table("AP_INVOICE_LINES_ALL");
    public EbsTableSelector InvoiceDistributions() => Table("AP_INVOICE_DISTRIBUTIONS_ALL");
    public EbsTableSelector Payments() => Table("AP_CHECKS_ALL");
    public EbsTableSelector PaymentTerms() => Table("AP_TERMS");

    // AR convenience methods
    public EbsTableSelector Customers() => Table("RA_CUSTOMERS").Table("HZ_PARTIES").Table("HZ_CUST_ACCOUNTS");
    public EbsTableSelector CustomerSiteUses() => Table("HZ_CUST_SITE_USES_ALL");
    public EbsTableSelector Transactions() => Table("RA_CUSTOMER_TRX_ALL");
    public EbsTableSelector TransactionLines() => Table("RA_CUSTOMER_TRX_LINES_ALL");
    public EbsTableSelector Receipts() => Table("AR_CASH_RECEIPTS_ALL");

    // GL convenience methods
    public EbsTableSelector JournalHeaders() => Table("GL_JE_HEADERS");
    public EbsTableSelector JournalLines() => Table("GL_JE_LINES");
    public EbsTableSelector JournalBatches() => Table("GL_JE_BATCHES");
    public EbsTableSelector CodeCombinations() => Table("GL_CODE_COMBINATIONS");
    public EbsTableSelector Balances() => Table("GL_BALANCES");
    public EbsTableSelector Ledgers() => Table("GL_SETS_OF_BOOKS").Table("GL_LEDGERS");

    // PO convenience methods
    public EbsTableSelector PurchaseOrders() => Table("PO_HEADERS_ALL");
    public EbsTableSelector PurchaseOrderLines() => Table("PO_LINES_ALL");
    public EbsTableSelector LineLocations() => Table("PO_LINE_LOCATIONS_ALL");
    public EbsTableSelector Distributions() => Table("PO_DISTRIBUTIONS_ALL");
    public EbsTableSelector Requisitions() => Table("PO_REQUISITION_HEADERS_ALL");
    public EbsTableSelector RequisitionLines() => Table("PO_REQUISITION_LINES_ALL");

    // INV convenience methods
    public EbsTableSelector Items() => Table("MTL_SYSTEM_ITEMS_B");
    public EbsTableSelector ItemCategories() => Table("MTL_ITEM_CATEGORIES");
    public EbsTableSelector Categories() => Table("MTL_CATEGORIES_B");
    public EbsTableSelector OnHandQuantities() => Table("MTL_ONHAND_QUANTITIES_DETAIL");
    public EbsTableSelector MaterialTransactions() => Table("MTL_MATERIAL_TRANSACTIONS");
    public EbsTableSelector InventoryOrganizations() => Table("MTL_PARAMETERS");
    public EbsTableSelector Subinventories() => Table("MTL_SECONDARY_INVENTORIES");
}
