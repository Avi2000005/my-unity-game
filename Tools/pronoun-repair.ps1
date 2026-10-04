# Repair the fall-through the pronoun sweep caused.
#
# WHAT HAPPENED, because it is the reason this file exists.
#
# PowerShell's `switch` is case-insensitive and does NOT stop at a matching
# branch unless you `break`. The sweep matched 'she' against branches 'She' AND
# 'SHE', both matched, both wrote their output, and the MatchEvaluator returned
# an array which the regex engine joined with a space.
#
#   she -> "He HE"      (509 occurrences)
#   her -> "HIS His"    (53 occurrences, the bare ones only; the
#                        "preceded by a word" pass used if/else and returned a
#                        single string, so that half is intact)
#
# MEASURED, not assumed: 'He HE' = 509, 'HIS His' = 53, and every other
# doubling of the same shape = 0. So there are exactly two things to fix and no
# third variant hiding somewhere.
#
# The case of the original word is gone: 'she', 'She' and 'SHE' all collapsed
# to the same text. That is real damage to real files with no git to undo it.
#
# What IS recoverable is the case the sentence wanted, and it is recoverable
# almost everywhere:
#
#   start of a sentence, or after . ! ?   ->  "He"  /  "His"
#   inside an ALL-CAPS report line         ->  "HE"  /  "HIS"
#   anywhere else                          ->  "he"  /  "his"
#
# Every line the rule touches is printed in the summary counts, and the ones
# where the pronoun is now ambiguous - "his him", i.e. a line that names two
# male pronouns where before it named one of each gender - are listed at the
# end, because those are not repairable by case rule at all. Those are lines
# where 'him' may have meant Mono or the crawler.

$root = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter\Assets\Painterly'

$files = Get-ChildItem $root -Recurse -Include *.cs |
         Where-Object { $_.Name -ne 'L1Fixups.cs' }

$fixHe = 0
$fixHis = 0
$unsure = New-Object System.Collections.Generic.List[string]
$touched = 0

foreach ($f in $files) {
    $src = [System.IO.File]::ReadAllText($f.FullName)
    if ($src -notmatch 'He HE|HIS His') { continue }

    $nl = if ($src.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = $src -split "`r?`n"
    $out = New-Object System.Collections.Generic[string[]] $lines.Count
    $touched++

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Is this a shouting line? The report strings are written in caps, and a
        # lower-case pronoun inside one of them is visibly wrong.
        $upper = ([regex]::Matches($line, '[A-Z]')).Count
        $lower = ([regex]::Matches($line, '[a-z]')).Count
        $caps = ($upper -gt 0) -and ($lower -lt $upper)

        foreach ($pair in @(@('He HE', 'he'), @('HIS His', 'his'))) {
            $pat = $pair[0]
            $low = $pair[1]

            $ms = [regex]::Matches($line, [regex]::Escape($pat))
            if ($ms.Count -eq 0) { continue }

            $built = ''
            $cursor = 0

            foreach ($m in $ms) {
                $built += $line.Substring($cursor, $m.Index - $cursor)

                $before = ''
                if ($m.Index -gt 0) {
                    $from = [Math]::Max(0, $m.Index - 3)
                    $before = $line.Substring($from, $m.Index - $from)
                }

                $tail = $m.Index + $m.Length
                $after = ''
                if ($tail -lt $line.Length) {
                    $after = $line.Substring($tail, [Math]::Min(2, $line.Length - $tail))
                }

                $sentenceStart =
                    ($before -match '^[\s\(\[\{""''*]*$') -or
                    ($before -match '[.!?]["''\)\]]*\s*$')

                if ($caps) { $repl = $low.ToUpperInvariant() }
                elseif ($sentenceStart) { $repl = $low.Substring(0, 1).ToUpperInvariant() + $low.Substring(1) }
                else { $repl = $low }

                $built += $repl
                $cursor = $tail

                if ($low -eq 'he') { $fixHe++ } else { $fixHis++ }
            }

            $built += $line.Substring($cursor)
            $line = $built
        }

        $out[$i] = $line
    }

    [System.IO.File]::WriteAllText($f.FullName, ($out -join $nl))

    # Lines that now name a male pronoun twice in a row: not case damage, a
    # referent question. Collected after writing so the file on disk is what
    # gets scanned.
    for ($i = 0; $i -lt $out.Count; $i++) {
        if ($out[$i] -match '(?i)\b(his|him|he)\s+(his|him|he)\b') {
            $unsure.Add(('{0}:{1}: {2}' -f $f.Name, ($i + 1), $out[$i].Trim()))
        }
    }
}

"repaired  " + $fixHe + " x 'He HE'  ->  he/He/HE"
"repaired  " + $fixHis + " x 'HIS His'  ->  his/His/HIS"
"files     " + $touched
""
"=== adjacent male pronouns, which are NOT case damage (need a human) : $($unsure.Count) ==="
$unsure | ForEach-Object { "  $_" }

Write-Output ""
Write-Output "=== leftovers of the broken pattern, must be 0 ==="
$left = 0
foreach ($f in $files) {
    $left += ([regex]::Matches([System.IO.File]::ReadAllText($f.FullName), 'He HE|HIS His')).Count
}
"count: $left"