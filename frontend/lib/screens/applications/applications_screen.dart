import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../providers/app_providers.dart';
import '../../utils/formatters.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/job_card.dart';
import '../../widgets/page_frame.dart';
import '../dashboard/dashboard_screen.dart';

class ApplicationsScreen extends ConsumerWidget {
  const ApplicationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => PageFrame(
        title: 'Candidaturas',
        subtitle: 'Acompanhe cada etapa do seu processo seletivo.',
        child: ref.watch(jobsProvider(const {
          'page': 1,
          'pageSize': 200,
          'includeHidden': true,
        })).when(
              loading: LoadingState.new,
              error: (error, _) => ErrorState(
                message: friendlyError(error),
                retry: () => ref.invalidate(jobsProvider),
              ),
              data: (page) {
                final jobs = page.items.where((job) =>
                    !['Unseen', 'Interesting', 'Favorited'].contains(job.status)).toList();
                const statuses = [
                  'Applied',
                  'InReview',
                  'Interview',
                  'Offer',
                  'Rejected',
                  'Closed',
                ];
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Wrap(
                      spacing: 12,
                      runSpacing: 12,
                      children: statuses.map((status) {
                        final count =
                            jobs.where((job) => job.status == status).length;
                        return SizedBox(
                          width: 170,
                          child: MetricCard(
                            metric: (
                              readableValue(status),
                              '$count',
                              _statusIcon(status),
                              'Candidaturas',
                            ),
                          ),
                        );
                      }).toList(),
                    ),
                    const SizedBox(height: 24),
                    if (jobs.isEmpty)
                      const EmptyState(
                        title: 'Nenhuma candidatura registrada',
                        message:
                            'Atualize o status de uma vaga para acompanhar sua candidatura.',
                      )
                    else
                      ...jobs.map((job) => Padding(
                            padding: const EdgeInsets.only(bottom: 12),
                            child: JobCard(job: job),
                          )),
                  ],
                );
              },
            ),
      );

  IconData _statusIcon(String status) => switch (status) {
        'Applied' => Icons.send_outlined,
        'InReview' => Icons.rate_review_outlined,
        'Interview' => Icons.forum_outlined,
        'Offer' => Icons.celebration_outlined,
        'Rejected' => Icons.cancel_outlined,
        _ => Icons.check_circle_outline,
      };
}
