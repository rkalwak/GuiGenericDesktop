# Copilot Instructions

## Line Endings
- **All files in this repository must use LF (`\n`) line endings** — never CRLF.
- This is enforced by `.gitattributes` (`eol=lf`). Always write files with LF.
- When writing files with PowerShell, **never** use `[System.Text.Encoding]::UTF8` — it adds a UTF-8 BOM. Always use `[System.Text.UTF8Encoding]::new($false)`.
- After writing a file, verify with a binary byte check: bare `0x0A` = LF ✅, `0x0D 0x0A` = CRLF ❌.

## Encoding & Diacritics
- All source files are **UTF-8 without BOM**.
- Polish diacritic characters (ą ć ę ł ń ó ś ź ż and their uppercase equivalents) must be preserved exactly — never replace them with ASCII approximations or escape sequences.
- When writing or replacing file content through PowerShell always use `[System.Text.UTF8Encoding]::new($false)` so diacritics round-trip correctly.

## Languages
- All user-facing strings, messages, and UI text must be written in either **Polish (pl)** or **English (en)**.
- Do not introduce strings in any other language.
- Comments in code may be in English only.
- For UI text in this repo, use the existing LocalizationManager resource dictionaries instead of hardcoded strings. Keep all user-facing labels in the resource files and reference them via `LocalizationManager.Get/GetFormat`.

## Testing Guidelines
- Never make tests dependent on environment variables. Put test data (like COM port, chip name) directly in the test class as constants.
- Keep test arrange and assertion values explicit and static: write literal expected values in the test itself instead of generating them dynamically from helper methods, LINQ, or runtime loops.
- Do not use LINQ, reflection-based generation, or helper methods to build expected test data or assertion values in unit tests.
- **Never run integration tests unless explicitly requested.**

## Test Style Expectations
- Prefer straightforward, readable test setup with fixed literal values and direct assertions.
- When verifying flag state, assert both the commented and uncommented forms explicitly in the same test: allowed/commented flags must be uncommented and unlisted/uncommented flags must be commented.
- Use static string literals for expected flag lines and parameter values; do not derive them from model data or collection transformations in the test body.

## Line Ending Verification (PowerShell)
```powershell
$b = [System.IO.File]::ReadAllBytes($path)
$crlf = 0; $lf = 0
for ($i = 0; $i -lt $b.Length - 1; $i++) {
    if ($b[$i] -eq 13 -and $b[$i+1] -eq 10) { $crlf++ }
    elseif ($b[$i] -eq 10 -and ($i -eq 0 -or $b[$i-1] -ne 13)) { $lf++ }
}
# Expected: crlf=0, lf>0, first byte != 239 (no BOM)
```


## Line Ending Verification (PowerShell)
```powershell
$b = [System.IO.File]::ReadAllBytes($path)
$crlf = 0; $lf = 0
for ($i = 0; $i -lt $b.Length - 1; $i++) {
    if ($b[$i] -eq 13 -and $b[$i+1] -eq 10) { $crlf++ }
    elseif ($b[$i] -eq 10 -and ($i -eq 0 -or $b[$i-1] -ne 13)) { $lf++ }
}
# Expected: crlf=0, lf>0, first byte != 239 (no BOM)