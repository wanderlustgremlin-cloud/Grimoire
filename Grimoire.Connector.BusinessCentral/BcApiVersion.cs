namespace Grimoire.Connector.BusinessCentral;

public enum BcApiVersion
{
    V1_0 = 1,
    V2_0 = 2
}

internal static class BcApiVersionExtensions
{
    public static string ToPathSegment(this BcApiVersion version) => version switch
    {
        BcApiVersion.V1_0 => "v1.0",
        BcApiVersion.V2_0 => "v2.0",
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };
}
