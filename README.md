# Virto Commerce Import Module

## Overview

The Import module is a framework for building data importers for the Virto Commerce platform. Other modules register *importers* that read records from a data source (CSV files, external systems) and write them into the platform. Users manage reusable *import profiles*, start and monitor imports, and review run history in the embedded **Import App**. Imports run as background jobs with live progress reporting, error collection, downloadable reports, and the ability to resume an interrupted run from a saved checkpoint.

## Key Features

* **Pluggable importers** — modules register `IDataImporter` implementations with their own reader, writer, validation, settings, and authorization requirement
* **Import profiles** — named, reusable configurations that bind an importer to a source file and importer-specific settings
* **Built-in CSV support** — `CsvDataReader` based on CsvHelper class maps, with configurable delimiter and page size, plus a CSV error reporter
* **Background processing** — imports run as Hangfire jobs, with at most one concurrent run per profile
* **Live progress** — push notifications with processed, total, and error counts plus an estimated remaining time
* **Resumable runs** — readers that implement `IImportDataReader<TCursor>` save a cursor every N pages, so an interrupted run continues from the last checkpoint
* **Error handling and reports** — errors are collected per record up to a configurable threshold and can be written to a downloadable report
* **Run history and notifications** — every run is recorded with its statistics, and the user who started it receives an email when it completes
* **Organization scoping** — for users who belong to an organization, profile and history searches, runs, and resumes are limited to that organization
* **Multi-database support** — SQL Server, MySQL, and PostgreSQL via dedicated EF Core provider assemblies

## Configuration

### Application Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Import.MaxErrorsCountThreshold` | Positive integer | `50` | Number of errors after which the import stops |
| `Import.DefaultImportReporter` | Short text | `DefaultDataReporter` | Reporter used when a profile does not specify one (`DefaultDataReporter` or `CsvDataReporter`) |
| `Import.RemainingEstimator` | Short text | `DefaultRemainingEstimator` | Algorithm for the estimated remaining time (`DefaultRemainingEstimator` or `LinearRegressionRemainingEstimator`) |
| `Import.Cursor.SaveIntervalPages` | Positive integer | `10` | How often, in pages, the reader cursor is saved for resuming a run |
| `Import.Cursor.LifetimeDays` | Positive integer | `7` | How many days a saved cursor stays valid for resuming |
| `Import.LimitOfLines` | Positive integer | `10000` | Hidden. Maximum number of lines in an import file |
| `Import.FileMaxSize` | Positive integer | `1` | Hidden. Maximum import file size, in MB |

### CSV Importer Settings

Importers built on `CsvDataReader` can add these settings to their import profiles with `WithSettings(CsvSettings.AllSettings)`:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Import.Csv.Delimiter` | Short text | `;` | Column delimiter in the CSV file |
| `Import.Csv.PageSize` | Positive integer | `50` | Number of records read and written per page |

### Permissions

| Permission | Description |
|------------|-------------|
| `import:access` | Open the Import App; view importers, profiles, and run history; preview, validate, run, resume, and cancel imports |
| `import:create` | Create import profiles |
| `import:update` | Update import profiles |
| `import:delete` | Delete import profiles |
| `import:read` | Declared by the module; not checked by its REST API |
| `import:execute` | Declared by the module; not checked by its REST API |

An importer can require an additional permission, which is checked when its import is run or resumed. Organization membership doesn't grant access by itself: for users who belong to an organization, it limits profile and history searches, runs, and resumes to that organization.

## Architecture

The module follows a layered architecture aligned with Virto Commerce platform conventions:

```
┌─────────────────────────────────────────────────────────────┐
│  Web Layer (API Controllers, Import App, Module init)       │
├─────────────────────────────────────────────────────────────┤
│  Data Layer (Services, Background Jobs, Repositories, EF)   │
├──────────┬──────────────┬──────────────┬────────────────────┤
│ SqlServer│    MySql     │  PostgreSql  │  DB Providers      │
├──────────┴──────────────┴──────────────┴────────────────────┤
│  CsvHelper (CSV reader, CSV reporter)                       │
├─────────────────────────────────────────────────────────────┤
│  Core Layer (Domain Models, Service Interfaces, Constants)  │
└─────────────────────────────────────────────────────────────┘
```

### Import Flow

1. The **Import App** (or an API client) posts an import profile to `POST /api/import/run`
2. The request is authorized: `import:access` first, then the importer's own requirement if it has one
3. `ImportRunService` enqueues an `ImportJob` in **Hangfire**; a distributed lock allows only one run per profile at a time
4. `DataImportProcessManager` creates the importer, reporter, and remaining-time estimator, then opens the reader and writer
5. When a run is resumed, the reader's cursor is restored from the run history
6. Records are read and written **page by page**; progress is sent as push notifications and saved to the run history, and the cursor is saved every `Import.Cursor.SaveIntervalPages` pages
7. The run stops when the source is exhausted, the error threshold is reached, or the user cancels it
8. On completion, the run history is finalized and an `ImportCompletedEmailNotification` is sent to the user who started the import

### Building an Importer

Register the importer in your module's `PostInitialize`:

```csharp
var importerRegistrar = appBuilder.ApplicationServices.GetRequiredService<IDataImporterRegistrar>();

importerRegistrar
    .Register<ProductCsvImporter>(() => appBuilder.ApplicationServices.GetRequiredService<ProductCsvImporter>())
    .WithSettings(ProductCsvImporterSettings.AllSettings)
    .WithAuthorizationPermission("my-module:import");
```

See [How to build a custom importer](/docs/03-building-custom-importer.md) for a complete walkthrough.

## Components

### Projects

| Project | Layer | Purpose |
|---------|-------|---------|
| `VirtoCommerce.ImportModule.Core` | Core | Domain models, importer and service interfaces, settings, permissions, notifications |
| `VirtoCommerce.ImportModule.CsvHelper` | Core | CSV data reader, CSV settings, and CSV error reporter |
| `VirtoCommerce.ImportModule.Data` | Data | Service implementations, import pipeline, background jobs, EF Core repository |
| `VirtoCommerce.ImportModule.Data.SqlServer` | Data | SQL Server EF Core configurations and migrations |
| `VirtoCommerce.ImportModule.Data.MySql` | Data | MySQL EF Core configurations and migrations |
| `VirtoCommerce.ImportModule.Data.PostgreSql` | Data | PostgreSQL EF Core configurations and migrations |
| `VirtoCommerce.ImportModule.Web` | Web | REST API controllers, authorization, Import App (VC-Shell), module bootstrapping |
| `VirtoCommerce.ImportModule.Tests` | Tests | Unit and integration tests |

### Key Services

| Service | Interface | Responsibility |
|---------|-----------|----------------|
| `ImportRunService` | `IImportRunService` | Starts, resumes, cancels, previews, and validates imports |
| `DataImportProcessManager` | `IDataImportProcessManager` | Runs the read-write loop, progress, error handling, and cursor checkpoints |
| `DataImporterRegistrar` | `IDataImporterRegistrar`, `IDataImporterFactory` | Registers importers and creates them by type name |
| `ImportReporterRegistrar` | `IImportReporterRegistrar`, `IImportReporterFactory` | Registers and creates error reporters |
| `ImportRemainingEstimatorRegistrar` | `IImportRemainingEstimatorRegistrar`, `IImportRemainingEstimatorFactory` | Registers and creates remaining-time estimators |
| `ImportProfileCrudService` | `IImportProfileCrudService` | CRUD operations for import profiles |
| `ImportProfilesSearchService` | `IImportProfilesSearchService` | Search import profiles |
| `ImportRunHistoryCrudService` | `IImportRunHistoryCrudService` | Persist run history and saved cursors |
| `ImportRunHistorySearchService` | `IImportRunHistorySearchService` | Search run history |
| `ImportAuthorizationHandler` | `IAuthorizationHandler` | Checks import permissions and resolves the user's organization scope |

### Extension Points

| Interface | Built-in implementations | Purpose |
|-----------|--------------------------|---------|
| `IDataImporter` | — (provided by other modules) | Defines an importer: reader, writer, validation, settings, authorization |
| `IImportDataReader` | `CsvDataReader` | Reads records page by page |
| `IImportDataReader<TCursor>` | — | Resumable reader: saves and restores a cursor so an interrupted run can continue |
| `IImportDataWriter` | — | Writes a page of records into the platform |
| `IImportReporter` | `DefaultDataReporter`, `CsvDataReporter` | Records import errors and produces a report |
| `IImportRemainingEstimator` | `DefaultRemainingEstimator`, `LinearRegressionRemainingEstimator` | Estimates the remaining import time |

### REST API

Base route: `api/import`

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| `GET` | `/importers` | `import:access` | List registered importers |
| `GET` | `/profiles/{profileId}` | `import:access` | Get an import profile by ID |
| `POST` | `/profiles/search` | `import:access` | Search import profiles |
| `POST` | `/profiles` | `import:create` | Create an import profile |
| `PUT` | `/profiles` | `import:update` | Update an import profile |
| `DELETE` | `/profiles` | `import:delete` | Delete an import profile |
| `POST` | `/profiles/execution/history/search` | `import:access` | Search run history |
| `POST` | `/preview` | `import:access` | Preview the first records of an import |
| `POST` | `/validate` | `import:access` | Validate an import profile and its source |
| `POST` | `/run` | `import:access` + importer's requirement | Start an import |
| `POST` | `/runs/resume` | `import:access` + importer's requirement | Resume an interrupted import |
| `POST` | `/task/cancel` | `import:access` | Cancel a running import |
| `GET` | `/organization` | `import:access` | Get the current user's organization |

## Documentation

* [Main concept](/docs/01-main-concept.md)
* [Additional development information](/docs/02-additional-development-information.md)
* [How to build a custom importer](/docs/03-building-custom-importer.md)
* [Data import developer guide](https://docs.virtocommerce.org/platform/developer-guide/Fundamentals/Data-Import/01-main-concept/)
* [Import App](https://docs.virtocommerce.org/platform/developer-guide/Fundamentals/Data-Import/import-app/)
* [REST API](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Import)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-import/)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-import/releases/latest)

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

This software is licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
distributed under the License is provided on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
