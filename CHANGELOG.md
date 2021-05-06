# Changelog
All notable changes to PullStream project will be documented in this file.

Changelog for PullStream.Csv is [here](src/PullStream.Csv/CHANGELOG.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- NotDisposing [#41](https://github.com/pepelev/PullStream/issues/41)

## [1.2.0] - 2021-05-03
### Added
- OutputChunk that knows how to write itself into context #27
- Extension methods for easy work with OutputChunk
- Bytes class that iherits OutputChunk
- Over method for Builder that wraps original stream #28
- Pure attributes for pure methods #34

### Changed
- Concatenation method now uses new Bytes class

## [1.1.0] - 2021-04-28
### Added
- Concatenation method that allows to concatenate streams #13
- Cancellation token support in SequenceStream.AsyncBuilder #21

## [1.0.1] - 2021-04-28
### Fixes
- Support cancellation for methods working with AsyncEnumerable #14

## [1.0.0] - 2021-04-23
### Changed
- Make logo soft


## [0.1.1] - 2021-04-23
### Added
- Null checks in public API
- README.md #5

### Fixed
- Bug in CircularBuffer.Position

## [0.1.0] - 2021-04-22
### Added
- Basic functionality #1 #3 #4 #6 #9 #10 #11 #12
- Logo

[Unreleased]: https://github.com/pepelev/pullstream/compare/v1.2.0...HEAD
[1.2.0]: https://github.com/pepelev/pullstream/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/pepelev/pullstream/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/pepelev/pullstream/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/pepelev/pullstream/compare/v0.1.1...v1.0.0
[0.1.1]: https://github.com/pepelev/pullstream/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/pepelev/pullstream/releases/tag/v0.1.0