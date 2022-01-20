# Changelog

!! MAKE SURE TO UPDATE TO THE LATEST NUGET PACKAGES !! 

## [0.11.3] - 2022-01-20

### Added
- Build log is now displayed during "Build & Deploy"
- InputDateTime
- InputNumber
- InputSelect
- InputText
- Lock
- NumericSensor

### Changed
- All sensors are now available in the `Sensors` class.
- Using pre compiled docker images should make updates faster
- Moved to .net 6 release version

### Fixed
- Multiple instances of the automation hosts run simultaneously

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
