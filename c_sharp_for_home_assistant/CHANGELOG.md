# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- [Snapshot] attribute
- 'Event' class. Allows to listen for abitrary events.
- Script entity
- ReentryPolicy.QueueLatest - Behaves similar to what ReentryPolicy.Queue was before

### Changed
- ReentryPolicy.Queue - Will now queue runs without a limit

## [0.4.2] - 2021-07-02
### Fixed
- Fixed local application host not being started on supervisor startup.