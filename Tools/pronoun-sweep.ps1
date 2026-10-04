# Sweep she/her -> he/him across every C# file in the project.
#
# Ari is male. This exists as a script rather than as an edit because there are
# 1067 occurrences in 70 files and hand-editing them is how you miss four.
#
# Two rules, and the second one is the whole difficulty:
#   she -> he           always safe
#   her -> him or his   decided by the word in front of it
#
# English has one pronoun for both cases, so the replacement cannot be
# positional. "behind her" is 'him'; "her feet" is 'his'. Getting this wrong
# in a tooltip is embarrassing; getting it wrong in the log a developer reads
# to decide whether a bug is real is worse.
#
# L1Fixups.cs is excluded on purpose: it contains the literal strings "she"
# and "her" that it searches for. Rewriting those breaks the search.

$root = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter\Assets\Painterly'

# Words after which "her" is the OBJECT of a verb or a preposition, so it is
# "him". Everything else is a possessive, so it is "his".
$Objective = @(
  'to','at','from','with','behind','for','towards','toward','past','into',
  'beside','near','without','around','below','before','after','against',
  'next','facing','aimed','aim','catch','hit','hurt','reach','stop','seen',
  'put','puts','sees','see','lets','let','move','leave','leaves','drops',
  'gives','give','carries','carry','knows','knew','given','hides','hide',
  'notice','notices','keeps','keep','gave','walk','walks','pick','picks',
  'have','has','hunting','loses','lose','pose','drop','shove','shoves',
  'shoving','follows','follow','tells','tell','watches','watch','sends',
  'send','shows','show','reaches','toward','turned','turn','turns','sits',
  'sit','stands','stand','pulls','pull','pushes','push','threw','throw',
  'left','watches','warned','warn','greets','greet','blocks','block',
  'tethered','tether','owed','owe','beaten','beat','struck','strike',
  'flanks','flank','chases','chase','pins','pin','blocks','cover','covers',
  'behind','beside','beneath','over','under','across','around','off','out',
  'through','toward','back','down','up','in','on','by'
)

$files = Get-ChildItem $root -Recurse -Include *.cs |
         Where-Object { $_.Name -ne 'L1Fixups.cs' }

$she = [regex]'\b(She|she|SHE)\b'
$her = [regex]'\b(Her|her|HER)\b'

$totalShe = 0
$totalHer = 0
$changedFiles = 0

$review = New-Object System.Collections.Generic.List[string]

foreach ($f in $files) {
  $src = [System.IO.File]::ReadAllText($f.FullName)
  $orig = $src

  # --- she -> he -----------------------------------------------------------
  $src = $she.Replace($src, { param($m)
    switch ($m.Groups[1].Value) {
      'She' { 'He' }
      'SHE' { 'HE' }
      default { 'he' }
    }
  })
  $totalShe += ([regex]::Matches($orig, '\b(She|she|SHE)\b')).Count

  # --- her -> him / his ----------------------------------------------------
  # Two passes: possessive first is wrong, so do it in one pass with the
  # evaluator deciding.
  $src = [regex]::Replace($src, '\b([A-Za-z]+)(\s+)(Her|her|HER)\b', {
    param($m)
    $prev = $m.Groups[1].Value.ToLowerInvariant()
    $pron = $m.Groups[3].Value

    $repl = if ($Objective -contains $prev) {
      if     ($pron -eq 'HER') { 'HIM' }
      elseif ($pron -eq 'Her') { 'Him' }
      else                     { 'him' }
    } else {
      if     ($pron -eq 'HER') { 'HIS' }
      elseif ($pron -eq 'Her') { 'His' }
      else                     { 'his' }
    }

    return $m.Groups[1].Value + $m.Groups[2].Value + $repl
  })

  # Bare "her" with no preceding word - a line that starts with it, or one
  # after punctuation. Posessive by default, which is right far more often:
  # "her feet" at the start of a line is "his feet", and "Her" after a full
  # stop is nearly always possessive too.
  $src = [regex]::Replace($src, '(?<![A-Za-z])(Her|her|HER)\b', {
    param($m)
    switch ($m.Groups[1].Value) {
      'HER' { 'HIS' }
      'Her' { 'His' }
      default { 'his' }
    }
  })

  $totalHer += ([regex]::Matches($orig, '\b(Her|her|HER)\b')).Count

  if ($src -ne $orig) {
    [System.IO.File]::WriteAllText($f.FullName, $src)
    $changedFiles++

    # Flag every changed line that now names a male pronoun more than once.
    # Those are the lines where "him" might have meant Mono or the crawler
    # and now reads as Ari twice.
    $old = $orig -split "`r?`n"
    $new = $src -split "`r?`n"
    for ($i = 0; $i -lt $new.Length; $i++) {
      if ($old[$i] -eq $new[$i]) { continue }
      $n = ([regex]::Matches($new[$i], '\b(he|him|his|He|Him|His|HE|HIM|HIS)\b')).Count
      if ($n -ge 2) {
        $review.Add(('{0}:{1}: {2}' -f $f.Name, ($i+1), $new[$i].Trim()))
      }
    }
  }
}

"she -> he : $totalShe occurrences"
"her -> him/his : $totalHer occurrences"
"files changed : $changedFiles of $($files.Count)"
""
"=== lines that now name a male pronoun twice or more: $($review.Count) ==="
"these need reading by hand; 'him' may have meant Mono or the crawler"
$review | ForEach-Object { "  $_" }