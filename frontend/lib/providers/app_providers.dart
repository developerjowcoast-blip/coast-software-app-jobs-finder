import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../core/api/job_api.dart';
import '../models/job.dart';
import '../repositories/job_repository.dart';

final apiProvider = Provider<JobApi>((ref) => JobApi());
final jobRepositoryProvider = Provider<JobRepository>(
  (ref) => JobRepository(ref.watch(apiProvider)),
);
final themeModeProvider = StateProvider<ThemeMode>((ref) => ThemeMode.system);
final jobFilterProvider = StateProvider<Map<String, dynamic>>((ref) => {});

final dashboardProvider = FutureProvider<Map<String, dynamic>>(
  (ref) => ref.watch(jobRepositoryProvider).dashboard(),
);
final jobsProvider = FutureProvider.family<JobPage, Map<String, dynamic>>(
  (ref, filters) => ref.watch(jobRepositoryProvider).jobs(filters),
);
final favoritesProvider = FutureProvider(
  (ref) => ref.watch(jobRepositoryProvider).favorites(),
);
final sourcesProvider = FutureProvider(
  (ref) => ref.watch(jobRepositoryProvider).sources(),
);
final settingsProvider = FutureProvider(
  (ref) => ref.watch(jobRepositoryProvider).settings(),
);
final statisticsProvider = FutureProvider(
  (ref) => ref.watch(jobRepositoryProvider).statistics(),
);
final newsProvider = FutureProvider(
  (ref) => ref.watch(jobRepositoryProvider).news(),
);

void refreshJobData(WidgetRef ref) {
  ref.invalidate(dashboardProvider);
  ref.invalidate(jobsProvider);
  ref.invalidate(favoritesProvider);
  ref.invalidate(statisticsProvider);
  ref.invalidate(newsProvider);
}
