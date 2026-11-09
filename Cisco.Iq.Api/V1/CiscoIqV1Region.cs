namespace Cisco.Iq.Api.V1;

/// <summary>The Cisco IQ data storage region that holds the account.</summary>
public enum CiscoIqV1Region
{
	/// <summary>United States.</summary>
	Us = 0,

	/// <summary>Europe, Middle East and Africa.</summary>
	Emea = 1,

	/// <summary>Asia Pacific, Japan and China.</summary>
	Apjc = 2
}

internal static class CiscoIqV1RegionExtensions
{
	internal static string ToWireValue(this CiscoIqV1Region region)
		=> region switch
		{
			CiscoIqV1Region.Us => "US",
			CiscoIqV1Region.Emea => "EMEA",
			CiscoIqV1Region.Apjc => "APJC",
			_ => throw new ArgumentOutOfRangeException(nameof(region), region, "Undefined Cisco IQ region.")
		};
}
