namespace Grimoire.Connector.BusinessCentral;

public sealed class BcModuleSelector
{
    internal HashSet<string> SelectedModules { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal HashSet<string>? SelectedEntities { get; private set; }

    public BcModuleSelector Sales(Action<BcEntitySelector>? configure = null)
        => SelectModule("Sales", configure);

    public BcModuleSelector Purchasing(Action<BcEntitySelector>? configure = null)
        => SelectModule("Purchasing", configure);

    public BcModuleSelector Financials(Action<BcEntitySelector>? configure = null)
        => SelectModule("Financials", configure);

    public BcModuleSelector Inventory(Action<BcEntitySelector>? configure = null)
        => SelectModule("Inventory", configure);

    public BcModuleSelector HR(Action<BcEntitySelector>? configure = null)
        => SelectModule("HR", configure);

    private BcModuleSelector SelectModule(string module, Action<BcEntitySelector>? configure)
    {
        SelectedModules.Add(module);

        if (configure is not null)
        {
            SelectedEntities ??= new(StringComparer.OrdinalIgnoreCase);
            var selector = new BcEntitySelector();
            configure(selector);
            foreach (var entity in selector.Entities)
                SelectedEntities.Add(entity);
        }

        return this;
    }
}

public sealed class BcEntitySelector
{
    internal HashSet<string> Entities { get; } = new(StringComparer.OrdinalIgnoreCase);

    public BcEntitySelector Entity(string entityName)
    {
        Entities.Add(entityName);
        return this;
    }

    // Sales
    public BcEntitySelector Customers() => Entity("customers");
    public BcEntitySelector SalesOrders() => Entity("salesOrders");
    public BcEntitySelector SalesOrderLines() => Entity("salesOrderLines");
    public BcEntitySelector SalesInvoices() => Entity("salesInvoices");
    public BcEntitySelector SalesInvoiceLines() => Entity("salesInvoiceLines");
    public BcEntitySelector SalesCreditMemos() => Entity("salesCreditMemos");
    public BcEntitySelector SalesQuotes() => Entity("salesQuotes");

    // Purchasing
    public BcEntitySelector Vendors() => Entity("vendors");
    public BcEntitySelector PurchaseOrders() => Entity("purchaseOrders");
    public BcEntitySelector PurchaseOrderLines() => Entity("purchaseOrderLines");
    public BcEntitySelector PurchaseInvoices() => Entity("purchaseInvoices");
    public BcEntitySelector PurchaseInvoiceLines() => Entity("purchaseInvoiceLines");

    // Financials
    public BcEntitySelector GeneralLedgerEntries() => Entity("generalLedgerEntries");
    public BcEntitySelector Accounts() => Entity("accounts");
    public BcEntitySelector Dimensions() => Entity("dimensions");
    public BcEntitySelector DimensionValues() => Entity("dimensionValues");
    public BcEntitySelector Journals() => Entity("journals");
    public BcEntitySelector JournalLines() => Entity("journalLines");
    public BcEntitySelector TaxGroups() => Entity("taxGroups");
    public BcEntitySelector TaxAreas() => Entity("taxAreas");
    public BcEntitySelector Currencies() => Entity("currencies");
    public BcEntitySelector PaymentTerms() => Entity("paymentTerms");
    public BcEntitySelector PaymentMethods() => Entity("paymentMethods");

    // Inventory
    public BcEntitySelector Items() => Entity("items");
    public BcEntitySelector ItemCategories() => Entity("itemCategories");
    public BcEntitySelector ItemVariants() => Entity("itemVariants");
    public BcEntitySelector UnitsOfMeasure() => Entity("unitsOfMeasure");
    public BcEntitySelector Locations() => Entity("locations");

    // HR
    public BcEntitySelector Employees() => Entity("employees");
}
