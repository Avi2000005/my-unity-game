param(
  [Parameter(Mandatory=$true)] [string]$Call,
  [int]$Timeout = 55000
)

# Drives Unity's official `unity mcp` server through a local Node bridge.
#
# -Call points at a JSON file containing {"tool": "...", "args": {...}}.
#
# The payload travels by file rather than as a command-line argument because
# PowerShell consumes the double quotes from a quoted native-command argument.
# That happens twice over — once for this script's own -Args, and again inside
# it for the node call — so a JSON object passed as an argument always arrives
# as {file:...} and fails to parse. A file has no shell between the writer and
# the reader.
$ErrorActionPreference = 'Stop'

$path = $Call
if (-not [System.IO.Path]::IsPathRooted($path)) { $path = Join-Path $PSScriptRoot $Call }
$doc = Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json

$env:MCP_ARGS = $doc.args | ConvertTo-Json -Depth 20 -Compress
$env:MCP_TIMEOUT_MS = [string]$Timeout

node (Join-Path $PSScriptRoot 'unity-mcp.js') call $doc.tool
$code = $LASTEXITCODE

$env:MCP_ARGS = ''
exit $code
