import 'package:shared_preferences/shared_preferences.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'providers/app_providers.dart';
import 'router/app_router.dart';
import 'core/theme/app_theme.dart';

class JobFinderApp extends ConsumerStatefulWidget {
  const JobFinderApp({super.key});

  @override
  ConsumerState<JobFinderApp> createState() => _JobFinderAppState();
}

class _JobFinderAppState extends ConsumerState<JobFinderApp> {
  @override
  void initState() {
    super.initState();
    SharedPreferences.getInstance().then((preferences) {
      if (!mounted) return;
      ref.read(themeModeProvider.notifier).state =
          themeModeFromName(preferences.getString(themePreferenceKey));
    });
  }

  @override
  Widget build(BuildContext context) {
    final mode = ref.watch(themeModeProvider);
    ref.listen(themeModeProvider, (_, next) {
      SharedPreferences.getInstance().then(
        (preferences) => preferences.setString(themePreferenceKey, next.name),
      );
    });
    return MaterialApp.router(
      title: 'Job Finder ADS',
      debugShowCheckedModeBanner: false,
      themeMode: mode,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      routerConfig: appRouter,
    );
  }
}
