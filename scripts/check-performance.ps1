param([string]$OutputRoot = (Join-Path $PSScriptRoot '..\dist'))

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($OutputRoot)
$index = Join-Path $root 'index.html'
$html = [System.IO.File]::ReadAllText($index)
$patterns = @(
    'href="(?<path>/assets/site\.[0-9a-f]{10}\.css)"',
    'src="(?<path>/assets/enhance\.[0-9a-f]{10}\.js)"'
)
$total = (Get-Item -LiteralPath $index).Length
foreach ($pattern in $patterns) {
    $match = [regex]::Match($html, $pattern)
    if (-not $match.Success) {
        throw "Missing hashed asset reference matching '$pattern' in $index."
    }
    $relative = $match.Groups['path'].Value.TrimStart('/').Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $total += (Get-Item -LiteralPath (Join-Path $root $relative)).Length
}

$budget = 150 * 1024
Write-Host ('Home HTML + CSS + JS: {0:N0} bytes ({1:N1} KiB); budget: < {2:N0} bytes.' -f $total, ($total / 1024), $budget)
if ($total -ge $budget) {
    throw "Home page payload is not below the $budget byte budget."
}
