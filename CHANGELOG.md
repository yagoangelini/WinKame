# Changelog

All notable changes to WinKame. Versions follow [semantic versioning](https://semver.org); downloads are on the
[Releases page](https://github.com/thalesmorandi/WinKame/releases). The release workflow copies each version's
section below into its GitHub release notes.

## [0.1.3] - 2026-11-09

### Added
- **WinKame as your email app.** Turn on *Settings › Integration › Offer WinKame as an email app*, then pick WinKame
  for email links in Windows *Default apps*. Clicking a `mailto:` link on a site or in a document then copies the
  address to the clipboard instead of opening Outlook or another mail program. Several recipients are copied
  separated by `; `; subject, body and cc are ignored. Nothing is sent anywhere.
- `scripts/test-fast.ps1` for contributors: skips the slow tests and prints only the summary.

## [0.1.2] - 2026-10-23

### Added
- **Accent color.** *Settings › General › Accent color*: the WinKame orange, seven other colors, the Windows accent
  color, or any hex code. The wheel, jobs card, shelf and progress bars change at once; text on light colors turns
  dark so it stays readable.
- Microsoft Store package (MSIX) build, ready for when the Store listing exists.

## [0.1.1] - 2026-10-22

### Fixed
- "Cannot change ObservableCollection during a CollectionChanged event" after the sixth action in a session.
- Add background (and other forms with a color field) failed with a null reference.
- Crop: the selection could not be drawn; it now starts inset and resizes from its edges and corners.
- ZIP: works with files open in other programs and folders with protected subfolders, renames duplicate names, and
  never leaves a broken ZIP after an error or cancel.

## [0.1.0] - 2026-10-22

First public release: the drag-and-tap wheel with format conversion, tools, file actions and custom actions for
images, audio and video, PDFs, documents, spreadsheets, subtitles and archives, all offline.

[0.1.3]: https://github.com/thalesmorandi/WinKame/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/thalesmorandi/WinKame/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/thalesmorandi/WinKame/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/thalesmorandi/WinKame/releases/tag/v0.1.0
