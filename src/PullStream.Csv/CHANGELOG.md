# Changelog
All notable changes to PullStream.Csv project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
Nothing to write yet

## [2.0.0] - 2021-06-15
### Changed
- Now NuGet package targets only netstandard2.0 (there used to be netstandard2.0 and netstandard2.1) [#48](https://github.com/pepelev/PullStream/issues/48)
- Use ChunkOutput from PullStream package instead of custom CsvRow [#31](https://github.com/pepelev/PullStream/issues/31)
- Now NuGet package references PullStream package of minimal verision 1.2.1 (there used to be 1.2.0)

## [1.0.1] - 2021-06-14
### Fixed
- Validate all arguments of public methods [#30](https://github.com/pepelev/PullStream/issues/30)

## [1.0.0] - 2021-04-30
### Changed
- Package version

## [0.1.0] - 2021-04-30
### Added
- Csv stream [#8](https://github.com/pepelev/PullStream/issues/8)

[Unreleased]: https://github.com/pepelev/pullstream/compare/csv-v2.0.0...pullstream-csv
[2.0.0]: https://github.com/pepelev/pullstream/compare/csv-v1.0.1...csv-v2.0.0
[1.0.1]: https://github.com/pepelev/pullstream/compare/csv-v1.0.0...csv-v1.0.1
[1.0.0]: https://github.com/pepelev/pullstream/compare/csv-v0.1.0...csv-v1.0.0
[0.1.0]: https://github.com/pepelev/pullstream/releases/tag/pullstream-json-v0.1.0