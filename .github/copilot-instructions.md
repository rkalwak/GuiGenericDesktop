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