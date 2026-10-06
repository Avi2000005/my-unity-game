# Sweep she/her -> he/him across every C# file in the project.
#
# Ari is male. 1067 occurrences across 70 files, so this is a script and not an
# edit — hand-editing them is how you miss four.
#
# ------------------------------------------------------------------ SAFETY --
# This is the second attempt. The first one destroyed 70 files.
#
# What went wrong, measured afterwards:
#   * the replacement was a PowerShell `switch`, which is case-insensitive and
#     does not stop at a matching branch. 'she' matched 'She' AND 'SHE', both
#     wrote output, the evaluator returned an array, and the regex engine
#     joined it: she became "He HE" and the case of the original was gone.
#   * the repair script then did `$out = New-Object string[] $lines.Count`,
#     which came back NULL, and wrote `$out -join $nl` — the empty string —
#     over every file it touched.
#   * and it printed "repaired 562", which was counted from `$line` in memory
#     and had never reached disk. The report was true and the files were empty.
#
# So this version refuses to run unless every one of these hold:
#   1. `-WhatIf` mode by default. Nothing is written unless -Apply is passed.
#   2. Every input file is copied to Tools\_pronoun_backup\ first.
#   3. The new text is built in memory, checked for a non-zero length, and
#      checked to still contain "class"/"namespace"/"using" for .cs files.
#   4. `Set-Content` is never used. WriteAllText is preceded by a length
#      assertion, and the file's new length is read back and verified.
#   5. Files whose she/her are part of a search literal are skipped, because
#      rewriting them breaks the tool that looks for them.
#   6. Every line the rewrite leaves with two adjacent male pronouns is listed
#      for a human, because "his him" can mean Ari and Mono and is not
#      something a regex can decide.

param(
    [switch]$Apply,
    [string]$Root = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter\Assets\Painterly',
    [string]$Backup = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter\Tools\_pronoun_backup'
)

# Files that search for the words "she" and "her" as data. Rewriting the
# literals breaks the search, and these are the tools, not the game.
$Never = @('L1Fixups.cs')

# Words after which "her" is the OBJECT of a verb or a preposition, so it
# becomes "him". Everything else is possessive, so it becomes "his".
# English uses one pronoun for both cases, so the replacement cannot be
# positional: "behind her" is 'him', "her feet" is 'his'.
$Objective = @(
    'to','at','from','with','behind','for','towards','toward','past','into',
    'beside','near','without','around','below','before','after','against',
    'next','facing','aimed','aim','catch','hit','hurt','reach','stop','seen',
    'put','puts','sees','see','lets','let','move','leave','leaves','drops',
    'gives','give','carries','carry','knows','knew','given','hides','hide',
    'notice','notices','keeps','keep','gave','walk','walks','pick','picks',
    'have','has','loses','lose','pose','drop','shove','shoves','shoving',
    'follows','follow','tells','tell','watches','watch','sends','send',
    'shows','show','reaches','turned','turn','turns','sits','sit','stands',
    'stand','pulls','pull','pushes','push','threw','throw','left','warned',
    'warn','greets','greet','blocks','block','tethered','tether','owed',
    'beaten','struck','strike','flanks','flank','chases','chase','pins',
    'pin','cover','covers','beneath','over','under','across','off','out',
    'through','back','down','up','in','on','by','onto','about','round'
)

# Case-preserving map, built with explicit branches. No switch: a switch here
# is exactly what destroyed the files.
function Convert-She([string]$w) {
    if ($w -ceq 'She') { return 'He' }
    if ($w -ceq 'SHE') { return 'HE' }
    return 'he'
}

function Convert-Her([string]$w, [string]$prev) {
    $obj = $Objective -contains $prev
    if ($obj) {
        if ($w -ceq 'HER') { return 'HIM' }
        if ($w -ceq 'Her') { return 'Him' }
        return 'him'
    }
    if ($w -ceq 'HER') { return 'HIS' }
    if ($w -ceq 'Her') { return 'His' }
    return 'his'
}

$files = Get-ChildItem $Root -Recurse -Include *.cs |
         Where-Object { $Never -notcontains $_.Name }

$sheRe = [regex]'\b(She|she|SHE)\b'
$herRe = [regex]'\b(Her|her|HER)\b'

$totalShe = 0
$totalHer = 0
$changed = 0
$wouldChange = 0
$review = New-Object System.Collections.Generic.List[string]
$plan = New-Object System.Collections.Generic.List[object]

foreach ($f in $files) {
    $src = [System.IO.File]::ReadAllText($f.FullName)
    $out = [regex]::Replace($src, '\b([A-Za-z]+)(\s+)(Her|her|HER)\b', {
        param($m)
        $prev = $m.Groups[1].Value.ToLowerInvariant()
        return $m.Groups[1].Value + $m.Groups[2].Value +
               (Convert-Her $m.Groups[3].Value $prev)
    })
    $out = [regex]::Replace($out, '(?<![A-Za-z])(Her|her|HER)\b', {
        param($m)
        return (Convert-Her $m.Groups[1].Value '')
    })
    $out = $sheRe.Replace($out, { param($m) Convert-She $m.Groups[1].Value })

    $totalShe += $sheRe.Matches($src).Count
    $totalHer += $herRe.Matches($src).Count

    if ($out -eq $src) { continue }

    $changed++
    $plan.Add([pscustomobject]@{ Path = $f.FullName; New = $out })

    foreach ($line in ($out -split "`r?`n")) {
        if ($line -match '(?i)\b(his|him|he)\s+(his|him|he)\b') {
            $review.Add(('{0}: {1}' -f $f.Name, $line.Trim()))
        }
    }
}

Write-Output ("she   occurrences : {0}" -f $totalShe)
Write-Output ("her   occurrences : {0}" -f $totalHer)
Write-Output ("files that change: {0} of {1}" -f $changed, $files.Count)
Write-Output ""

if (-not $Apply) {
    Write-Output "DRY RUN. Nothing written. Re-run with -Apply to do it."
    Write-Output ""
    Write-Output "=== files that would change ==="
    foreach ($e in $plan) { "  {0}" -f $e.Path.Replace($Root + '\', '') }
    Write-Output ""
    Write-Output ("=== lines left with two adjacent male pronouns: {0} ===" -f $review.Count)
    Write-Output "these are NOT repairable by rule. 'him' may have meant Mono."
    $review | Select-Object -First 40 | ForEach-Object { "  $_" }
    return
}

# ---- back up before touching anything ---------------------------------------
New-Item -ItemType Directory -Force -Path $Backup | Out-Null
foreach ($e in $plan) {
    $rel = $e.Path.Replace($Root + '\', '')
    $dest = Join-Path $Backup $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item $e.Path $dest -Force
}
Write-Output ("backed up {0} files to {1}" -f $plan.Count, $Backup)

# ---- write, with an assertion after every single write ----------------------
$written = 0
$failed = 0
foreach ($e in $plan) {
    if ($e.New.Length -lt 200) {
        Write-Output ("REFUSED  {0}  new text is only {1} chars" -f $e.Path, $e.New.Length)
        $failed++
        continue
    }
    if (-not ($e.New -match 'using |namespace |class ')) {
        Write-Output ("REFUSED  {0}  new text has no code in it" -f $e.Path)
        $failed++
        continue
    }

    [System.IO.File]::WriteAllText($e.Path, $e.New)

    # Compare BYTES with BYTES.
    #
    # This line was `$now -ne $e.New.Length`, and it rolled back all 48 files.
    # `(Get-Item).Length` is bytes on disk; `$e.New.Length` is characters in
    # memory. Every file in this project contains em-dashes and curly
    # apostrophes, which are 3 bytes and 2 UTF-16 units respectively, so the
    # byte count is always the larger of the two and the assertion can never
    # pass. The guard did not catch a fault - it *was* the fault, it just
    # happened to fail safe instead of failing loud.
    #
    # The same mistake made a recovery script earlier discard 17 of 20 good
    # files, for the same reason. If a length assertion here ever fires on a
    # file that looks perfectly fine, the assertion is what is broken.
    $wantBytes = [System.Text.Encoding]::UTF8.GetByteCount($e.New)
    $now = (Get-Item $e.Path).Length
    if ($now -lt 200 -or $now -ne $wantBytes) {
        # Put the backup straight back. This is the whole point of having one.
        $b = Join-Path $Backup $e.Path.Replace($Root + '\', '')
        Copy-Item $b $e.Path -Force
        Write-Output ("ROLLED BACK {0}  wrote {1} bytes, expected {2}" -f $e.Path, $now, $wantBytes)
        $failed++
        continue
    }

    # Read it back and compare the TEXT, not the length. A length check proves
    # bytes arrived; it does not prove they are the right bytes. This does.
    $readBack = [System.IO.File]::ReadAllText($e.Path)
    if ($readBack -ne $e.New) {
        $b = Join-Path $Backup $e.Path.Replace($Root + '\', '')
        Copy-Item $b $e.Path -Force
        Write-Output ("ROLLED BACK {0}  the text on disk is not the text written" -f $e.Path)
        $failed++
        continue
    }
    $written++
}

Write-Output ""
Write-Output ("written {0}, failed {1}" -f $written, $failed)
Write-Output ""
Write-Output ("=== lines left with two adjacent male pronouns: {0} ===" -f $review.Count)
$review | Select-Object -First 60 | ForEach-Object { "  $_" }