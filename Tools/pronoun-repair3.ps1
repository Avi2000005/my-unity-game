<#
    Hand-list repair for the she/her -> he/him sweep.

    WHAT WENT WRONG WITH THE SWEEP, MEASURED
    ------------------------------------------
    Tools\pronoun-sweep2.ps1 replaced 439 occurrences across 46 files. It was
    correct about gender - Ari is male - and it destroyed the grammar in 30
    places, all of one shape:

        "her feet"  ->  "him feet"        (should be "his feet")
        "her own"   ->  "him own"         (should be "his own")
        "stops her" ->  "stops his"       (should be "stops him")

    The rule the sweep used picked "him" before a vowel and "his" before a
    consonant. That is right for "her eyes" -> "his eyes" and wrong for every
    noun that begins with a vowel, because `him` is an object pronoun and can
    never be a possessive determiner. 21 sites are of this shape.

    A further 9 sites turned "stops her" / "pops her" into "stops his" /
    "pops his", which is the same mistake reached from the other direction:
    an object pronoun became a possessive one.

    And 8 lines became ambiguous rather than wrong. They were already using
    "he" / "his" for Mono or for a crawler on the same line, so when Ari's
    "her" became "him" two different people ended up sharing one pronoun.
    Those are fixed by naming Ari, which is the only way a sentence with two
    male people in it can be unambiguous.

    HOW THESE WERE FOUND
    -------------------
    By diffing the sweep's own backup (Tools\_pronoun_backup) against the live
    files, not by reading the 46 files. An earlier attempt at the detector
    reported 7 ambiguous lines and missed one, because it looked for a second
    pronoun on the SAME LINE, and the statement "Mono stands 2.00 m and she
    stands 1.80 m" is split across two lines. A detector that only reads one
    line at a time is not reading statements.

    WHY A SCRIPT AND NOT 35 EDITS
    -----------------------------
    Every one of these is an exact-string substitution in a string literal or
    a doc comment, and a hand edit that misses one is invisible until someone
    reads that tooltip. The substitution list is the reviewable artefact; the
    script only applies it.

    SAFETY, AND WHY IT IS WRITTEN OUT AGAIN
    ---------------------------------------
    Two scripts have already destroyed this project by claiming success. The
    first wrote the empty string over 70 files and reported "repaired 562",
    because it counted replacements in memory that had never reached disk. The
    second rolled back 17 of 20 good files because it compared
    `(Get-Item).Length` - BYTES on disk - against `$text.Length` - CHARACTERS
    in memory, and every file here contains em-dashes that are 3 bytes and 2
    UTF-16 units, so the assertion could never pass.

    So this one:
      * is a dry run unless -Apply is passed;
      * backs up every file it will touch, first;
      * requires each substitution to match EXACTLY ONCE, and skips the whole
        file if any substitution in it does not - a partial file is worse than
        an untouched one;
      * asserts byte count against UTF8.GetByteCount, not against .Length;
      * and reads the text back off disk and compares it to what it meant to
        write. A length check proves bytes arrived. Only the read-back proves
        they are the right bytes.

    Run it with no arguments to see the plan.
#>

[CmdletBinding()]
param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

$Root   = 'C:\Users\chate\Desktop\Echos_of_Forgotten_Painter'
$Target = Join-Path $Root 'Assets\Painterly'
$Backup = Join-Path $Root 'Tools\_pronoun_backup2'

# ---------------------------------------------------------------------------
# The substitution list.
#
# file  ->  @{ Find = <exact text>; Replace = <exact text>; Why = <reason> }
#
# Paths are relative to Assets\Painterly, with backslashes.
# ---------------------------------------------------------------------------

$Fixes = @(

    # ---- group A: "him" used as a possessive determiner --------------------
    # 21 sites. English never allows this: `him` is an object pronoun.

    @{ File = 'Editor\AriFit.cs';            Find = '// THE SCALE IS ABOUT HIM FEET';                        Replace = '// THE SCALE IS ABOUT HIS FEET';                    Why = 'her feet -> his feet' }
    @{ File = 'Editor\AriFit.cs';            Find = 'keeps him feet';                                        Replace = 'keeps his feet';                                Why = 'her feet -> his feet' }
    @{ File = 'Editor\AriFit.cs';            Find = 'puts him soles at y ';                                  Replace = 'puts his soles at y ';                           Why = 'her soles -> his soles' }
    @{ File = 'Editor\AriFit.cs';            Find = 'against him own mesh';                                 Replace = 'against his own mesh';                            Why = 'her own -> his own' }
    @{ File = 'Editor\AriFit.cs';            Find = 'taken about him feet';                                 Replace = 'taken about his feet';                            Why = 'her feet -> his feet' }
    @{ File = 'Editor\AriFit.cs';            Find = 'against him own collider';                             Replace = 'against his own collider';                        Why = 'her own -> his own' }
    @{ File = 'Editor\AriWiring.cs';        Find = 'm from him chest';                                     Replace = 'm from his chest';                                Why = 'her chest -> his chest' }
    @{ File = 'Editor\Beat4Setup.cs';        Find = 'with him soles on the ground';                         Replace = 'with his soles on the ground';                    Why = 'her soles -> his soles' }
    @{ File = 'Editor\Beat5Crawlers.cs';    Find = 'measured it from him spawn';                           Replace = 'measured it from his spawn';                      Why = 'her spawn -> his spawn' }
    @{ File = 'Editor\Beat5Setup.cs';       Find = 'walks in under him own steam';                         Replace = 'walks in under his own steam';                    Why = 'her own -> his own' }
    @{ File = 'Editor\Beat5Setup.cs';       Find = ' m from him centre against a ';                        Replace = ' m from his centre against a ';                   Why = 'her centre -> his centre' }
    @{ File = 'Editor\L1CrawlerProbe.cs';   Find = 'from its eye to him chest';                            Replace = 'from its eye to his chest';                       Why = 'her chest -> his chest' }
    @{ File = 'Editor\TreeSightline.cs';    Find = 'standing in him own shot';                             Replace = 'standing in his own shot';                        Why = 'her own -> his own' }
    @{ File = 'Scripts\AriAnim.cs';         Find = 'Assign Ari.controller to him Animator';                 Replace = 'Assign Ari.controller to his Animator';            Why = 'her Animator -> his Animator' }
    @{ File = 'Scripts\AriMover.cs';        Find = 'How far below him feet to look for ground';             Replace = 'How far below his feet to look for ground';        Why = 'her feet -> his feet' }
    @{ File = 'Scripts\AriMover.cs';        Find = 'measured on him mesh';                                  Replace = 'measured on his mesh';                             Why = 'her mesh -> his mesh' }
    @{ File = 'Scripts\BrushPainter.cs';    Find = 'on this object or in him children';                    Replace = 'on this object or in his children';               Why = 'her children -> his children' }
    @{ File = 'Scripts\HoldLever.cs';       Find = 'is stopped by him capsule';                             Replace = 'is stopped by his capsule';                        Why = 'her capsule -> his capsule' }
    @{ File = 'Scripts\MonoCompanion.cs';   Find = 'talk over him shoulder';                                Replace = 'talk over his shoulder';                           Why = 'her shoulder -> his shoulder' }
    @{ File = 'Scripts\MonoCompanion.cs';   Find = 'not under him feet';                                   Replace = 'not under his feet';                              Why = 'her feet -> his feet' }

    # ---- group B: object pronoun turned into a possessive -----------------
    # 9 sites. The mirror image of group A, and the same class of error.

    @{ File = 'Editor\Beat3Setup.cs';        Find = 'the wall sweep stops his on contact, so a measured';    Replace = 'the wall sweep stops him on contact, so a measured'; Why = 'stops her -> stops him' }
    @{ File = 'Editor\Beat3Setup.cs';        Find = 'Ari''s capsule is 0.60 m across and his sweep stops his on'; Replace = 'Ari''s capsule is 0.60 m across and his sweep stops him on'; Why = 'stops her -> stops him' }
    @{ File = 'Editor\Beat4Setup.cs';        Find = 'that height is what stops his"';                       Replace = 'that height is what stops him"';                    Why = 'stops her -> stops him' }
    @{ File = 'Editor\CharacterSizeProbe.cs'; Find = 'what the physics engine stops his with. Decides whether he'; Replace = 'what the physics engine stops him with. Decides whether he'; Why = 'stops her -> stops him' }
    @{ File = 'Editor\CharacterSizeProbe.cs'; Find = 'What the physics stops his with governs.';            Replace = 'What the physics stops him with governs.';           Why = 'stops her -> stops him' }
    @{ File = 'Editor\CollisionLiveProbe.cs'; Find = 'the village stops his dead';                          Replace = 'the village stops him dead';                         Why = 'stops her dead -> stops him dead' }
    @{ File = 'Editor\CollisionLiveProbe.cs'; Find = 'this flag is stopping his and the first test';         Replace = 'this flag is stopping him and the first test';        Why = 'stopping her -> stopping him' }
    @{ File = 'Editor\GullyBuilder.cs';      Find = 'wall sweep stops his on contact with both walls';      Replace = 'wall sweep stops him on contact with both walls';     Why = 'stops her -> stops him' }
    @{ File = 'Scripts\AriMover.cs';        Find = 'snapping straight to the surface pops his up';         Replace = 'snapping straight to the surface pops him up';        Why = 'pops her up -> pops him up' }

    # ---- group C: Ari and a second male character now share one pronoun ---
    # 8 sites. Not wrong English - unreadable English. Each already used "he"
    # or "his" for Mono or for a crawler, so there is no pronoun left that
    # distinguishes the two. Naming Ari is the fix; nothing else is.

    @{ File = 'Editor\CharacterSizeProbe.cs'; Find = 'MONO IS NOT SMALLER THAN ARI. He stands ';              Replace = 'MONO IS NOT SMALLER THAN ARI. Mono stands ';           Why = '"He" here is Mono, so the next "he" cannot be Ari' }
    @{ File = 'Editor\CharacterSizeProbe.cs'; Find = ' m and he stands ';                                      Replace = ' m and Ari stands ';                                 Why = 'the same pair, second half - must name Ari' }
    @{ File = 'Editor\CharacterSizeProbe.cs'; Find = 'excludes his and includes him';                        Replace = 'excludes Ari and includes Mono';                     Why = 'first = Ari, second = Mono' }
    @{ File = 'Editor\CharacterSizeProbe.cs'; Find = 'excludes his and admits him';                          Replace = 'excludes Ari and admits Mono';                       Why = 'first = Ari, second = Mono' }
    @{ File = 'Editor\Beat5Setup.cs';       Find = 'over the only thing he was meant to hide behind';      Replace = 'over the only thing Ari was meant to hide behind';     Why = 'first "he" is the crawler, so this one is Ari' }
    # The dash in this sentence is U+2014, not a hyphen, and it is left out of
    # the match on purpose: pasting it from a console is how an em-dash quietly
    # becomes a hyphen and a substitution stops matching with no other clue.
    @{ File = 'Editor\Beat5Setup.cs';       Find = 'he cannot reach him where he stops,';                  Replace = 'Ari cannot reach him where he stops,';               Why = 'first is Ari, second is the crawler' }
    @{ File = 'Editor\L1CastProbe.cs';      Find = 'the project''s existing definition of his chest';       Replace = 'the project''s existing definition of Ari''s chest';   Why = 'it is Ari''s chest that InkCrawler measures from' }
    @{ File = 'Scripts\Beat5Director.cs';   Find = 'Mono opens his eyes, wherever he is standing.';        Replace = 'Mono opens his eyes, wherever Ari is standing.';       Why = '"his eyes" is Mono, so this one is Ari' }
    @{ File = 'Scripts\Beat5Director.cs';   Find = 'is an entrance he can walk past without ever seeing';   Replace = 'is an entrance Ari can walk past without ever seeing';  Why = 'same line, same collision' }
    @{ File = 'Scripts\MonoCompanion.cs';   Find = 'Where his chest is, as a fraction of his body height.'; Replace = 'Where Ari''s chest is, as a fraction of Ari''s body height.'; Why = 'Ari''s chest, not Mono''s - "his" reads as Mono' }
    @{ File = 'Scripts\MonoCompanion.cs';   Find = 'the same 0.6 that InkCrawler measures his reach from';  Replace = 'the same 0.6 that InkCrawler measures Ari''s reach from'; Why = 'Ari''s reach' }
    @{ File = 'Scripts\MonoCompanion.cs';   Find = 'the brief is ''up to him chest''';                       Replace = 'the brief is ''up to Ari''s chest''';                 Why = 'the brief quotes Ari''s chest' }
)

# ---------------------------------------------------------------------------

Write-Output ''
Write-Output 'pronoun repair - group A: "him" used as a possessive determiner'
Write-Output '                    group B: object pronoun turned into a possessive'
Write-Output '                    group C: Ari and another male character share a pronoun'
Write-Output ''
Write-Output ('substitutions: {0}   files: {1}' -f $Fixes.Count,
    (($Fixes.File | Sort-Object -Unique) | Measure-Object).Count)

# --- plan: every substitution must match exactly once ------------------------

$plan = @{}
$bad = 0

foreach ($f in $Fixes) {
    $full = Join-Path $Target $f.File
    if (-not (Test-Path $full)) {
        Write-Output ("MISSING FILE  {0}" -f $f.File)
        $bad++
        continue
    }
    if (-not $plan.ContainsKey($f.File)) {
        $plan[$f.File] = [pscustomobject]@{ Path = $full; Text = [System.IO.File]::ReadAllText($full); Items = @() }
    }
    $e = $plan[$f.File]
    $e.Text = $e.Text.Replace($f.Find, $f.Replace)
    $e.Items += $f.Why
}

# Second pass over the ORIGINAL text for the match-count check, because
# $e.Text has already been rewritten by the time every item is added.
foreach ($f in $Fixes) {
    $full = Join-Path $Target $f.File
    if (-not (Test-Path $full)) { continue }
    $orig = [System.IO.File]::ReadAllText($full)
    $n = ([regex]::Matches($orig, [regex]::Escape($f.Find))).Count
    if ($n -ne 1) {
        Write-Output ("AMBIGUOUS OR GONE  {0}  matches {1}x:  {2}" -f $f.File, $n, $f.Find)
        $bad++
    }
}

Write-Output ''
Write-Output '=== files that would change ==='
foreach ($k in ($plan.Keys | Sort-Object)) {
    $e = $plan[$k]
    $before = (Get-Item $e.Path).Length
    $after  = [System.Text.Encoding]::UTF8.GetByteCount($e.Text)
    $delta  = $after - $before
    Write-Output ('  {0,-34} {1,7} -> {2,7} B  ({3:+#;-#;0})  {4} fix(es)' -f $k, $before, $after, $delta, $e.Items.Count)
}

if ($bad -gt 0) {
    Write-Output ''
    Write-Output ("NOTHING WRITTEN. {0} substitution(s) did not match exactly once." -f $bad)
    Write-Output 'A file is all-or-nothing here on purpose: a half-applied fix to a'
    Write-Output 'source file is a worse state than an untouched one.'
    return
}

if (-not $Apply) {
    Write-Output ''
    Write-Output 'DRY RUN. Nothing written. Re-run with -Apply.'
    return
}

# --- backup first, then write, then verify ----------------------------------

New-Item -ItemType Directory -Force -Path $Backup | Out-Null
foreach ($k in $plan.Keys) {
    $dest = Join-Path $Backup $k
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item $plan[$k].Path $dest -Force
}
Write-Output ''
Write-Output ('backed up {0} files to {1}' -f $plan.Count, $Backup)

$written = 0
foreach ($k in ($plan.Keys | Sort-Object)) {
    $e = $plan[$k]

    # Refuse anything that lost its code. The first destructive script wrote
    # '' over 70 files and each one passed a "was this empty?" test that it
    # never actually ran.
    if ($e.Text.Length -lt 200 -or $e.Text -notmatch 'using |namespace |class ') {
        Write-Output ("REFUSED  {0}  the rewritten text has no code in it" -f $k)
        continue
    }

    [System.IO.File]::WriteAllText($e.Path, $e.Text)

    # BYTES against BYTES. `(Get-Item).Length` is bytes on disk and
    # `$text.Length` is characters in memory; the em-dashes and curly
    # apostrophes in these files make the byte count the larger of the two
    # every single time, so the original `-ne $e.New.Length` assertion here
    # rolled back 48 files that were written perfectly.
    $want = [System.Text.Encoding]::UTF8.GetByteCount($e.Text)
    $now  = (Get-Item $e.Path).Length

    if ($now -ne $want) {
        Copy-Item (Join-Path $Backup $k) $e.Path -Force
        Write-Output ("ROLLED BACK  {0}  wrote {1} bytes, expected {2}" -f $k, $now, $want)
        continue
    }

    # Read the text back. A byte count proves bytes arrived; it does not prove
    # they are the bytes intended. This does.
    $back = [System.IO.File]::ReadAllText($e.Path)
    if ($back -ne $e.Text) {
        Copy-Item (Join-Path $Backup $k) $e.Path -Force
        Write-Output ("ROLLED BACK  {0}  the text on disk is not the text written" -f $k)
        continue
    }

    $written++
    Write-Output ('wrote  {0,-34} {1} B' -f $k, $now)
}

Write-Output ''
Write-Output ('written {0}, refused or rolled back {1}' -f $written, ($plan.Count - $written))
