# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.0] - 2020-06-12
### Added
- Another Maintenance endpoint for requeueing demos
- ErrorHandlingMiddleware
### Changed
- Routes for maintenace controller

## [2.1.0] - 2020-06-08
### Added
- Maintenance endpoint for requeueing demos

## [2.0.0] - 2020-06-03
### Added 
- Add downloadurl endpoint v1/public/match/{matchId}/download-url
- Producer for MatchData insertion is now a common producer instead of fanout producer

### Changed
- Demo Retry Logic

## [1.1.1] - 2020-04-22
### Added
- Demo removal controller 
- Logging to browser extension

## [1.1.0] - 2020-04-10
### Added
- Maintenance controller with Http endpoint "restart-unfinished-demos"

## [1.0.3] - 2020-04-07
### Changed
- Uncoupled Gather communication from work
	- Resend messages when an unhandled Exception occurs

## [1.0.1] - 2020-03-03
### Changed
- Retreival of UserIdentity for SubscriptionType


## [0.5.0] - 2020-04-01
### Changed
- Expect ManualDownloadReport as model from ManualDemoDownloader

## [0.4.0] - 2020-03-27
### Changed
- Quality is now received from Gatherer transfer model
- Ignore reanalysis for previouosly failed matches

### Removed
- `HTTP_USER_SUBSCRIPTION` env var

## ...

## [0.3.5] - 2020-03-17
### Changed
- Multiple endpoints for the webapp

## [0.3.4] - 2020-03-17
### Changed
- Hash endpoint now receives analyzer quality instead of frames

## [0.3.3] - 2020-03-12
### Fixed
- Missing MatchId in Models

## [0.3.2] - 2020-03-12
### Fixed
- Missing MatchId in Model

## [0.3.1] - 2020-03-12
### Added
- Http endpoint for browser extension update


## [0.3.0] - 2020-03-10
### Added
- CI
- More logging

### Fixed
 - Fails if depending http service is unresponsive



## [0.2.3] - 2020-03-05
### Added
- Documentation regarding specific mock environment variables.

## [0.2.2] - 2020-03-05
### Added
- Documentation.


## [0.2.1] - 2020-03-05
### Added
- Documentation.
- Sending to match data fanout
- Self-Migration

### Changed
- Database: rename FilePath => BlobUrl
- Rabbit to Release 0.5.0

###  Fixed
- Optimize database calls

## [0.2.0] - 2020-02-27
## Added
- Swagger documentation
- Docker support
- Manual Upload

## Updated
- Rabbit to Release 0.4.0
- Routes
