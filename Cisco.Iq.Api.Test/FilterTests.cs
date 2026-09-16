namespace Cisco.Iq.Api.Test;

public class FilterTests
{
	[Fact]
	public async Task EveryFilter_UsesRepeatedArraysEpochDatesUppercaseOrderAndOmitsNulls()
	{
		var expected = new Queue<Dictionary<string, string[]>>();
		using var client = new CiscoIqClient(AuthenticationTests.Options, new TestTransport((request, _) =>
		{
			var query = request.RequestUri!.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
				.Select(pair => pair.Split('=', 2))
				.GroupBy(pair => Uri.UnescapeDataString(pair[0]))
				.ToDictionary(group => group.Key, group => group.Select(pair => Uri.UnescapeDataString(pair[1])).ToArray());
			query.Should().BeEquivalentTo(expected.Dequeue());
			return Task.FromResult(TestTransport.Json("{\"items\":[],\"meta\":{}}"));
		}), PagingAndRetryTests.Exchange);
		expected.Enqueue(new Dictionary<string, string[]> {
			["productFamily"] = ["a value", "b&two"],
			["productId"] = ["a value", "b&two"],
			["serialNumber"] = ["a value", "b&two"],
			["hostname"] = ["a value", "b&two"],
			["ipAddress"] = ["a value", "b&two"],
			["equipmentType"] = ["a value", "b&two"],
			["productType"] = ["a value", "b&two"],
			["softwareType"] = ["a value", "b&two"],
			["softwareVersion"] = ["a value", "b&two"],
			["role"] = ["a value", "b&two"],
			["importance"] = ["a value", "b&two"],
			["location"] = ["a value", "b&two"],
			["assetTags"] = ["a value", "b&two"],
			["dataSource"] = ["a value", "b&two"],
			["contractNumber"] = ["a value", "b&two"],
			["contractId"] = ["a value", "b&two"],
			["contractStatus"] = ["a value", "b&two"],
			["coverageStatus"] = ["a value", "b&two"],
			["supportType"] = ["a value", "b&two"],
			["supportTier"] = ["a value", "b&two"],
			["partnerName"] = ["a value", "b&two"],
			["telemetryStatus"] = ["a value", "b&two"],
			["lastSignalType"] = ["a value", "b&two"],
			["currentHardwareMilestone"] = ["a value", "b&two"],
			["currentSoftwareMilestone"] = ["a value", "b&two"],
			["nextHardwareMilestone"] = ["a value", "b&two"],
			["nextSoftwareMilestone"] = ["a value", "b&two"],
			["lastSignalBefore"] = ["1700000000123"],
			["lastSignalAfter"] = ["1700000000123"],
			["coverageEndBefore"] = ["1700000000123"],
			["coverageEndAfter"] = ["1700000000123"],
			["warrantyEndBefore"] = ["1700000000123"],
			["warrantyEndAfter"] = ["1700000000123"],
			["shipDateBefore"] = ["1700000000123"],
			["shipDateAfter"] = ["1700000000123"],
			["endOfSoftwareMaintenanceBefore"] = ["1700000000123"],
			["endOfSoftwareMaintenanceAfter"] = ["1700000000123"],
			["nextHardwareMilestoneDateBefore"] = ["1700000000123"],
			["nextHardwareMilestoneDateAfter"] = ["1700000000123"],
			["nextSoftwareMilestoneDateBefore"] = ["1700000000123"],
			["nextSoftwareMilestoneDateAfter"] = ["1700000000123"],
			["hardwareLastDateOfSupportBefore"] = ["1700000000123"],
			["hardwareLastDateOfSupportAfter"] = ["1700000000123"],
			["softwareLastDateOfSupportBefore"] = ["1700000000123"],
			["softwareLastDateOfSupportAfter"] = ["1700000000123"],
			["hasCriticalOrHighSecurityAdvisories"] = ["true"],
			["max"] = ["2"],
			["offset"] = ["2"],
			["sort"] = ["lastSignalDate"],
			["order"] = ["DESC"],
			["fields"] = ["assetId,productId"]
		});
		await client.Assets.GetAssetsAsync(new AssetFilter
		{
			ProductFamily = ["a value", "b&two"],
			ProductId = ["a value", "b&two"],
			SerialNumber = ["a value", "b&two"],
			Hostname = ["a value", "b&two"],
			IpAddress = ["a value", "b&two"],
			EquipmentType = ["a value", "b&two"],
			ProductType = ["a value", "b&two"],
			SoftwareType = ["a value", "b&two"],
			SoftwareVersion = ["a value", "b&two"],
			Role = ["a value", "b&two"],
			Importance = ["a value", "b&two"],
			Location = ["a value", "b&two"],
			AssetTags = ["a value", "b&two"],
			DataSource = ["a value", "b&two"],
			ContractNumber = ["a value", "b&two"],
			ContractId = ["a value", "b&two"],
			ContractStatus = ["a value", "b&two"],
			CoverageStatus = ["a value", "b&two"],
			SupportType = ["a value", "b&two"],
			SupportTier = ["a value", "b&two"],
			PartnerName = ["a value", "b&two"],
			TelemetryStatus = ["a value", "b&two"],
			LastSignalType = ["a value", "b&two"],
			CurrentHardwareMilestone = ["a value", "b&two"],
			CurrentSoftwareMilestone = ["a value", "b&two"],
			NextHardwareMilestone = ["a value", "b&two"],
			NextSoftwareMilestone = ["a value", "b&two"],
			LastSignalBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			LastSignalAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			CoverageEndBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			CoverageEndAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			WarrantyEndBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			WarrantyEndAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			ShipDateBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			ShipDateAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			EndOfSoftwareMaintenanceBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			EndOfSoftwareMaintenanceAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			NextHardwareMilestoneDateBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			NextHardwareMilestoneDateAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			NextSoftwareMilestoneDateBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			NextSoftwareMilestoneDateAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			HardwareLastDateOfSupportBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			HardwareLastDateOfSupportAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			SoftwareLastDateOfSupportBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			SoftwareLastDateOfSupportAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			HasCriticalOrHighSecurityAdvisories = true,
			Max = 2,
			Offset = 2,
			Sort = "lastSignalDate",
			Order = CiscoIqSortOrder.Descending,
			Fields = "assetId,productId"
		});
		expected.Enqueue(new Dictionary<string, string[]> {
			["contractNumber"] = ["a value", "b&two"],
			["contractStatus"] = ["a value", "b&two"],
			["serviceLevel"] = ["a value", "b&two"],
			["supportTier"] = ["a value", "b&two"],
			["partnerName"] = ["a value", "b&two"],
			["contractEndBefore"] = ["1700000000123"],
			["contractEndAfter"] = ["1700000000123"]
		});
		await client.Assets.GetContractsAsync(new ContractFilter
		{
			ContractNumber = ["a value", "b&two"],
			ContractStatus = ["a value", "b&two"],
			ServiceLevel = ["a value", "b&two"],
			SupportTier = ["a value", "b&two"],
			PartnerName = ["a value", "b&two"],
			ContractEndBefore = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123),
			ContractEndAfter = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123)
		});
		expected.Enqueue(new Dictionary<string, string[]> {
			["impact"] = ["a value", "b&two"],
			["vulnerabilityStatus"] = ["a value", "b&two"]
		});
		await client.Assessments.GetSecurityAdvisoriesForAssetAsync("asset", new SecurityAdvisoryFilter
		{
			Impact = ["a value", "b&two"],
			VulnerabilityStatus = ["a value", "b&two"]
		});
		expected.Enqueue(new Dictionary<string, string[]> {
			["impact"] = ["a value", "b&two"],
			["vulnerabilityStatus"] = ["a value", "b&two"]
		});
		await client.Assessments.GetFieldNoticesForAssetAsync("asset", new FieldNoticeFilter
		{
			Impact = ["a value", "b&two"],
			VulnerabilityStatus = ["a value", "b&two"]
		});
		expected.Enqueue([]);
		await client.Assets.GetAssetsAsync(new AssetFilter());
		expected.Should().BeEmpty();
	}
}
