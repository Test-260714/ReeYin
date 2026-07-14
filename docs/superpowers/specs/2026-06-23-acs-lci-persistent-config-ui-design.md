# ACS LCI Persistent Config UI Design

Date: 2026-06-23

## Goal
Make the ACS control-card configuration page persist LCI test parameters and reorganize the LCI page so straight fixed-distance pulse and segmented circle tests are visually separated.

## Persistence
Existing saved ACS settings are stored under `AcsControlCardOptions`, while current LCI test inputs are ViewModel-only fields. Add two serializable option models:

- `AcsLciFixedDistancePulseConfig`: stores buffer number, axes, pulse width, interval, pulse start/end distances, ConfigOut routing, timeout, and point text.
- `AcsLciSegmentCircleConfig`: stores buffer number, axes, velocity, XSEG start point, circle center, radius, gate active state, and timeout.

`AcsControlCardOptions` owns these objects so normal control-card configuration serialization can save them with the ACS card.

## ViewModel
Keep the current public LCI property names for compatibility with existing commands and tests, but implement them as wrappers over `Options.LciFixedDistancePulse` and `Options.LciSegmentCircle`. Setters raise their existing property names and update the persistent option object.

## Page Layout
Within the existing `LCI脉冲` tab, replace the single long form with an inner TabControl:

- `直线脉冲`: fixed-distance pulse parameters and execution button.
- `分段画圆`: circle parameters and execution button.

The right side remains shared status/script output. This avoids long stacked controls and prevents parameter groups from being hidden or confused.

## Testing
Add console tests for option-model defaults, ViewModel wrapper persistence, and XAML bindings/layout separation. Keep the existing ACSPL+ script tests unchanged.
