using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal sealed class HrModule : IEbsModule
{
    public string ModuleName => "HR";

    public void Register(EbsRegistry registry)
    {
        // People
        registry.Table("PER_ALL_PEOPLE_F", "PER_ALL_PEOPLE_F", "HR", "HR")
            .Columns("PERSON_ID", "EMPLOYEE_NUMBER", "FIRST_NAME", "LAST_NAME",
                "FULL_NAME", "DATE_OF_BIRTH", "EMAIL_ADDRESS", "NATIONAL_IDENTIFIER",
                "SEX", "MARITAL_STATUS", "NATIONALITY",
                "EFFECTIVE_START_DATE", "EFFECTIVE_END_DATE",
                "PERSON_TYPE_ID", "BUSINESS_GROUP_ID")
            .Column("PARTY_ID", since: EbsVersion.R12)
            .Column("PERSON_NUMBER", since: EbsVersion.R122)
            .Column("GLOBAL_PERSON_ID", since: EbsVersion.R122)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PER_ALL_ASSIGNMENTS_F", "PERSON_ID", "PERSON_ID")
            .JoinTo("PER_PERSON_TYPES", "PERSON_TYPE_ID", "PERSON_TYPE_ID")
            .JoinTo("HR_ALL_ORGANIZATION_UNITS", "BUSINESS_GROUP_ID", "ORGANIZATION_ID");

        // Assignments
        registry.Table("PER_ALL_ASSIGNMENTS_F", "PER_ALL_ASSIGNMENTS_F", "HR", "HR")
            .Columns("ASSIGNMENT_ID", "PERSON_ID", "ASSIGNMENT_NUMBER",
                "ASSIGNMENT_TYPE", "PRIMARY_FLAG", "ASSIGNMENT_STATUS_TYPE_ID",
                "ORGANIZATION_ID", "JOB_ID", "POSITION_ID", "GRADE_ID",
                "LOCATION_ID", "SUPERVISOR_ID", "PAYROLL_ID",
                "EFFECTIVE_START_DATE", "EFFECTIVE_END_DATE",
                "BUSINESS_GROUP_ID")
            .Column("SUPERVISOR_ASSIGNMENT_ID", since: EbsVersion.R12)
            .Column("DEFAULT_CODE_COMB_ID", since: EbsVersion.R11i)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("PER_ALL_PEOPLE_F", "PERSON_ID", "PERSON_ID")
            .JoinTo("HR_ALL_ORGANIZATION_UNITS", "ORGANIZATION_ID", "ORGANIZATION_ID")
            .JoinTo("PER_JOBS", "JOB_ID", "JOB_ID")
            .JoinTo("PER_ALL_POSITIONS", "POSITION_ID", "POSITION_ID", since: EbsVersion.R11i, until: EbsVersion.R12)
            .JoinTo("HR_ALL_POSITIONS_F", "POSITION_ID", "POSITION_ID", since: EbsVersion.R12)
            .JoinTo("PER_GRADES", "GRADE_ID", "GRADE_ID")
            .JoinTo("HR_LOCATIONS_ALL", "LOCATION_ID", "LOCATION_ID");

        // Organizations
        registry.Table("HR_ALL_ORGANIZATION_UNITS", "HR_ALL_ORGANIZATION_UNITS", "HR", "HR")
            .Columns("ORGANIZATION_ID", "NAME", "TYPE",
                "DATE_FROM", "DATE_TO", "LOCATION_ID",
                "BUSINESS_GROUP_ID", "INTERNAL_EXTERNAL_FLAG")
            .Column("ATTRIBUTE_CATEGORY", since: EbsVersion.R11i)
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE")
            .JoinTo("HR_LOCATIONS_ALL", "LOCATION_ID", "LOCATION_ID");

        // Locations
        registry.Table("HR_LOCATIONS_ALL", "HR_LOCATIONS_ALL", "HR", "HR")
            .Columns("LOCATION_ID", "LOCATION_CODE", "DESCRIPTION",
                "ADDRESS_LINE_1", "ADDRESS_LINE_2", "ADDRESS_LINE_3",
                "TOWN_OR_CITY", "REGION_1", "REGION_2", "REGION_3",
                "POSTAL_CODE", "COUNTRY", "TELEPHONE_NUMBER_1",
                "INACTIVE_DATE")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Jobs
        registry.Table("PER_JOBS", "PER_JOBS", "HR", "HR")
            .Columns("JOB_ID", "NAME", "DATE_FROM", "DATE_TO",
                "JOB_GROUP_ID", "BUSINESS_GROUP_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Positions (R11i)
        registry.Table("PER_ALL_POSITIONS", "PER_ALL_POSITIONS", "HR", "HR")
            .Columns("POSITION_ID", "NAME", "JOB_ID", "ORGANIZATION_ID",
                "LOCATION_ID", "DATE_EFFECTIVE", "DATE_END",
                "BUSINESS_GROUP_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Positions (R12+ uses date-tracked table)
        registry.Table("HR_ALL_POSITIONS_F", "HR_ALL_POSITIONS_F", "HR", "HR")
            .Column("POSITION_ID", since: EbsVersion.R12)
            .Column("NAME", since: EbsVersion.R12)
            .Column("JOB_ID", since: EbsVersion.R12)
            .Column("ORGANIZATION_ID", since: EbsVersion.R12)
            .Column("LOCATION_ID", since: EbsVersion.R12)
            .Column("EFFECTIVE_START_DATE", since: EbsVersion.R12)
            .Column("EFFECTIVE_END_DATE", since: EbsVersion.R12)
            .Column("BUSINESS_GROUP_ID", since: EbsVersion.R12)
            .Column("CREATED_BY", since: EbsVersion.R12)
            .Column("CREATION_DATE", since: EbsVersion.R12)
            .Column("LAST_UPDATED_BY", since: EbsVersion.R12)
            .Column("LAST_UPDATE_DATE", since: EbsVersion.R12)
            .JoinTo("PER_JOBS", "JOB_ID", "JOB_ID")
            .JoinTo("HR_ALL_ORGANIZATION_UNITS", "ORGANIZATION_ID", "ORGANIZATION_ID", since: EbsVersion.R12);

        // Grades
        registry.Table("PER_GRADES", "PER_GRADES", "HR", "HR")
            .Columns("GRADE_ID", "NAME", "DATE_FROM", "DATE_TO",
                "BUSINESS_GROUP_ID")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");

        // Person Types
        registry.Table("PER_PERSON_TYPES", "PER_PERSON_TYPES", "HR", "HR")
            .Columns("PERSON_TYPE_ID", "SYSTEM_PERSON_TYPE", "USER_PERSON_TYPE",
                "BUSINESS_GROUP_ID", "ACTIVE_FLAG", "DEFAULT_FLAG")
            .Columns("CREATED_BY", "CREATION_DATE", "LAST_UPDATED_BY", "LAST_UPDATE_DATE");
    }
}
