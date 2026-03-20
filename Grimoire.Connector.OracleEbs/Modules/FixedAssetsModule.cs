using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class FixedAssetsModule : IEbsModule
{
    public string ModuleName => "FixedAssets";

    public void Register(EbsRegistry registry)
    {
        // Asset Master (FA_ADDITIONS)
        registry.Table("FA_ADDITIONS", "FA_ADDITIONS", "FA", "FixedAssets")
            .Columns("ASSET_ID", "ASSET_NUMBER", "DESCRIPTION",
                "ASSET_CATEGORY_ID", "SERIAL_NUMBER",
                "DATE_PLACED_IN_SERVICE", "ASSET_COST",
                "CURRENT_UNITS", "CURRENT_SALVAGE_VALUE",
                "ORG_ID", "ASSET_TYPE")
            .Column("REVALUED_FLAG", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("FA_ASSET_HISTORY", "ASSET_ID", "ASSET_ID")
            .JoinTo("FA_RETIREMENTS", "ASSET_ID", "ASSET_ID")
            .JoinTo("FA_BOOKS", "ASSET_ID", "ASSET_ID");

        // Asset History (Transaction Ledger - Full Depth)
        registry.Table("FA_ASSET_HISTORY", "FA_ASSET_HISTORY", "FA", "FixedAssets")
            .Columns("ASSET_HISTORY_ID", "ASSET_ID",
                "TRANSACTION_TYPE_CODE", "TRANSACTION_DATE",
                "FROM_LOCATION_ID", "TO_LOCATION_ID",
                "UNITS_TRANSFERRED", "COST_TRANSFERRED",
                "DATE_RETIRED", "REVALUED_FLAG",
                "CURRENT_COST_AFTER_TRANSACTION",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("FA_ADDITIONS", "ASSET_ID", "ASSET_ID");

        // Retirements (Transaction Detail)
        registry.Table("FA_RETIREMENTS", "FA_RETIREMENTS", "FA", "FixedAssets")
            .Columns("RETIREMENT_ID", "ASSET_ID", "ASSET_HISTORY_ID",
                "RETIREMENT_DATE", "RETIREMENT_AMOUNT",
                "RETIREMENT_TYPE_CODE",
                "PROCEEDS_OF_SALE", "LOSS_ON_RETIREMENT",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("FA_ADDITIONS", "ASSET_ID", "ASSET_ID");

        // Asset Books (Asset-Book Participation, 1:M per asset)
        registry.Table("FA_BOOKS", "FA_BOOKS", "FA", "FixedAssets")
            .Columns("ASSET_ID", "BOOK_TYPE_CODE",
                "DEPRN_METHOD_CODE",
                "RATE", "LIFE_IN_MONTHS",
                "SALVAGE_VALUE", "DEPRECIATED_BASE",
                "CURRENT_ACCUMULATED_DEPRN",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("FA_ADDITIONS", "ASSET_ID", "ASSET_ID")
            .JoinTo("FA_DEPRN_DETAIL", "ASSET_ID", "ASSET_ID");

        // Depreciation Detail (Transaction Ledger - Per Period)
        registry.Table("FA_DEPRN_DETAIL", "FA_DEPRN_DETAIL", "FA", "FixedAssets")
            .Columns("DEPRN_DETAIL_ID", "ASSET_ID", "BOOK_TYPE_CODE",
                "DEPRN_PERIOD", "DEPRN_YEAR",
                "BEGINNING_BALANCE", "DEPRN_AMOUNT",
                "ENDING_BALANCE", "DEPRN_PERIODS_ELAPSED",
                "ORG_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("FA_BOOKS", "ASSET_ID", "ASSET_ID")
            .JoinTo("FA_DEPRN_PERIODS", "DEPRN_PERIOD", "DEPRN_PERIOD");

        // Depreciation Periods (Reference)
        registry.Table("FA_DEPRN_PERIODS", "FA_DEPRN_PERIODS", "FA", "FixedAssets")
            .Columns("DEPRN_PERIOD", "DEPRN_YEAR",
                "PERIOD_START_DATE", "PERIOD_END_DATE",
                "FISCAL_YEAR", "FISCAL_QUARTER",
                "CLOSED_FLAG")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");
    }
}
