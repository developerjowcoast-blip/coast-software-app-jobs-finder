import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../providers/app_providers.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/job_card.dart';
import '../../widgets/page_frame.dart';

class FavoritesScreen extends ConsumerWidget {
  const FavoritesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => PageFrame(
        title: 'Favoritas',
        subtitle: 'Oportunidades que você salvou para acompanhar.',
        child: ref.watch(favoritesProvider).when(
              loading: LoadingState.new,
              error: (error, _) => ErrorState(
                message: friendlyError(error),
                retry: () => ref.invalidate(favoritesProvider),
              ),
              data: (jobs) => jobs.isEmpty
                  ? const EmptyState(
                      title: 'Sua lista está vazia',
                      message: 'Salve vagas interessantes para encontrá-las aqui.',
                    )
                  : Column(
                      children: jobs
                          .map((job) => Padding(
                                padding: const EdgeInsets.only(bottom: 12),
                                child: JobCard(job: job),
                              ))
                          .toList(),
                    ),
            ),
      );
}
