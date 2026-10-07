---
name: Pwrschdlr
description: "A native Windows 11 WinUI 3 app that follows the Settings look: Mica backdrop, a centred column of Fluent resource cards, and one accent taken from the user's Windows colour."
colors:
  accent: "AccentFillColorDefaultBrush"
  accentText: "AccentTextFillColorPrimaryBrush"
  cardBackground: "CardBackgroundFillColorDefaultBrush"
  cardStroke: "CardStrokeColorDefaultBrush"
  secondaryText: "TextFillColorSecondaryBrush"
  disabledFill: "ControlStrongFillColorDisabledBrush"
  webApp: "#b5470b"
  webAppDark: "#ff9a5c"
typography:
  title: "TitleTextBlockStyle"
  subtitle: "SubtitleTextBlockStyle"
  bodyStrong: "BodyStrongTextBlockStyle"
  caption: "CaptionTextBlockStyle"
  countdown: "60"
rounded:
  control: "ControlCornerRadius"
  webButton: "2px"
spacing:
  cardPadding: "16,12"
  cardMinHeight: "68"
  cardColumnSpacing: "16"
  sectionHeaderMargin: "1,28,0,6"
  actionTileWidth: "124"
  actionTileHeight: "96"
  actionTileMargin: "0,0,8,8"
  glyphSize: "20"
  pageMaxWidth: "760"
  pagePadding: "32,20,32,32"
  ringSize: "320"
  ringThickness: "14"
  webMargin: "clamp(20px, 3.4vw, 48px)"
  webGutter: "24px"
components:
  card:
    backgroundColor: "{colors.cardBackground}"
    rounded: "{rounded.control}"
    padding: "{spacing.cardPadding}"
  settingsRow:
    backgroundColor: "{colors.cardBackground}"
    rounded: "{rounded.control}"
    padding: "{spacing.cardPadding}"
    height: "{spacing.cardMinHeight}"
  actionTile:
    backgroundColor: "{colors.cardBackground}"
    rounded: "{rounded.control}"
    width: "{spacing.actionTileWidth}"
    height: "{spacing.actionTileHeight}"
  countdownRing:
    backgroundColor: "{colors.disabledFill}"
    size: "{spacing.ringSize}"
  primaryButton:
    backgroundColor: "{colors.accent}"
    rounded: "{rounded.control}"
---

# Design System: Pwrschdlr

## Overview

**Creative North Star: "a Windows 11 settings page"**

Pwrschdlr is a WinUI 3 app that deliberately looks like the app it lives beside:
the window uses `MicaBackdrop`, a page is a centred column of cards topped by a
`TitleTextBlockStyle` heading, and each setting is a row with a glyph, a header
and a description, the way Windows 11 presents one. `App.xaml` defines the
hands-on styles (`CardStyle`, `ActionTileStyle`, `SettingsSectionHeaderStyle`
and the `SettingsCard` template); everything else comes from the stock Fluent
resources the app merges with `XamlControlsResources`. No hex colour is chosen
for the desktop UI — the accent and card colours are Fluent resources, so the
accent follows the user's Windows colour.

**The System-Accent Rule.** Interactive emphasis comes from
`AccentFillColorDefaultBrush` and `AccentTextFillColorPrimaryBrush`, so the app
takes the user's Windows accent colour instead of choosing its own.

**The Settings-Row Rule.** Every setting is the same card: a
`CardBackgroundFillColorDefaultBrush` fill, a `CardStrokeColorDefaultBrush`
outline, `ControlCornerRadius`, 16,12 padding and a 68 minimum height.

## Colors

The desktop app names Fluent resources rather than fixing hex values. They are
declared in `src/Pwrschdlr/App.xaml` and used by the views:

| Token | Resource | Used for |
| --- | --- | --- |
| `accent` | `AccentFillColorDefaultBrush` | The countdown arc; the primary button |
| `accentText` | `AccentTextFillColorPrimaryBrush` | Action-tile glyphs and the summary glyph |
| `cardBackground` | `CardBackgroundFillColorDefaultBrush` | Card, tile and settings-row fill |
| `cardStroke` | `CardStrokeColorDefaultBrush` | Card and settings-row outline |
| `secondaryText` | `TextFillColorSecondaryBrush` | Descriptions and the target time |
| `disabledFill` | `ControlStrongFillColorDisabledBrush` | The countdown ring's track |

No hex value is invented for any of these; the system colour is resolved by
Windows at run time. The website sets its own two variables in
`site/index.html`, `--app: #b5470b` and `--app-dark: #ff9a5c`, and takes the
rest of its paper, ink and hairline values from `site/site.css`.

**The No-Hex Rule.** A desktop surface is filled with a Fluent resource, never
a literal colour.

**The Status-Fill Rule.** The countdown ring is the accent arc over the
disabled-fill track, so the remaining time is read from the same resources as
the rest of the app.

## Typography

Text uses the stock WinUI text styles rather than a custom ramp:

| Token | Style | Used for |
| --- | --- | --- |
| `title` | `TitleTextBlockStyle` | The "Timer" and "Settings" page headings |
| `subtitle` | `SubtitleTextBlockStyle` | The running timer's summary line |
| `bodyStrong` | `BodyStrongTextBlockStyle` | Section headers (via `SettingsSectionHeaderStyle`) and the running action name |
| `caption` | `CaptionTextBlockStyle` | Descriptions (via `SecondaryCaptionStyle`) |

Fluent glyphs in a settings row are a `FontIcon` at 20. The one custom size is
the countdown itself, a `TextBlock` at 60 with `SemiBold` weight, which is the
largest thing on the page on purpose.

**The Ramp Rule.** Text takes its size and weight from the Fluent text styles,
not from a pixel value written at the call site.

**The One-Big-Number Rule.** The countdown is the only element with a
hand-picked size, and it is the number the whole page exists to show.

## Layout

- A page is a `StackPanel` with `MaxWidth` 760 and padding 32,20,32,32, held
  inside a `Grid` so the column stays centred in the `ScrollViewer`.
- A settings row pads 16,12 with a `ColumnSpacing` of 16 and a `MinHeight` of
  68; section headers sit at a margin of 1,28,0,6.
- The five action tiles are 124 × 96 with a margin of 0,0,8,8 between them.
- The countdown ring is a 320 × 320 `Grid` with a `StrokeThickness` of 14.
- The website uses the shared portfolio grid in `site/site.css`: a 12-column
  layout inside a `min(100% - 2 * var(--margin), 1344px)` wrap, with
  `--margin: clamp(20px, 3.4vw, 48px)` and `--gutter: 24px`.

**The Centred-Column Rule.** Pages are one 760-wide column; a page is never
split into two competing columns.

**The Tile Rule.** The five actions are shown as equal 124 × 96 tiles, so the
choice is a single row of peers rather than a list.

## Elevation & Depth

Depth comes from the material, not from shadows: `MicaBackdrop` gives the
window its backdrop, and a card is separated from the backdrop by its one-pixel
`CardStrokeColorDefaultBrush` outline. `InfoBar` strips (`UpdateBar`,
`StatusBar` and the running timer's `KeepsRunningBar`) sit above the page
content and carry their own Fluent surface.

**The Mica Rule.** The window uses `MicaBackdrop`; pages do not paint their own
opaque background over it.

**The Stroke-Not-Shadow Rule.** A card is defined by its border, not by a drop
shadow.

## Shapes

Every card, tile and settings row takes its corner radius from the Fluent
`ControlCornerRadius` resource (`App.xaml`), rather than a hard-coded number,
so the app matches the Windows controls around it. On the website, buttons have
a 2px radius and everything else is square (`site/site.css`).

**The Control-Radius Rule.** Corners come from `ControlCornerRadius`, so a
change to the Fluent radius follows automatically.

**The Two-Pixel Web Rule.** On the website only buttons are rounded, and only
by 2px.

## Components

| Component | Built from |
| --- | --- |
| `SettingsCard` | The `App.xaml` row style: glyph at 20, header, description in `secondaryText`, and the setting control in the third column, on a `cardBackground` fill with a `cardStroke` outline. |
| Action tile | `ActionTileStyle` on a `GridViewItem`: 124 × 96, `cardBackground` fill, `cardStroke` outline, `ControlCornerRadius`, centred content, and the list's accent border when selected. |
| Countdown ring | `TimerView.xaml`: a 320 × 320 grid with a 14-thick `Ellipse` on `disabledFill` and a `Path` arc on `accent`, with the countdown at 60 and the target time beneath it. |
| Primary button | `AccentButtonStyle`: for Start, Cancel timer and Update. |
| Secondary button | The default WinUI button: for postpone, presets, "What's new" and "Check now". |
| InfoBar strip | `UpdateBar` (with "What's new" and "Update"), `StatusBar` and `KeepsRunningBar`. |
| Navigation | A `NavigationView` with a Timer item and the built-in Settings item, under a `TitleBar`. |
| Website plate | `site/site.css`: the screenshot on a field mixed from `--app` at 16%, outlined with a hairline. |

**The Glyph Rule.** A settings row leads with a Fluent glyph at `glyphSize` 20,
so the list scans by icon as well as by text.

**The Disabled-Reason Rule.** An action the PC cannot perform stays visible and
greyed, with the reason in `secondaryText`, rather than disappearing.

## Do's and Don'ts

- **Do** take the accent and card colours from the Fluent resources; **don't**
  write a hex value into a desktop view.
- **Do** build a setting as a `SettingsCard` row; **don't** invent a new row
  shape for one setting.
- **Do** use the Fluent text styles; **don't** add pixel font sizes beyond the
  one countdown.
- **Do** keep corners on `ControlCornerRadius`; **don't** round a desktop
  control by hand — the 2px radius belongs to the website.
- **Do** let `MicaBackdrop` show through; **don't** paint an opaque page
  background over it.
