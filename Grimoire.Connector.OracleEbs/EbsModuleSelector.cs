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

    public EbsModuleSelector Sales(Action<EbsTableSelector>? configure = null)
        => SelectModule("Sales", configure);

    public EbsModuleSelector Shipping(Action<EbsTableSelector>? configure = null)
        => SelectModule("Shipping", configure);

    public EbsModuleSelector FixedAssets(Action<EbsTableSelector>? configure = null)
        => SelectModule("FixedAssets", configure);

    public EbsModuleSelector EAM(Action<EbsTableSelector>? configure = null)
        => SelectModule("EAM", configure);

    public EbsModuleSelector Equipment(Action<EbsTableSelector>? configure = null)
        => SelectModule("Equipment", configure);

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

    // Sales convenience methods
    public EbsTableSelector Orders() => Table("OE_ORDER_HEADERS_ALL");
    public EbsTableSelector OrderLines() => Table("OE_ORDER_LINES_ALL");
    public EbsTableSelector Quotes() => Table("OE_QUOTE_HEADERS_ALL");
    public EbsTableSelector QuoteLines() => Table("OE_QUOTE_LINES_ALL");
    public EbsTableSelector PickSlips() => Table("OE_PICK_SLIPS");
    public EbsTableSelector PickSlipLines() => Table("OE_PICK_SLIP_LINES");

    // Shipping convenience methods
    public EbsTableSelector DeliveryLegs() => Table("WSH_DELIVERY_LEGS");
    public EbsTableSelector DeliveryDetails() => Table("WSH_DELIVERY_DETAILS");
    public EbsTableSelector TripStops() => Table("WSH_TRIP_STOPS");

    // Fixed Assets convenience methods
    public EbsTableSelector AssetMasters() => Table("FA_ADDITIONS");
    public EbsTableSelector AssetHistory() => Table("FA_ASSET_HISTORY");
    public EbsTableSelector Retirements() => Table("FA_RETIREMENTS");
    public EbsTableSelector AssetBooks() => Table("FA_BOOKS");
    public EbsTableSelector DepreciationDetail() => Table("FA_DEPRN_DETAIL");
    public EbsTableSelector DepreciationPeriods() => Table("FA_DEPRN_PERIODS");

    // EAM convenience methods
    public EbsTableSelector EquipmentEam() => Table("EAM_EQUIPMENT_SERIAL_NUM");
    public EbsTableSelector ActivityHistory() => Table("EAM_ACTIVITY_HISTORY");
    public EbsTableSelector WorkOrders() => Table("EAM_WORK_ORDERS");
    public EbsTableSelector WorkOrderActivities() => Table("EAM_WO_ACTIVITIES");
    public EbsTableSelector ScheduledWorkOrders() => Table("EAM_SCHEDULED_WORK_ORDERS");
    public EbsTableSelector EquipmentHierarchy() => Table("EAM_OR_NETWORKS");

    // Equipment (Custom) convenience methods
    public EbsTableSelector EquipmentMasters() => Table("CUSTOM_EQUIPMENT_MASTER");
    public EbsTableSelector MaintenanceLogs() => Table("CUSTOM_MAINTENANCE_LOG");
    public EbsTableSelector MaintenanceParts() => Table("CUSTOM_MAINTENANCE_PARTS");
    public EbsTableSelector MaintenanceSchedules() => Table("CUSTOM_MAINTENANCE_SCHEDULE");
    public EbsTableSelector EquipmentHistoryLog() => Table("CUSTOM_EQUIPMENT_HISTORY");
}
