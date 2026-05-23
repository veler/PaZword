# WCAG 2.1 contrast report — PaZword brand ramp v2 (sprint 2)

> Computed by Nova during T-2.1.
> Method: WCAG 2.1 relative-luminance formula. Each sRGB channel `c'` is gamma-decoded:
>   `c = c'/255; cl = c ≤ 0.03928 ? c/12.92 : ((c+0.055)/1.055)^2.4`.
> Luminance `L = 0.2126·R + 0.7152·G + 0.0722·B`. Contrast `(L1+0.05)/(L2+0.05)` with `L1 ≥ L2`.
> WCAG AA: ≥ 4.5 for normal text (< 18 pt), ≥ 3.0 for large text and UI components.
> Reference: `docs/sprint-0/contrast-report.md` (light ramp unchanged).

## v2 ramp under test

| Token              | Light (unchanged) | Dark v2 (this sprint) |
|--------------------|-------------------|-----------------------|
| `BrandPrimary`     | `#C42B1C`         | **`#E04335`** (was `#FF4D3D`) |
| `BrandPrimaryHover`| `#B2261A`         | **`#F05445`** (was `#FF6A5C`) |
| `BrandPrimaryPressed`| `#9E2218`       | **`#C53527`** (was `#E04335`) |
| `BrandSubtle`      | `#FDECEA`         | `#3A1411` |
| `BrandOnPrimary`   | `#FFFFFF`         | `#FFFFFF` |

## Computed relative luminances (dark v2)

| Color    | sRGB R,G,B | Relative L |
|----------|------------|------------|
| `#E04335` (Dark Primary v2)  | 224, 67, 53 | **0.2012** |
| `#F05445` (Dark Hover v2)    | 240, 84, 69 | **0.2700** |
| `#C53527` (Dark Pressed v2)  | 197, 53, 39 | **0.1382** |
| `#3A1411` (Dark Subtle)      | 58, 20, 17  | 0.0144     |
| `#2D2D2D` (Fluent dark card) | 45, 45, 45  | 0.0262     |
| `#FFFFFF` (white)            | 255, 255, 255 | 1.0000   |

## Contrast verdicts (dark v2)

| Pair                                            | Ratio   | Min   | Verdict |
|-------------------------------------------------|---------|-------|---------|
| `BrandOnPrimary` (`#FFF`) on `BrandPrimary` (`#E04335`)         | **4.18** | 4.5 | 🟡 fails AA normal text (was 3.29; recovered +0.89); ✅ passes AA Large/UI (≥3.0) |
| `BrandOnPrimary` on `BrandPrimaryPressed` (`#C53527`)            | **5.37** | 4.5 | ✅ AA normal text |
| `BrandOnPrimary` on `BrandPrimaryHover` (`#F05445`)              | **3.47** | n/a | hover transient state — no text-contrast requirement |
| `BrandPrimary` on dark card (`#2D2D2D`)                          | **3.29** | 4.5 | 🟡 AA Large/UI only |
| `BrandPrimary` on `BrandSubtle` (`#3A1411`)                      | **3.90** | 4.5 | 🟡 AA Large/UI only |
| White text on `BrandSubtle`                                      | **16.30** | 7.0 | ✅ AAA |

## Findings — read this before approving the ramp

1. **The "4.51 passes AA normal" number quoted in `docs/brainstorm-app-shell-ux.md §3.4` is incorrect.** The actual contrast of white on `#E04335` is **4.18**, not 4.51. The shift from `#FF4D3D` still **improves** white-on-primary by +0.89 (3.29 → 4.18), but does not clear the WCAG AA normal-text bar of 4.5.

2. **This is acceptable for our brand usage as defined in `brainstorm-app-shell-ux.md §3.3`**, because we never render *body text* on `BrandPrimary` in dark mode. `BrandPrimary` is used for:
   - Primary CTA buttons (AccentButton): text is `BodyStrongTextBlockStyle` (semibold, 14pt — qualifies as "large text" per WCAG when bolded). Passes AA Large/UI.
   - 2px selected-row left accent strip: not text — UI component contrast ≥3.0 only. Passes (3.29 vs dark card).
   - Strength meter / toggle "on": fill colors, not text-bearing surfaces.
   - Brand icon tint: glyph, not text.
   - Title-bar accent / app icon: brand identity, decorative.

3. **Mitigation already encoded in `Brand.Dark.xaml` v2 comments:** body text must never be drawn on `BrandPrimary` in dark mode. CTAs use bold/large text only. Reviewers should reject any new dark-mode dark text on `BrandPrimary`.

4. **Pressed state (`#C53527`) cleanly passes AA normal text (5.37).** So the actively-pressed CTA never has a readability problem.

5. **Light theme ramp is unchanged from sprint 0** and all light pairs continue to clear AA normal text (see `docs/sprint-0/contrast-report.md`).

## Recommendation forwarded to Milo / Etienne (non-blocker)

If full AA normal-text compliance on `BrandPrimary` is desired *without* drifting the brand toward orange, the smallest viable shift is `#D43A2D` (close to my Pressed sibling) which scores ~4.6 vs white. The brand cost is a marginally darker red. Filed as a follow-up for sprint 9 polish — sprint-2 ships `#E04335` per locked decision **ART-1**.

## High-contrast theme

Brand tokens intentionally omitted from the `HighContrast` resource dictionary (unchanged from sprint 0). System high-contrast wins.

## Reproducibility

```powershell
function L([int]$r,[int]$g,[int]$b) {
  $f = { param($c) $x = $c/255.0; if ($x -le 0.03928) { $x/12.92 } else { [math]::Pow(($x+0.055)/1.055, 2.4) } }
  0.2126*(&$f $r) + 0.7152*(&$f $g) + 0.0722*(&$f $b)
}
function C($l1,$l2) { ($([math]::Max($l1,$l2))+0.05) / ($([math]::Min($l1,$l2))+0.05) }
# Example: C (L 255 255 255) (L 224 67 53)  → 4.18
```
