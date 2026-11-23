import 'package:go_router/go_router.dart';

import '../screens/dashboard/dashboard_screen.dart';
import '../screens/jobs/jobs_screen.dart';
import '../screens/news/news_screen.dart';
import '../screens/filters/filters_screen.dart';
import '../screens/favorites/favorites_screen.dart';
import '../screens/applications/applications_screen.dart';
import '../screens/statistics/statistics_screen.dart';
import '../screens/settings/settings_screen.dart';
import '../widgets/app_shell.dart';

final appRouter = GoRouter(
  initialLocation: '/dashboard',
  routes: [
    ShellRoute(
      builder: (context, state, child) =>
          AppShell(location: state.uri.path, child: child),
      routes: [
        GoRoute(path: '/dashboard', builder: (_, _) => const DashboardScreen()),
        GoRoute(path: '/jobs', builder: (_, _) => const JobsScreen()),
        GoRoute(path: '/news', builder: (_, _) => const NewsScreen()),
        GoRoute(path: '/filters', builder: (_, _) => const FiltersScreen()),
        GoRoute(path: '/favorites', builder: (_, _) => const FavoritesScreen()),
        GoRoute(path: '/applications', builder: (_, _) => const ApplicationsScreen()),
        GoRoute(path: '/statistics', builder: (_, _) => const StatisticsScreen()),
        GoRoute(path: '/settings', builder: (_, _) => const SettingsScreen()),
      ],
    ),
  ],
);
