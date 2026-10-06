<#
    Makes BeatText.Pages verify each page it produces, using the same
    measurement BeatText.Fit uses, and shortens a page until it passes.

    WHY THIS EXISTS
    ---------------
    Complaint 2 is "text at least 5x current size". BeatText.Pages was written
    to satisfy it by never shrinking: the caller hands Block one page, and the
    page was built to fit at exactly the 5x target, so the fit Block performs
    is a no-op. BeatHud and MonoCompanion now call Fit zero times.

    That no-op depends on one assumption that has not been checked:

        Pages wraps the text by measuring each word with CalcSize, and assumes
        the result occupies the same number of lines as Unity's own wrapping
        of that text, which is what Fit and Block measure with CalcHeight.

    Those are two different measurements of the same string, and they disagree
    whenever a word measures wider than the sum of its parts, or a line carries
    a taller descender than the "Wjq" probe string LinesThatFit counted with.
    If they disagree, the page overflows, Fit silently shrinks it, complaint 2
    is back, and the paging is still sitting in the file looking like it worked.

    So the page is measured the way Fit measures it, and shortened by a line at
    a time until it fits. After this, "the page fits at the 5x size" is a fact
    the code checked rather than a promise it made.

    A single line that still overflows on its own is left alone. That is the
    one case where shrinking is correct - a paragraph with no spaces in it
    cannot be paged - and Shortfalls reports it rather than hiding it.

    WHY A LINE-INDEX SPLICE AND NOT A STRING REPLACE
    ----------------------------------------------
    Three attempts at an exact-string replace of this block failed today, all
    on indentation: the same file uses 4-spaces-plus-1-tab at member level,
    4-spaces-plus-3-tabs inside Pages, and 4-spaces-plus-4-tabs inside its
    nested block. Reading the bytes is the only way to get this right here, so
    the block is located by its content and replaced by index. The range
    440..460 is not hard coded - it is searched for, and the search is
    asserted to land on the block that was inspected before any write.

    SAFETY
    ------
    Dry run unless -Apply. Asserts the exact 21 lines it expects to find, so a
    file that has moved on is refused rather than half-spliced. Braces are
    balanced in the result. Byte count is asserted against UTF8.GetByteCount,
    and the text is read back off disk and compared, because a byte count
    proves bytes arrived and not that they are the right bytes.
#>

[CmdletBinding()]
param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

$File   = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter\Assets\Painterly\Scripts\BeatText.cs'
$Backup = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter\Tools\_scratch_backup\BeatText.cs.pagedfit'

$nl = "`r`n"
$S  = '    '                      # 4 spaces
$M  = $S + "`t"                   # 4 spaces + 1 tab  - member level
$L3 = $S + "`t`t`t"               # 4 spaces + 3 tabs - Pages body
$L4 = $L3 + "`t"
$L5 = $L4 + "`t"
$L6 = $L5 + "`t"
$L7 = $L6 + "`t"

# ---------------------------------------------------------------------------
# The block to replace, exactly as it exists on disk today.
# ---------------------------------------------------------------------------

# The expected old lines, with the right indentation per level.
$ExpectedOld = @(
    $L3 + "if (lines.Count == 0)"
    $L3 + "{"
    $L4 + "pages.Add(text);"
    $L3 + "}"
    $L3 + "else"
    $L3 + "{"
    $L4 + "for (int i = 0; i < lines.Count; i += perPage)"
    $L4 + "{"
    $L5 + "int end = Mathf.Min(lines.Count, i + perPage);"
    $L5 + "var page = new System.Text.StringBuilder(256);"
    $L5 + "for (int k = i; k < end; k++)"
    $L5 + "{"
    $L6 + "if (page.Length > 0)"
    $L6 + "{"
    $L6 + "page.Append('\n');"
    $L6 + "}"
    $L6 + "page.Append(lines[k]);"
    $L5 + "}"
    $L5 + "pages.Add(page.ToString());"
    $L4 + "}"
    $L3 + "}"
)

$New = @(
    $L3 + "if (lines.Count == 0)"
    $L3 + "{"
    $L4 + "pages.Add(text);"
    $L3 + "}"
    $L3 + "else"
    $L3 + "{"
    $L4 + "// Each page is measured with the SAME call Fit makes before it is"
    $L4 + "// accepted, and shortened a line at a time until it passes."
    $L4 + "//"
    $L4 + "// This is not belt and braces. The wrapping above and Unity's own"
    $L4 + "// wrapping are two different measurements of the same string - one"
    $L4 + "// is CalcSize per word, the other CalcHeight over the joined result"
    $L4 + "// - and they disagree whenever a word measures wider than the sum of"
    $L4 + "// its parts, or a line carries a taller descender than the probe"
    $L4 + "// string LinesThatFit counted with. If they disagree and this loop"
    $L4 + "// is not here, Fit quietly shrinks the page, complaint 2 is back, and"
    $L4 + "// the paging is still in the file looking like it worked. A page that"
    $L4 + "// was never checked at its own size is a promise, not a fact."
    $L4 + "//"
    $L4 + "// A single line that overflows on its own is left alone, because it"
    $L4 + "// cannot be paged - a run of characters with no space in it has"
    $L4 + "// exactly one page - and Shortfalls reports it rather than hiding it."
    $L4 + "int i = 0;"
    $L4 + "while (i < lines.Count)"
    $L5 + "{"
    $L5 + "int end = Mathf.Min(lines.Count, i + perPage);"
    $L5 + "string page = Join(lines, i, end);"
    $L5 + "int k = end;"
    $L5 + "while (k > i + 1 && probe.CalcHeight(new GUIContent(page), width) > maxHeight)"
    $L6 + "{"
    $L6 + "k--;"
    $L6 + "page = Join(lines, i, k);"
    $L6 + "}"
    $L5 + "pages.Add(page);"
    $L5 + "i = k;"
    $L5 + "}"
    $L3 + "}"
)

$Helper = @(
    $M + "/// <summary>Lines from, to, joined with newlines. to is exclusive.</summary>"
    $M + "private static string Join(List<string> lines, int from, int to)"
    $M + "{"
    $L3 + "if (to - from <= 1)"
    $L3 + "{"
    $L4 + "return lines[from];"
    $L3 + "}"
    $L3 + "var sb = new System.Text.StringBuilder(256);"
    $L3 + "for (int i = from; i < to; i++)"
    $L3 + "{"
    $L4 + "if (sb.Length > 0)"
    $L4 + "{"
    $L5 + "sb.Append('\n');"
    $L4 + "}"
    $L4 + "sb.Append(lines[i]);"
    $L3 + "}"
    $L3 + "return sb.ToString();"
    $M + "}"
)

# ---------------------------------------------------------------------------

$raw = [System.IO.File]::ReadAllText($File)
$lines = $raw -split "`r`n", -1
$before = (Get-Item $File).Length

Write-Output ''
Write-Output 'BeatText - verify every page at its own size'
Write-Output ('file {0} B, {1} CRLF segments' -f $before, $lines.Count)

# --- locate -----------------------------------------------------------------
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Trim() -eq 'if (lines.Count == 0)') { $start = $i; break }
}
if ($start -lt 0) { Write-Output 'ANCHOR MISSING - nothing written.'; return }

# The block closes at the SECOND '}' at Pages' own indent level: the first one
# closes the if, the second closes the else. Finding the first is how the
# earlier attempt spliced 4 lines instead of 21.
$closes = @()
for ($i = $start; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -eq $L3 + '}') { $closes += $i }
    if ($closes.Count -eq 2) { break }
}
if ($closes.Count -lt 2) { Write-Output 'CLOSING BRACE NOT FOUND - nothing written.'; return }
$end = $closes[1]

# --- assert what is there is what we expect ---------------------------------
$actual = @()
for ($i = $start; $i -le $end; $i++) { $actual += $lines[$i] }

Write-Output ''
Write-Output ('replacing lines {0}..{1} (1-based {2}..{3}), {4} lines' -f $start, $end, ($start + 1), ($end + 1), $actual.Count)
Write-Output ('expected {0} lines' -f $ExpectedOld.Count)

if ($actual.Count -ne $ExpectedOld.Count) {
    Write-Output ''
    Write-Output 'NOT WRITTEN - the block is not the size that was inspected.'
    for ($i = 0; $i -lt [Math]::Max($actual.Count, $ExpectedOld.Count); $i++) {
        $a = if ($i -lt $actual.Count) { $actual[$i].Trim() } else { '(none)' }
        $b = if ($i -lt $ExpectedOld.Count) { $ExpectedOld[$i].Trim() } else { '(none)' }
        $mark = if ($a -eq $b) { '  ' } else { '!!' }
        Write-Output ('  {0} {1,3}  disk: {2,-52} expected: {3}' -f $mark, $i, $a, $b)
    }
    return
}

$mismatch = 0
for ($i = 0; $i -lt $actual.Count; $i++) {
    if ($actual[$i] -cne $ExpectedOld[$i]) { $mismatch++ }
}
if ($mismatch -gt 0) {
    Write-Output ''
    Write-Output ("NOT WRITTEN - {0} of {1} lines differ from the inspected block." -f $mismatch, $actual.Count)
    for ($i = 0; $i -lt $actual.Count; $i++) {
        if ($actual[$i] -cne $ExpectedOld[$i]) {
            Write-Output ('  line {0}' -f ($start + $i + 1))
            Write-Output ('    disk: |{0}|' -f $actual[$i])
            Write-Output ('    want: |{0}|' -f $ExpectedOld[$i])
        }
    }
    return
}

Write-Output '  all 21 lines match the inspected block exactly.'

# --- locate the PageSeconds doc comment, for the helper ---------------------
$anchorIdx = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'Seconds a page stays on screen') { $anchorIdx = $i; break }
}
if ($anchorIdx -lt 1) {
    Write-Output ''
    Write-Output 'NOT WRITTEN - the PageSeconds doc comment is not where it was.'
    return
}
$anchorIdx--   # step onto the '/// <summary>' line above it

$lead = [regex]::Match($lines[$anchorIdx], '^[\s]*').Value
Write-Output ''
Write-Output ('helper goes before line {0}, member indent {1} chars: {2}' -f ($anchorIdx + 1), $lead.Length, ($lead -replace "`t", '<T>'))
if ($lead -ne $M) {
    Write-Output ("NOT WRITTEN - that indent is not the member indent this file uses ({0} expected)." -f ($M -replace "`t", '<T>'))
    return
}

# --- plan -------------------------------------------------------------------
# NOTE ON STYLE, BECAUSE IT COST REAL TIME:
# This loop uses if / elseif / else and contains no `continue` at all.
# A `continue` written on the same line as an inner foreach's closing brace
# binds to that FOREACH, not to the loop around it. The outer loop therefore
# never advanced, and the splice quietly produced 133 lines out of 587. The
# result was still refused, but it was refused for the wrong reason - and a
# guard that fires for the wrong reason teaches you nothing.
$out = New-Object System.Collections.ArrayList
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($i -eq $start) {
        foreach ($r in $New) { [void]$out.Add($r) }
    }
    elseif ($i -ge $start -and $i -le $end) {
        # dropped - this is the block being replaced. Both bounds are needed:
        # `$i -le $end` alone is true for every line ABOVE $start too, which
        # silently deleted the 440 lines in front of the block.
    }
    elseif ($i -eq $anchorIdx) {
        foreach ($h in $Helper) { [void]$out.Add($h) }
        [void]$out.Add('')
        [void]$out.Add($lines[$i])
    }
    else {
        [void]$out.Add($lines[$i])
    }
}

# Line count is asserted, not assumed. The brace check below caught the bad
# splice, but only by accident; this is what was actually wrong with it.
$wantLines = ($lines.Count - ($end - $start + 1) + $New.Count + $Helper.Count + 1)
if ($out.Count -ne $wantLines) {
    Write-Output ''
    Write-Output ("NOT WRITTEN - splice produced {0} lines, expected {1}." -f $out.Count, $wantLines)
    return
}

$text = $out -join $nl

$ob = ([regex]::Matches($text, '\{')).Count
$cb = ([regex]::Matches($text, '\}')).Count
Write-Output ''
Write-Output ('result: {0} B (was {1}, {2:+#;-#;0})   braces {3}/{4}' -f `
    [System.Text.Encoding]::UTF8.GetByteCount($text), $before,
    ([System.Text.Encoding]::UTF8.GetByteCount($text) - $before), $ob, $cb)

if ($ob -ne $cb) {
    Write-Output ''
    Write-Output ("NOT WRITTEN - braces would not balance ({0} open, {1} close)." -f $ob, $cb)
    return
}

if (-not $Apply) {
    Write-Output ''
    Write-Output 'DRY RUN. Nothing written. Re-run with -Apply.'
    return
}

# --- write, then verify ------------------------------------------------------
New-Item -ItemType Directory -Force -Path (Split-Path $Backup) | Out-Null
Copy-Item $File $Backup -Force
Write-Output ''
Write-Output ("backed up to {0} ({1} B)" -f $Backup, (Get-Item $Backup).Length)

[System.IO.File]::WriteAllText($File, $text)

$want = [System.Text.Encoding]::UTF8.GetByteCount($text)
$now = (Get-Item $File).Length
if ($now -ne $want) {
    Copy-Item $Backup $File -Force
    Write-Output ("ROLLED BACK - wrote {0} bytes, expected {1}" -f $now, $want)
    return
}

$back = [System.IO.File]::ReadAllText($File)
if ($back -cne $text) {
    Copy-Item $Backup $File -Force
    Write-Output 'ROLLED BACK - the text on disk is not the text written.'
    return
}

Write-Output ("wrote {0} B, byte count matches, read-back identical" -f $now)
$bn = [System.IO.File]::ReadAllText($File)
Write-Output ''
Write-Output 'markers in the file now:'
foreach ($mk in @('private static string Join(', 'while (i < lines.Count)', 'probe.CalcHeight(new GUIContent(page), width)', 'while (k > i + 1')) {
    Write-Output ('  {0,-54} {1}' -f $mk, ([regex]::Matches($bn, [regex]::Escape($mk))).Count)
}
Write-Output ('  braces {0}/{1}' -f ([regex]::Matches($bn, '\{')).Count, ([regex]::Matches($bn, '\}')).Count)