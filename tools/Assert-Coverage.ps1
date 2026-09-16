param(
	[Parameter(Mandatory)][string]$Path,
	[double]$MinimumLineCoverage = 100,
	[double]$MinimumBranchCoverage = 0
)

$ErrorActionPreference = 'Stop'
[xml]$report = Get-Content -LiteralPath $Path -Raw
$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$lines = [int]::Parse($report.coverage.GetAttribute('lines-valid'), $invariant)
$lineCoverage = 100 * [double]::Parse($report.coverage.GetAttribute('line-rate'), $invariant)
$branchCoverage = 100 * [double]::Parse($report.coverage.GetAttribute('branch-rate'), $invariant)
if ($lines -le 0) { throw 'The coverage report contains no instrumented library lines.' }
Write-Output ('Coverage: {0:N2}% lines, {1:N2}% branches ({2} library lines).' -f $lineCoverage, $branchCoverage, $lines)
if ($lineCoverage -lt $MinimumLineCoverage -or $branchCoverage -lt $MinimumBranchCoverage) {
	throw "Coverage is below the required threshold: $MinimumLineCoverage% lines, $MinimumBranchCoverage% branches."
}
