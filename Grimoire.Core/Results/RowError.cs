namespace Grimoire.Core.Results;

public enum RowErrorType
{
    MappingError,
    ForeignKeyNotFound,
    TypeConversion,
    NullValue,
    LoadError
}

public sealed record RowError(
    string EntityName,
    RowErrorType ErrorType,
    string Message,
    IDictionary<string, object?>? SourceData = null,
    Exception? Exception = null);
