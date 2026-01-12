# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow Semantic Versioning.

## Unreleased

### Changed
- Upgraded `MinVer` to `7.0.0`.
- Upgraded `bUnit` to `2.5.3`.
- Refactored the `Marquee` component into smaller partial files and consolidated common `using` directives into `GlobalUsings.cs`.
- Improved drag handling performance by switching to Pointer Events and avoiding per-move animation discovery.
- Tightened marquee CSS selectors and replaced a brittle inline-style selector with explicit CSS classes.
- Made the demo publish base-href rewrite task cross-platform.

