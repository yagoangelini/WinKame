# WinKame, a morandi.cloud product. Copyright (C) 2026 Thales de Oliveira Morandi.
# SPDX-License-Identifier: GPL-3.0-or-later. See LICENSE and NOTICE.
# Quick check while iterating: skips the slow tests (PDF/image compression, audio/video, spreadsheets)
# and prints only failures and the summary. CI and releases still run the full suite.
#   ./scripts/test-fast.ps1          fast tests
#   ./scripts/test-fast.ps1 -All     everything
#   ./scripts/test-fast.ps1 Mailto   only tests whose name contains "Mailto"
param([string]$Name = "", [switch]$All)
$root = Split-Path -Parent $PSScriptRoot
$filter = @()
if (-not $All) { $filter += "Category!=Slow" }
if ($Name) { $filter += "FullyQualifiedName~$Name" }
$args2 = @("test", (Join-Path $root "tests\WinKame.Tests"), "-c", "Debug", "-nologo", "-v", "q")
if ($filter.Count -gt 0) { $args2 += @("--filter", ($filter -join "&")) }
# Through cmd so PowerShell 5.1 does not turn dotnet's stderr into error records.
$out = cmd /c ("dotnet " + (($args2 | ForEach-Object { if ($_ -match '[ &|]') { '"' + $_ + '"' } else { $_ } }) -join " ") + " 2>&1")
$code = $LASTEXITCODE
$out | Where-Object { $_ -match "error|\[FAIL\]|Falha|Failed|Aprovado!|Passed!|Com falha|Total" } | Select-Object -First 40
exit $code
