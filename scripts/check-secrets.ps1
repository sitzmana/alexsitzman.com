$ErrorActionPreference = 'Stop'

# Flags anything that looks like a committed credential. Intentionally noisy:
# a false positive costs a glance, a false negative costs a rotation.
$pattern = '(?i)(api[_-]?key\s*[:=]|client[_-]?secret|password\s*[:=]|passwd\s*[:=]|BEGIN [A-Z ]*PRIVATE KEY|AccountKey=|DefaultEndpointsProtocol=|xox[baprs]-|gh[pousr]_[A-Za-z0-9]{20,}|eyJ[A-Za-z0-9_-]{20,}\.)'

$files = Get-ChildItem -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|dist|\.git|TestResults)[\\/]' -and
        $_.Extension -notin '.dll', '.exe', '.pdb', '.png', '.jpg', '.ico' -and
        # The scanner's own pattern would otherwise match itself.
        $_.FullName -ne $PSCommandPath
    }

$hits = $files | Select-String -Pattern $pattern

if ($hits) {
    Write-Host 'Potential secrets found:' -ForegroundColor Red
    $hits | ForEach-Object { '  {0}:{1}' -f $_.Path, $_.LineNumber }
    exit 1
}

Write-Host "Scanned $($files.Count) files. No credential-like strings found." -ForegroundColor Green
