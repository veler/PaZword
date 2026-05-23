# WCAG 2.1 contrast report — PaZword brand ramp

> Computed by Nova during T-0.4 (sprint 0).
> Method: WCAG 2.1 relative-luminance formula. Each sRGB channel `c'` is gamma-decoded:
>   `c = c'/255; cl = c ≤ 0.03928 ? c/12.92 : ((c+0.055)/1.055)^2.4`.
> Luminance `L = 0.2126·R + 0.7152·G + 0.0722·B`. Contrast `(L1+0.05)/(L2+0.05)` with `L1 ≥ L2`.
> WCAG AA: ≥ 4.5 for normal text (< 18 pt), ≥ 3.0 for large text and UI components.

## Ramp under test (PROJECT_BRIEF §6)

| Token              | Light    | Dark     |
|--------------------|----------|----------|
| `BrandPrimary`     | `#C42B1C` | `#FF4D3D` |
| `BrandPrimaryHover`| `#B2261A` | `#FF6A5C` |
| `BrandPrimaryPressed`| `#9E2218` | `#E04335` |
| `BrandSubtle`      | `#FDECEA` | `#3A1411` |
| `BrandOnPrimary`   | `#FFFFFF` | `#FFFFFF` |

Reference Fluent surfaces (WinUI 2024 system tokens):

| Surface                                    | Light    | Dark     |
|--------------------------------------------|----------|----------|
| `LayerOnMicaBaseAltFillColorDefault` (~card) | `#FDFDFD` | `#2D2D2D` |
| `TextFillColorPrimary`                     | `#1A1A1A` | `#FFFFFF` |

## Computed relative luminances

| Color    | sRGB R,G,B | Relative L |
|----------|------------|------------|
| `#C42B1C` (Light Primary) | 196,43,28 | **0.1355** |
| `#B2261A` (Light Hover)   | 178,38,26 | **0.1093** |
| `#9E2218` (Light Pressed) | 158,34,24 | **0.0848** |
| `#FDECEA` (Light Subtle)  | 253,236,234 | **0.8681** |
| `#FF4D3D` (Dark Primary)  | 255,77,61 | **0.2690** |
| `#FF6A5C` (Dark Hover)    | 255,106,92 | **0.3234** |
| `#E04335` (Dark Pressed)  | 224,67,53 | **0.2012** |
| `#3A1411` (Dark Subtle)   | 58,20,17 | **0.0144** |
| `#FFFFFF` (white)         | 255,255,255 | 1.0000 |
| `#1A1A1A` (light text)    | 26,26,26 | 0.0103 |
| `#FDFDFD` (light card)    | 253,253,253 | 0.9823 |
| `#2D2D2D` (dark card)     | 45,45,45 | 0.0262 |

## Contrast verdicts

### Light theme

| Pair                                            | Ratio   | Min   | Verdict |
|-------------------------------------------------|---------|-------|---------|
| `BrandOnPrimary` (`#FFF`) on `BrandPrimary` (`#C42B1C`)         | **5.66** | 4.5 | ✅ AA normal text |
| `BrandOnPrimary` on `BrandPrimaryHover` (`#B2261A`)            | **6.59** | 4.5 | ✅ AA normal text |
| `BrandOnPrimary` on `BrandPrimaryPressed` (`#9E2218`)          | **7.79** | 4.5 | ✅ AAA |
| `BrandPrimary` on light card (`#FDFDFD`)                       | **5.56** | 4.5 | ✅ AA normal text |
| `BrandPrimary` on `BrandSubtle` (`#FDECEA`)                    | **4.95** | 4.5 | ✅ AA normal text |
| `TextFillColorPrimary` (`#1A1A1A`) on `BrandSubtle`            | **15.23** | 7.0 | ✅ AAA |

### Dark theme

| Pair                                            | Ratio   | Min   | Verdict |
|-------------------------------------------------|---------|-------|---------|
| `BrandOnPrimary` (`#FFF`) on `BrandPrimary` (`#FF4D3D`)         | **3.29** | 4.5 | ❌ AA normal; ✅ AA large/UI |
| `BrandOnPrimary` on `BrandPrimaryPressed` (`#E04335`)          | **4.18** | 4.5 | ❌ AA normal; ✅ AA large/UI |
| `BrandPrimary` (`#FF4D3D`) on dark card (`#2D2D2D`)            | **4.19** | 4.5 | ❌ AA normal; ✅ AA large/UI |
| `BrandPrimary` on `BrandSubtle` (`#3A1411`)                    | **4.95** | 4.5 | ✅ AA normal text |
| White text on `BrandSubtle`                                    | **16.30** | 7.0 | ✅ AAA |

## Findings

1. **Light theme passes AA normal text across the full ramp.** Every relevant pair clears 4.5:1.
2. **Dark theme `BrandOnPrimary` on `BrandPrimary` fails AA normal text (3.29 < 4.5)** but passes AA large/UI (≥ 3.0). Acceptable for icon-only accent buttons and 1 px+ accent bars; **do not render body text on `BrandPrimary` in dark mode** — pair with `BrandSubtle` (4.95) or system surface colors instead.
3. **Dark `BrandPrimary` on the standard dark Fluent card (`#2D2D2D`) is only 4.19** — borderline for normal text. Headings and large UI labels are fine; body copy should fall back to `TextFillColorPrimary` and reserve `BrandPrimary` for emphasis/links/icons.
4. High-contrast: `Brand.*` tokens are intentionally absent from the `HighContrast` theme dictionary so system high-contrast accents win.

## Open follow-ups (forwarded to Milo for sprint 2)

- Consider shifting dark-mode `BrandPrimary` from `#FF4D3D` toward `#FF6354` / `#E84032` to push the white-on-accent contrast above 4.5. Trade-off: identity drifts slightly more saturated/orange.
- Define a `BrandOnSubtleBrush` token (= `TextFillColorPrimary` per theme) so styles bind to it explicitly instead of `BrandPrimary` on `BrandSubtle`.
- Q for Milo: should we introduce a `BrandPrimaryOnDark` (deeper red, e.g. `#D43A2D`) used specifically as a text/icon color over neutral dark surfaces, while keeping `BrandPrimary` for fills? Would let us hit AA without losing the bright fill.

## Reproducibility

To recompute, evaluate this snippet (PowerShell with .NET):

```powershell
function L([int]$r,[int]$g,[int]$b) {
  $f = { param($c) $x = $c/255.0; if ($x -le 0.03928) { $x/12.92 } else { [math]::Pow(($x+0.055)/1.055, 2.4) } }
  0.2126*(&$f $r) + 0.7152*(&$f $g) + 0.0722*(&$f $b)
}
function C($l1,$l2) { ($([math]::Max($l1,$l2))+0.05) / ($([math]::Min($l1,$l2))+0.05) }
# example: C (L 255 255 255) (L 196 43 28)
```
