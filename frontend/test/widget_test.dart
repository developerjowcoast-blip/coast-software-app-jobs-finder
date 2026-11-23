import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:job_finder_frontend/features/jobs/models/job.dart';
import 'package:job_finder_frontend/features/jobs/providers.dart';
import 'package:job_finder_frontend/features/jobs/data/job_repository.dart';
import 'package:job_finder_frontend/core/api/job_api.dart';
import 'package:job_finder_frontend/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  testWidgets('application starts on the dashboard', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          dashboardProvider.overrideWith(
            (ref) async => {
              'totalJobs': 0,
              'newJobsToday': 0,
              'favorites': 0,
              'averageCompatibility': 0,
            },
          ),
          jobsProvider.overrideWith(
            (ref, filters) async => const JobPage([], 0),
          ),
        ],
        child: const JobFinderApp(),
      ),
    );
    await tester.pump();

    expect(find.text('Dashboard'), findsWidgets);
    expect(find.text('Seu próximo passo começa aqui'), findsOneWidget);
  });

  testWidgets('news screen shows only the provided novidades', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          newsProvider.overrideWith(
            (ref) async => NewsPage(
              items: [
                NewsJob(
                  job:                   Job(
                    id: 'news-1',
                    title: 'Estágio QA',
                    company: 'Empresa',
                    location: 'São Paulo',
                    workMode: 'Hybrid',
                    source: 'Gupy',
                    url: 'https://empresa.gupy.io/job/news-1',
                    publishedAt: DateTime(2026, 11, 20),
                    score: 94,
                    summary: 'Oportunidade em QA',
                    category: 'QA',
                    status: 'Unseen',
                    favorite: false,
                    hidden: false,
                  ),
                  isNewSinceLastVisit: true,
                  collectedLast24Hours: true,
                  scoreChanged: true,
                  previousScore: 82,
                  scoreChangedAt: DateTime(2026, 11, 20),
                  isViewed: false,
                  viewedAt: null,
                ),
              ],
              lastVisitAt: DateTime(2026, 11, 19, 10),
              currentVisitAt: DateTime(2026, 11, 20, 10),
              total: 1,
            ),
          ),
          jobRepositoryProvider.overrideWithValue(
            JobRepository(JobApi(baseUrl: 'http://localhost:5000')),
          ),
        ],
        child: MaterialApp(home: const Scaffold(body: NewsScreen())),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Novidades'), findsOneWidget);
    expect(find.text('Estágio QA'), findsOneWidget);
    expect(find.text('Coletada nas últimas 24h'), findsOneWidget);
    expect(find.text('Score: 82% → 94%'), findsOneWidget);
  });
}
