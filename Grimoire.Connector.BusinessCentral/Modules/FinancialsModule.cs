using Grimoire.Connector.BusinessCentral.Registry;

namespace Grimoire.Connector.BusinessCentral.Modules;

internal sealed class FinancialsModule : IBcModule
{
    public string ModuleName => "Financials";

    public void Register(BcRegistry registry)
    {
        registry.Entity("generalLedgerEntries", "generalLedgerEntries", "Financials")
            .Fields("id", "entryNumber", "postingDate",
                "documentNumber", "documentType",
                "accountId", "accountNumber",
                "description",
                "debitAmount", "creditAmount",
                "lastModifiedDateTime");

        registry.Entity("accounts", "accounts", "Financials")
            .Fields("id", "number", "displayName",
                "category", "subCategory",
                "blocked",
                "accountType",
                "directPosting",
                "netChange",
                "lastModifiedDateTime")
            .Field("balance", since: BcApiVersion.V2_0);

        registry.Entity("dimensions", "dimensions", "Financials")
            .Fields("id", "code", "displayName",
                "lastModifiedDateTime");

        registry.Entity("dimensionValues", "dimensionValues", "Financials")
            .Fields("id", "code", "dimensionId",
                "displayName",
                "lastModifiedDateTime");

        registry.Entity("journals", "journals", "Financials")
            .Fields("id", "code", "displayName",
                "balancingAccountId", "balancingAccountNumber",
                "lastModifiedDateTime");

        registry.Entity("journalLines", "journalLines", "Financials")
            .Fields("id", "journalId", "journalDisplayName",
                "lineNumber", "accountType", "accountId", "accountNumber",
                "postingDate", "documentNumber", "externalDocumentNumber",
                "amount", "description", "comment",
                "taxCode",
                "balanceAccountType", "balancingAccountId", "balancingAccountNumber",
                "lastModifiedDateTime");

        registry.Entity("taxGroups", "taxGroups", "Financials")
            .Fields("id", "code", "displayName",
                "taxType",
                "lastModifiedDateTime");

        registry.Entity("taxAreas", "taxAreas", "Financials")
            .Fields("id", "code", "displayName",
                "taxType",
                "lastModifiedDateTime");

        registry.Entity("currencies", "currencies", "Financials", since: BcApiVersion.V2_0)
            .Fields(BcApiVersion.V2_0,
                "id", "code", "displayName",
                "symbol", "amountDecimalPlaces", "amountRoundingPrecision",
                "lastModifiedDateTime");

        registry.Entity("paymentTerms", "paymentTerms", "Financials")
            .Fields("id", "code", "displayName",
                "dueDateCalculation", "discountDateCalculation",
                "discountPercent",
                "calculateDiscountOnCreditMemos",
                "lastModifiedDateTime");

        registry.Entity("paymentMethods", "paymentMethods", "Financials")
            .Fields("id", "code", "displayName",
                "lastModifiedDateTime");
    }
}
