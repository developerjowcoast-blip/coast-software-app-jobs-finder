import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:local_notifier/local_notifier.dart';
import 'package:material_ui/material_ui.dart';

import 'app.dart';

export 'app.dart';
export 'screens/news/news_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await localNotifier.setup(appName: 'Job Finder ADS');
  runApp(const ProviderScope(child: JobFinderApp()));
}
