using Microsoft.Data.SqlClient;

namespace Grimoire.Demo;

public static class DatabaseSeeder
{
    public static async Task SeedLegacyDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();

        // Create legacy table
        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LegacyEmployees')
            CREATE TABLE LegacyEmployees (
                EmpId       INT PRIMARY KEY,
                FullName    NVARCHAR(200) NOT NULL,
                Email       NVARCHAR(200),
                DeptName    NVARCHAR(100) NOT NULL,
                HireDate    DATETIME,
                IsActive    BIT
            )
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LegacyResponsibilities')
            CREATE TABLE LegacyResponsibilities (
                ResponsibilityId INT PRIMARY KEY,
                EmpId            INT NOT NULL FOREIGN KEY REFERENCES LegacyEmployees(EmpId),
                Responsibility   NVARCHAR(200) NOT NULL,
                AssignedDate     DATETIME
            )
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Seed employees if not already present
        cmd.CommandText = "SELECT COUNT(*) FROM LegacyEmployees";
        var empCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken))!;

        if (empCount == 0)
        {
            cmd.CommandText = """
                INSERT INTO LegacyEmployees (EmpId, FullName, Email, DeptName, HireDate, IsActive)
                VALUES
                (1,  'Alice Johnson',    'alice.johnson@legacy.com',    'Engineering',  '2019-03-15', 1),
                (2,  'Bob Smith',        'bob.smith@legacy.com',        'Engineering',  '2020-06-01', 1),
                (3,  'Carol Williams',   'carol.w@legacy.com',          'Engineering',  '2018-01-10', 1),
                (4,  'David Brown',      'david.brown@legacy.com',      'Engineering',  '2021-09-20', 1),
                (5,  'Eva Martinez',     'eva.m@legacy.com',            'Engineering',  '2022-02-14', 1),
                (6,  'Frank Davis',      'frank.d@legacy.com',          'Marketing',    '2019-07-22', 1),
                (7,  'Grace Lee',        'grace.lee@legacy.com',        'Marketing',    '2020-11-05', 1),
                (8,  'Henry Wilson',     'henry.w@legacy.com',          'Marketing',    '2021-04-18', 1),
                (9,  'Ivy Chen',         'ivy.chen@legacy.com',         'Marketing',    '2023-01-09', 1),
                (10, 'Jack Taylor',      'jack.t@legacy.com',           'Sales',        '2018-05-30', 1),
                (11, 'Karen Anderson',   'karen.a@legacy.com',          'Sales',        '2019-08-12', 1),
                (12, 'Leo Thomas',       'leo.thomas@legacy.com',       'Sales',        '2020-12-03', 1),
                (13, 'Mia Jackson',      'mia.j@legacy.com',            'Sales',        '2021-06-25', 0),
                (14, 'Nathan White',     'nathan.w@legacy.com',         'Sales',        '2022-03-17', 1),
                (15, 'Olivia Harris',    'olivia.h@legacy.com',         'HR',           '2017-11-01', 1),
                (16, 'Paul Martin',      'paul.m@legacy.com',           'HR',           '2019-02-28', 1),
                (17, 'Quinn Robinson',   'quinn.r@legacy.com',          'HR',           '2020-09-14', 1),
                (18, 'Rachel Clark',     NULL,                          'HR',           '2023-07-01', 1),
                (19, 'Sam Lewis',        'sam.lewis@legacy.com',        'Engineering',  '2023-04-10', NULL),
                (20, 'Tina Walker',      'tina.w@legacy.com',           'Marketing',    NULL,         1)
                """;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Seed responsibilities if not already present
        cmd.CommandText = "SELECT COUNT(*) FROM LegacyResponsibilities";
        var respCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken))!;

        if (respCount == 0)
        {
            cmd.CommandText = """
                INSERT INTO LegacyResponsibilities (ResponsibilityId, EmpId, Responsibility, AssignedDate)
                VALUES
                (1,  1,  'Backend API Development',      '2019-03-15'),
                (2,  1,  'Code Review Lead',             '2021-01-10'),
                (3,  2,  'Frontend Development',         '2020-06-01'),
                (4,  3,  'Database Administration',      '2018-01-10'),
                (5,  3,  'Performance Optimization',     '2020-05-01'),
                (6,  4,  'CI/CD Pipeline Management',    '2021-09-20'),
                (7,  5,  'Cloud Infrastructure',         '2022-02-14'),
                (8,  6,  'Campaign Strategy',            '2019-07-22'),
                (9,  6,  'Brand Guidelines',             '2020-03-01'),
                (10, 7,  'Social Media Management',      '2020-11-05'),
                (11, 8,  'Content Creation',             '2021-04-18'),
                (12, 9,  'Market Research',              '2023-01-09'),
                (13, 10, 'Enterprise Sales',             '2018-05-30'),
                (14, 10, 'Key Account Management',       '2019-12-01'),
                (15, 11, 'Sales Training',               '2019-08-12'),
                (16, 12, 'CRM Administration',           '2020-12-03'),
                (17, 14, 'Sales Forecasting',            '2022-03-17'),
                (18, 15, 'Recruitment',                  '2017-11-01'),
                (19, 15, 'Employee Onboarding',          '2019-06-01'),
                (20, 16, 'Benefits Administration',      '2019-02-28'),
                (21, 17, 'Compliance Training',          '2020-09-14'),
                (22, 18, 'Payroll Processing',           '2023-07-01'),
                (23, 19, 'DevOps Automation',            NULL),
                (24, 20, 'Email Marketing',              '2021-08-15')
                """;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public static async Task PrepareTargetDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Departments')
            CREATE TABLE Departments (
                Id   INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL UNIQUE
            )
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
            CREATE TABLE Employees (
                Id           INT IDENTITY(1,1) PRIMARY KEY,
                FirstName    NVARCHAR(100) NOT NULL,
                LastName     NVARCHAR(100) NOT NULL,
                Email        NVARCHAR(200),
                DepartmentId INT FOREIGN KEY REFERENCES Departments(Id),
                HireDate     DATETIME,
                IsActive     BIT NOT NULL DEFAULT 1
            )
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Responsibilities')
            CREATE TABLE Responsibilities (
                Id           INT IDENTITY(1,1) PRIMARY KEY,
                EmployeeId   INT NOT NULL FOREIGN KEY REFERENCES Employees(Id),
                Title        NVARCHAR(200) NOT NULL,
                AssignedDate DATETIME
            )
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
