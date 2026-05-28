# Real Font Fixtures

Checked-in `.ttf`, `.otf`, or `.ttc` files in this directory are included in the smoke-test font compatibility pass.

Each font fixture must include a sidecar `{font-name}_manifest.json` with at least `name`, `family`, `styles`, `formats`, and `license`. The harness embeds each fixture in a generated PDF under `artifacts/font-fixtures`, checks manifest metadata, verifies simple WinAnsi output, and, for TrueType-outline fonts, verifies Type0 Identity-H/Identity-V output and subset embedding. OpenType/CFF fonts are expected to use an `OTTO` SFNT header and are emitted as `/FontFile3` streams with `/Subtype /OpenType`.
