# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.6.0] - 2021-07-05
### Changed
- Overhauled UI

### Fixed
- Manual run start for ReentyPolicy.QueueLatest and ReentyPolicy.Queue
- Manual run stop for ReentyPolicy.QueueLatest and ReentyPolicy.Queue

## [0.5.0] - 2021-07-04
### Added
- Snapshot attribute
- Events.All class. Allows to listen for abitrary events.
- Events.Current class. Allows to access the current event.
- Script entity
- ReentryPolicy.QueueLatest - Behaves similar to what ReentryPolicy.Queue was before

### Changed
- ReentryPolicy.Queue - Will now queue runs without a limit
- Updated project template to latest nuget packages

## Fixed
- UI secrets editor layout

## [0.4.2] - 2021-07-02
### Fixed
- Fixed local application host not being started on supervisor startup.