# Controller Generator

## Overview

`ControllerGenerator.py` is a code-generation script that automatically creates ASP.NET Core API controllers from DL (domain layer) service interfaces. It eliminates the need to manually write boilerplate CRUD controllers for each entity in the system.

## What It Does

The script reads C# interface files from a configured input directory, detects which services they implement (`IDLReadableService` and/or `IDLWritableService`), and generates corresponding REST controllers with standard endpoints:

- **Readable services** get `GET /` (GetAll) and `GET /lookup` (GetLookup)
- **Writable services** get `POST /` (Create), `PUT /` (Update), and `DELETE /{id}` (DeleteOrDeactivate)

It also generates an abstract **base controller** (`{App}BaseController<T, TKey>`) that all generated controllers inherit from, providing shared routing and a common `GetById` endpoint.


## How to Run

```bash
python3 ControllerGenerator.py
```

The script must be run from anywhere — it resolves its own directory to locate the configuration file and input/output folders.

## Configuration

The script reads from `ControllerGenerator.json` (located in the same directory) with the following keys:

| Key | Description |
|-----|-------------|
| `Namespace` | The C# namespace for generated controllers |
| `InputDir` | Folder containing `.cs` service interface files (relative to script directory) |
| `App` | Application name prefix (e.g., `Biller`) — used for route prefixes (`api/{app}/[controller]`) and the base controller class name |

## Output

Generated controllers are written to the `Generated/` subdirectory. Each file includes a header comment indicating it is auto-generated and instructing developers to use partial classes for any custom logic.

## Important Notes

- **Do not edit files in `Generated/` directly** — they will be overwritten on the next run.
- To add custom endpoints or override behaviour, create a partial class in a separate file OUTSIDE the `Generated/` folder, usually in the parent directory.
- The source of truth for each controller is its corresponding service interface in the configured input directory. Add or modify interfaces there, then re-run the generator.
