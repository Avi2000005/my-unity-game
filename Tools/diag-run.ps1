param([string[]]$Files)
foreach ($f in $Files) {
  $j = @{ tool = "run_script"; args = @{ file = $f; entry = "__no_entry__"; timeout_ms = 30000 } } | ConvertTo-Json -Depth 5
  $tmp = "call-diag.json"
  Set-Content -Path $tmp -Value $j -Encoding UTF8
  Write-Output "########## $f"
  .\mcp.ps1 -Call $tmp -Timeout 45000
}
