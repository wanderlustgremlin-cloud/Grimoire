using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Grimoire.Connector.BusinessCentral.Modules;
using Grimoire.Connector.BusinessCentral.Registry;
using Grimoire.Core.Extract;

namespace Grimoire.Connector.BusinessCentral;

public sealed class BusinessCentralExtractor : ICustomExtractor
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly BcApiVersion _version;
    private readonly Guid _companyId;
    private readonly BcModuleSelector _moduleSelector;
    private readonly BcRegistry _registry;

    public BusinessCentralExtractor(
        string environmentUrl,
        BcApiVersion version,
        Guid companyId,
        HttpClient httpClient,
        Action<BcModuleSelector> configureModules)
    {
        _httpClient = httpClient;
        _version = version;
        _companyId = companyId;

        // Normalize base URL: strip trailing slash
        _baseUrl = environmentUrl.TrimEnd('/');

        _moduleSelector = new BcModuleSelector();
        configureModules(_moduleSelector);

        if (_moduleSelector.SelectedModules.Count == 0)
            throw new InvalidOperationException("At least one Business Central module must be selected.");

        _registry = new BcRegistry();
        RegisterModules();
    }

    public async IAsyncEnumerable<SourceRow> ExtractAsync(
        ExtractRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // The mapping's FromTables() specifies the BC entity name (e.g., "customers")
        var entityName = request.SourceTables.Count > 0
            ? request.SourceTables[0]
            : request.EntityName;

        var entity = _registry.FindEntity(entityName, _version)
            ?? throw new InvalidOperationException(
                $"Entity '{entityName}' is not registered for Business Central API {_version.ToPathSegment()}. " +
                $"Check that the entity name matches a BC API entity and the correct module is selected.");

        // Build $select from requested columns or all available fields
        var availableFields = entity.GetFieldsForVersion(_version);
        IReadOnlyList<string> selectFields;

        if (request.SourceColumns.Count > 0)
        {
            // Filter to only fields that exist in this version
            selectFields = request.SourceColumns
                .Where(c => availableFields.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            selectFields = availableFields;
        }

        var url = BuildUrl(entity.ApiPath, selectFields);

        await foreach (var row in FetchPagesAsync(url, cancellationToken))
        {
            yield return row;
        }
    }

    private string BuildUrl(string apiPath, IReadOnlyList<string> selectFields)
    {
        var url = $"{_baseUrl}/api/{_version.ToPathSegment()}/companies({_companyId})/{apiPath}";

        if (selectFields.Count > 0)
        {
            url += $"?$select={string.Join(",", selectFields)}";
        }

        return url;
    }

    private async IAsyncEnumerable<SourceRow> FetchPagesAsync(
        string url,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? nextUrl = url;

        while (nextUrl is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await _httpClient.GetAsync(nextUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

            if (json.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in valueArray.EnumerateArray())
                {
                    var row = new SourceRow();
                    foreach (var property in item.EnumerateObject())
                    {
                        row[property.Name] = ConvertJsonValue(property.Value);
                    }
                    yield return row;
                }
            }

            // Follow OData pagination
            nextUrl = json.TryGetProperty("@odata.nextLink", out var nextLink)
                ? nextLink.GetString()
                : null;
        }
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.TryGetDateTimeOffset(out var dto) ? dto : element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private void RegisterModules()
    {
        var modules = new IBcModule[]
        {
            new SalesModule(),
            new PurchasingModule(),
            new FinancialsModule(),
            new InventoryModule(),
            new HrModule()
        };

        foreach (var module in modules)
        {
            if (_moduleSelector.SelectedModules.Contains(module.ModuleName))
            {
                module.Register(_registry);
            }
        }
    }
}
