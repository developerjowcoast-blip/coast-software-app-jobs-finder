# Job Finder ADS — Flutter desktop

Flutter desktop client for the Job Finder REST API (.NET 9). The app uses a feature-oriented structure, Riverpod providers, Dio repositories, GoRouter navigation, Material 3, and a responsive desktop layout.

## Requirements

- Flutter 3.50 / Dart 3.14 or newer
- Windows 10/11 with Developer Mode enabled (needed by Flutter plugins)
- Visual Studio 2022 with the **Desktop development with C++** workload to build Windows
- The API running locally (from the repository root):

```powershell
dotnet run --project src/JobFinder.API --urls http://localhost:5000
```

## Run

From `frontend/`:

```powershell
flutter pub get
flutter run -d windows --dart-define=JOB_FINDER_API_URL=http://localhost:5000
```

The API URL defaults to `http://localhost:5000`; override it with `JOB_FINDER_API_URL` when launching/building. The API exposes `/api` routes documented in the root README. API database and log settings remain server-managed; client theme preference and high-score notification history are stored locally.

## Verify and build

```powershell
flutter analyze
flutter test
flutter build windows --debug --dart-define=JOB_FINDER_API_URL=http://localhost:5000
```

The client includes dashboard metrics, searchable and filterable jobs, favorites, application tracking, statistics charts, source and refresh settings, local theme selection, native high-compatibility notifications, and external URL launching.
