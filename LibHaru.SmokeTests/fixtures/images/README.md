# Optional Image Fixtures

Drop external `.jpg`, `.jpeg`, or `.png` files in this directory to include them in the smoke-test image compatibility
pass automatically.

The checked-in fixture set includes a small RGB JPEG, RGB PNG, raw RGB sample, and 1-bit sample for CCITT Group 4
output. Keep any future fixtures small and deterministic so the smoke suite remains fast and repository-friendly.

Raw and 1-bit CCITT-oriented fixtures need metadata in `image-fixtures.tsv` because dimensions, stride, bit depth, and
color space are not self-describing. Each non-comment row is tab-separated:

```text
kind	path	width	height	colorSpace	bitsPerComponent	lineWidth	blackIs1	topIsFirst
```

Use `raw` for raw DeviceGray/DeviceRGB/DeviceCMYK samples and `ccitt` for 1-bit samples that should be emitted through
Group 4 CCITT compression. Optional fields can be `-`; `ccitt` defaults `lineWidth` to `(width + 7) / 8`, `blackIs1` to
`true`, and `topIsFirst` to `true`.
