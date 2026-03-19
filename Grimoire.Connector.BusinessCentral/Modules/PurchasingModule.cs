using Grimoire.Connector.BusinessCentral.Registry;

namespace Grimoire.Connector.BusinessCentral.Modules;

internal sealed class PurchasingModule : IBcModule
{
    public string ModuleName => "Purchasing";

    public void Register(BcRegistry registry)
    {
        registry.Entity("vendors", "vendors", "Purchasing")
            .Fields("id", "number", "displayName",
                "addressLine1", "addressLine2", "city", "state", "country", "postalCode",
                "phoneNumber", "email", "website",
                "taxLiable", "taxRegistrationNumber",
                "currencyId", "currencyCode",
                "paymentTermsId", "paymentMethodId",
                "blocked",
                "balance",
                "lastModifiedDateTime");

        registry.Entity("purchaseOrders", "purchaseOrders", "Purchasing", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "number",
                "orderDate", "postingDate",
                "vendorId", "vendorNumber", "vendorName",
                "payToName", "payToVendorId", "payToVendorNumber",
                "shipToName", "shipToContact",
                "buyFromAddressLine1", "buyFromAddressLine2",
                "buyFromCity", "buyFromState", "buyFromCountry", "buyFromPostCode",
                "currencyId", "currencyCode",
                "paymentTermsId",
                "purchaser",
                "requestedReceiptDate",
                "discountAmount", "discountAppliedBeforeTax",
                "totalAmountExcludingTax", "totalTaxAmount", "totalAmountIncludingTax",
                "fullyReceived",
                "status",
                "lastModifiedDateTime");

        registry.Entity("purchaseOrderLines", "purchaseOrderLines", "Purchasing", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "documentId",
                "sequence", "itemId", "accountId",
                "lineType", "lineObjectNumber", "description",
                "unitOfMeasureId", "unitOfMeasureCode",
                "unitCost", "quantity",
                "discountAmount", "discountPercent", "discountAppliedBeforeTax",
                "amountExcludingTax", "taxCode", "taxPercent",
                "totalTaxAmount", "amountIncludingTax",
                "invoiceDiscountAllocation",
                "netAmount", "netTaxAmount", "netAmountIncludingTax",
                "expectedReceiptDate", "receivedQuantity", "invoicedQuantity",
                "receiveQuantity", "invoiceQuantity");

        registry.Entity("purchaseInvoices", "purchaseInvoices", "Purchasing")
            .Fields("id", "number", "vendorInvoiceNumber",
                "invoiceDate", "postingDate", "dueDate",
                "vendorId", "vendorNumber", "vendorName",
                "payToName", "payToVendorId", "payToVendorNumber",
                "buyFromAddressLine1", "buyFromAddressLine2",
                "buyFromCity", "buyFromState", "buyFromCountry", "buyFromPostCode",
                "currencyId", "currencyCode",
                "paymentTermsId",
                "purchaser",
                "discountAmount", "discountAppliedBeforeTax",
                "totalAmountExcludingTax", "totalTaxAmount", "totalAmountIncludingTax",
                "status",
                "lastModifiedDateTime");

        registry.Entity("purchaseInvoiceLines", "purchaseInvoiceLines", "Purchasing")
            .Fields("id", "documentId",
                "sequence", "itemId", "accountId",
                "lineType", "lineObjectNumber", "description",
                "unitOfMeasureId", "unitOfMeasureCode",
                "unitCost", "quantity",
                "discountAmount", "discountPercent", "discountAppliedBeforeTax",
                "amountExcludingTax", "taxCode", "taxPercent",
                "totalTaxAmount", "amountIncludingTax",
                "invoiceDiscountAllocation",
                "netAmount", "netTaxAmount", "netAmountIncludingTax");
    }
}
