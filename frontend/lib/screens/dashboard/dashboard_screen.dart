import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/app_providers.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/job_card.dart';
import '../../widgets/page_frame.dart';
import '../../widgets/section_heading.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => PageFrame(
        title: 'Dashboard',
        subtitle: 'Acompanhe oportunidades e mantenha sua busca em movimento.',
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ref.watch(dashboardProvider).when(
                  loading: LoadingState.new,
                  error: (error, _) => ErrorState(
                    message: friendlyError(error),
                    retry: () => ref.invalidate(dashboardProvider),
                  ),
                  data: (data) => DashboardMetrics(data: data),
                ),
            const SizedBox(height: 30),
            SectionHeading(
              title: 'Oportunidades recentes',
              action: TextButton(
                onPressed: () => context.go('/jobs'),
                child: const Text('Ver todas  →'),
              ),
            ),
            const SizedBox(height: 14),
            ref.watch(jobsProvider(const {'page': 1, 'pageSize': 4})).when(
                  loading: LoadingState.new,
                  error: (error, _) => ErrorState(
                    message: friendlyError(error),
                    retry: () => ref.invalidate(jobsProvider),
                  ),
                  data: (page) => page.items.isEmpty
                      ? const EmptyState(
                          title: 'Ainda não há vagas',
                          message: 'Use “Buscar vagas” para carregar novas oportunidades.',
                        )
                      : Column(
                          children: page.items
                              .take(4)
                              .map((job) => Padding(
                                    padding: const EdgeInsets.only(bottom: 12),
                                    child: JobCard(job: job),
                                  ))
                              .toList(),
                        ),
                ),
          ],
        ),
      );
}

class DashboardMetrics extends StatelessWidget {
  const DashboardMetrics({required this.data, super.key});
  final Map<String, dynamic> data;

  @override
  Widget build(BuildContext context) {
    final metrics = <(String, String, IconData, String)>[
      ('Vagas encontradas', '${data['totalJobs'] ?? 0}', Icons.work_outline, 'No seu radar'),
      ('Novas hoje', '${data['newJobsToday'] ?? 0}', Icons.bolt_rounded, 'Recentes'),
      ('Favoritas', '${data['favorites'] ?? 0}', Icons.bookmark_border_rounded, 'Salvas'),
      ('Compatibilidade', '${((data['averageCompatibility'] as num?) ?? 0).round()}%', Icons.auto_awesome_outlined, 'Média'),
      ('Candidaturas ativas', '${data['activeApplications'] ?? 0}', Icons.track_changes_outlined, 'Em andamento'),
      ('Fontes monitoradas', '${data['monitoredSources'] ?? 0}', Icons.rss_feed_outlined, 'Ativas'),
    ];
    return LayoutBuilder(
      builder: (context, box) {
        final columns = box.maxWidth > 900 ? 3 : box.maxWidth > 540 ? 2 : 1;
        return GridView.count(
          crossAxisCount: columns,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          childAspectRatio: columns == 1 ? 4.2 : 2.1,
          crossAxisSpacing: 14,
          mainAxisSpacing: 14,
          children: metrics.map((metric) => MetricCard(metric: metric)).toList(),
        );
      },
    );
  }
}

class MetricCard extends StatelessWidget {
  const MetricCard({required this.metric, super.key});
  final (String, String, IconData, String) metric;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(19),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(9),
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.primaryContainer,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(metric.$3,
                        size: 20, color: Theme.of(context).colorScheme.primary),
                  ),
                  const Spacer(),
                  Text(metric.$4,
                      style: Theme.of(context).textTheme.labelSmall?.copyWith(
                            color:
                                Theme.of(context).colorScheme.onSurfaceVariant,
                          )),
                ],
              ),
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(metric.$2,
                      style: Theme.of(context)
                          .textTheme
                          .headlineMedium
                          ?.copyWith(fontWeight: FontWeight.w800)),
                  const SizedBox(width: 9),
                  Expanded(
                    child: Text(metric.$1,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.onSurfaceVariant,
                          fontSize: 12,
                        )),
                  ),
                ],
              ),
            ],
          ),
        ),
      );
}
