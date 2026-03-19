using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class GlModule : IEbsModule
{
    public string ModuleName => "GL";

    public void Register(EbsRegistry registry)
    {
        // Journal Headers
        registry.Table("GL_JE_HEADERS", "GL_JE_HEADERS", "GL", "GL")
            .Columns("JE_HEADER_ID", "JE_BATCH_ID", "NAME",
                "JE_CATEGORY", "JE_SOURCE", "PERIOD_NAME",
                "DEFAULT_EFFECTIVE_DATE", "STATUS",
                "ACTUAL_FLAG", "CURRENCY_CODE", "CURRENCY_CONVERSION_RATE",
                "SET_OF_BOOKS_ID", "DESCRIPTION",
                "RUNNING_TOTAL_DR", "RUNNING_TOTAL_CR",
                "RUNNING_TOTAL_ACCOUNTED_DR", "RUNNING_TOTAL_ACCOUNTED_CR")
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("GL_JE_LINES", "JE_HEADER_ID", "JE_HEADER_ID")
            .JoinTo("GL_JE_BATCHES", "JE_BATCH_ID", "JE_BATCH_ID");

        // Journal Lines
        registry.Table("GL_JE_LINES", "GL_JE_LINES", "GL", "GL")
            .Columns("JE_HEADER_ID", "JE_LINE_NUM",
                "CODE_COMBINATION_ID", "EFFECTIVE_DATE", "PERIOD_NAME",
                "STATUS", "DESCRIPTION",
                "ENTERED_DR", "ENTERED_CR",
                "ACCOUNTED_DR", "ACCOUNTED_CR",
                "SET_OF_BOOKS_ID")
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("GL_JE_HEADERS", "JE_HEADER_ID", "JE_HEADER_ID")
            .JoinTo("GL_CODE_COMBINATIONS", "CODE_COMBINATION_ID", "CODE_COMBINATION_ID");

        // Journal Batches
        registry.Table("GL_JE_BATCHES", "GL_JE_BATCHES", "GL", "GL")
            .Columns("JE_BATCH_ID", "NAME", "STATUS",
                "DESCRIPTION", "DEFAULT_PERIOD_NAME",
                "DEFAULT_EFFECTIVE_DATE",
                "RUNNING_TOTAL_DR", "RUNNING_TOTAL_CR",
                "RUNNING_TOTAL_ACCOUNTED_DR", "RUNNING_TOTAL_ACCOUNTED_CR",
                "SET_OF_BOOKS_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Chart of Accounts / Code Combinations
        registry.Table("GL_CODE_COMBINATIONS", "GL_CODE_COMBINATIONS", "GL", "GL")
            .Columns("CODE_COMBINATION_ID", "CHART_OF_ACCOUNTS_ID",
                "SEGMENT1", "SEGMENT2", "SEGMENT3", "SEGMENT4", "SEGMENT5",
                "SEGMENT6", "SEGMENT7", "SEGMENT8", "SEGMENT9", "SEGMENT10",
                "ENABLED_FLAG", "START_DATE_ACTIVE", "END_DATE_ACTIVE",
                "SUMMARY_FLAG", "DETAIL_POSTING_ALLOWED_FLAG",
                "ACCOUNT_TYPE")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Balances
        registry.Table("GL_BALANCES", "GL_BALANCES", "GL", "GL")
            .Columns("CODE_COMBINATION_ID", "CURRENCY_CODE",
                "PERIOD_NAME", "ACTUAL_FLAG",
                "SET_OF_BOOKS_ID",
                "PERIOD_NET_DR", "PERIOD_NET_CR",
                "BEGIN_BALANCE_DR", "BEGIN_BALANCE_CR",
                "QUARTER_TO_DATE_DR", "QUARTER_TO_DATE_CR",
                "PROJECT_TO_DATE_DR", "PROJECT_TO_DATE_CR")
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Column("TRANSLATED_FLAG", since: EbsVersion.R11i)
            .JoinTo("GL_CODE_COMBINATIONS", "CODE_COMBINATION_ID", "CODE_COMBINATION_ID");

        // Sets of Books (R11i) / Ledgers (R12+)
        registry.Table("GL_SETS_OF_BOOKS", "GL_SETS_OF_BOOKS", "GL", "GL")
            .Columns("SET_OF_BOOKS_ID", "NAME", "SHORT_NAME",
                "CHART_OF_ACCOUNTS_ID", "CURRENCY_CODE", "PERIOD_SET_NAME",
                "ACCOUNTED_PERIOD_TYPE")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        registry.Table("GL_LEDGERS", "GL_LEDGERS", "GL", "GL")
            .Column("LEDGER_ID", since: EbsVersion.R12)
            .Column("NAME", since: EbsVersion.R12)
            .Column("SHORT_NAME", since: EbsVersion.R12)
            .Column("CHART_OF_ACCOUNTS_ID", since: EbsVersion.R12)
            .Column("CURRENCY_CODE", since: EbsVersion.R12)
            .Column("PERIOD_SET_NAME", since: EbsVersion.R12)
            .Column("ACCOUNTED_PERIOD_TYPE", since: EbsVersion.R12)
            .Column("LEDGER_CATEGORY_CODE", since: EbsVersion.R12)
            .Column("OBJECT_TYPE_CODE", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12);
    }
}
