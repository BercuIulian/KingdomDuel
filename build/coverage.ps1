# Testele cu coverage, raportul combinat și pragurile: ./build/coverage.ps1 (-Open deschide raportul).
param([switch]$Open)

Set-Location (Split-Path $PSScriptRoot -Parent)
Remove-Item TestResults, coverage -Recurse -Force -ErrorAction SilentlyContinue

dotnet test --solution KingdomDuel.sln `
    --coverlet `
    --coverlet-output-format cobertura `
    --coverlet-include "[KingdomDuel.*]*" `
    --coverlet-exclude "[KingdomDuel.*Tests]*" `
    --coverlet-exclude-by-file "**/Migrations/*.cs" `
    --coverlet-exclude-by-file "**/ApiClients/ApiClient.cs" `
    --coverlet-exclude-by-file "**/Program.cs" `
    --coverlet-exclude-by-attribute GeneratedCode `
    --coverlet-skip-auto-props `
    --results-directory TestResults
$tests = $LASTEXITCODE

dotnet tool restore | Out-Null
dotnet reportgenerator "-reports:TestResults/**/*.cobertura*.xml" "-targetdir:coverage" "-reporttypes:Html;MarkdownSummaryGithub;JsonSummary" "-verbosity:Warning"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run build/coverage-gate.cs -- coverage/Summary.json
$gate = $LASTEXITCODE

if ($Open) { Invoke-Item coverage/index.html }

if ($tests -ne 0) { exit $tests }
exit $gate
