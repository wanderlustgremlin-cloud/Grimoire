using Grimoire.Connector.BusinessCentral.Registry;

namespace Grimoire.Connector.BusinessCentral.Modules;

internal sealed class SalesModule : IBcModule
{
    public string ModuleName => "Sales";

    public void Register(BcRegistry registry)
    {
        registry.Entity("customers", "customers", "Sales")
            .Fields("id", "number", "displayName", "type",
                "addressLine1", "addressLine2", "city", "state", "country", "postalCode",
                "phoneNumber", "email", "website",
                "taxLiable", "taxAreaId", "taxAreaDisplayName",
                "taxRegistrationNumber",
                "currencyId", "currencyCode",
                "paymentTermsId", "shipmentMethodId", "paymentMethodId",
                "blocked",
                "balance", "overdueAmount", "totalSalesExcludingTax",
                "lastModifiedDateTime");

        registry.Entity("salesOrders", "salesOrders", "Sales", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "number", "externalDocumentNumber",
                "orderDate", "postingDate", "customerId", "customerNumber", "customerName",
                "billToName", "billToCustomerId", "billToCustomerNumber",
                "shipToName", "shipToContact",
                "currencyId", "currencyCode",
                "paymentTermsId",
                "salesperson",
                "requestedDeliveryDate",
                "discountAmount", "discountAppliedBeforeTax",
                "totalAmountExcludingTax", "totalTaxAmount", "totalAmountIncludingTax",
                "fullyShipped",
                "status",
                "lastModifiedDateTime");

        registry.Entity("salesOrderLines", "salesOrderLines", "Sales", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "documentId",
                "sequence", "itemId", "accountId",
                "lineType", "lineObjectNumber", "description",
                "unitOfMeasureId", "unitOfMeasureCode",
                "quantity", "unitPrice",
                "discountAmount", "discountPercent", "discountAppliedBeforeTax",
                "amountExcludingTax", "taxCode", "taxPercent",
                "totalTaxAmount", "amountIncludingTax",
                "invoiceDiscountAllocation",
                "netAmount", "netTaxAmount", "netAmountIncludingTax",
                "shipmentDate", "shippedQuantity", "invoicedQuantity",
                "shipQuantity", "invoiceQuantity");

        registry.Entity("salesInvoices", "salesInvoices", "Sales")
            .Fields("id", "number", "externalDocumentNumber",
                "invoiceDate", "postingDate", "dueDate",
                "customerId", "customerNumber", "customerName",
                "billToName", "billToCustomerId", "billToCustomerNumber",
                "shipToName", "shipToContact",
                "currencyId", "currencyCode",
                "paymentTermsId",
                "salesperson",
                "discountAmount", "discountAppliedBeforeTax",
                "totalAmountExcludingTax", "totalTaxAmount", "totalAmountIncludingTax",
                "status",
                "lastModifiedDateTime")
            .Field("remainingAmount", since: BcApiVersion.V2_0);

        registry.Entity("salesInvoiceLines", "salesInvoiceLines", "Sales")
            .Fields("id", "documentId",
                "sequence", "itemId", "accountId",
                "lineType", "lineObjectNumber", "description",
                "unitOfMeasureId", "unitOfMeasureCode",
                "unitPrice", "quantity",
                "discountAmount", "discountPercent", "discountAppliedBeforeTax",
                "amountExcludingTax", "taxCode", "taxPercent",
                "totalTaxAmount", "amountIncludingTax",
                "invoiceDiscountAllocation",
                "netAmount", "netTaxAmount", "netAmountIncludingTax");

        registry.Entity("salesCreditMemos", "salesCreditMemos", "Sales")
            .Fields("id", "number", "externalDocumentNumber",
                "creditMemoDate", "postingDate", "dueDate",
                "customerId", "customerNumber", "customerName",
                "billToName", "billToCustomerId", "billToCustomerNumber",
                "currencyId", "currencyCode",
                "paymentTermsId",
                "salesperson",
                "discountAmount", "discountAppliedBeforeTax",
                "totalAmountExcludingTax", "totalTaxAmount", "totalAmountIncludingTax",
                "status",
                "lastModifiedDateTime");

        registry.Entity("salesQuotes", "salesQuotes", "Sales", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "number", "externalDocumentNumber",
                "documentDate", "postingDate", "dueDate",
                "customerId", "customerNumber", "customerName",
                "billToName", "billToCustomerId", "billToCustomerNumber",
                "shipToName", "shipToContact",
                "currencyId", "currencyCode",
                "paymentTermsId",
                "salesperson",
                "discountAmount", "discountAppliedBeforeTax",
                "totalAmountExcludingTax", "totalTaxAmount", "totalAmountIncludingTax",
                "status", "sentDate", "validUntilDate", "acceptedDate",
                "lastModifiedDateTime");
    }
}
