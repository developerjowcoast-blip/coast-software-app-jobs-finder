import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/job.dart';
import '../../providers/app_providers.dart';
import '../../services/job_actions_service.dart';
import '../../utils/formatters.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/page_frame.dart';

class NewsScreen extends ConsumerStatefulWidget {
  const NewsScreen({super.key});

  @override
  ConsumerState<NewsScreen> createState() => _NewsScreenState();
}

class _NewsScreenState extends ConsumerState<NewsScreen> {
  bool _visitRecorded = false;

  @override
  Widget build(BuildContext context) => PageFrame(
        title: 'Novidades',
        subtitle: 'Vagas novas e mudanças de compatibilidade desde sua última visita.',
        child: ref.watch(newsProvider).when(
              loading: LoadingState.new,
              error: (error, _) => ErrorState(
                message: friendlyError(error),
                retry: () => ref.invalidate(newsProvider),
              ),
              data: (page) {
                if (!_visitRecorded) {
                  _visitRecorded = true;
                  WidgetsBinding.instance.addPostFrameCallback((_) async {
                    try {
                      await ref.read(jobRepositoryProvider).recordNewsVisit();
                    } catch (_) {}
                  });
                }
                return NewsContent(page: page);
              },
            ),
      );
}

class NewsContent extends ConsumerWidget {
  const NewsContent({required this.page, super.key});
  final NewsPage page;

  @override
  Widget build(BuildContext context, WidgetRef ref) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  page.lastVisitAt == null
                      ? 'Primeira visita • novidades das últimas 24 horas'
                      : 'Última visita: ${formatVisit(page.lastVisitAt!)}',
                ),
              ),
              if (page.items.any((item) => !item.isViewed))
                OutlinedButton.icon(
                  onPressed: () async {
                    final repository = ref.read(jobRepositoryProvider);
                    for (final item
                        in page.items.where((item) => !item.isViewed)) {
                      await repository.markNewsViewed(item.job);
                    }
                    ref.invalidate(newsProvider);
                  },
                  icon: const Icon(Icons.done_all_rounded),
                  label: const Text('Marcar todas'),
                ),
            ],
          ),
          const SizedBox(height: 18),
          if (page.items.isEmpty)
            const EmptyState(
              title: 'Nada novo por aqui',
              message: 'Nenhuma vaga nova ou mudança de score desde sua última visita.',
            )
          else
            Column(
              children: page.items
                  .map((item) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: NewsJobCard(item: item),
                      ))
                  .toList(),
            ),
        ],
      );
}

class NewsJobCard extends ConsumerWidget {
  const NewsJobCard({required this.item, super.key});
  final NewsJob item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final job = item.job;
    final colors = Theme.of(context).colorScheme;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(job.title,
                style:
                    const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            Text('${job.company} • ${job.location}',
                style:
                    TextStyle(color: colors.onSurfaceVariant, fontSize: 13)),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                if (item.collectedLast24Hours)
                  const Chip(
                    label: Text('Coletada nas últimas 24h'),
                    avatar: Icon(Icons.bolt_rounded, size: 17),
                  ),
                if (item.scoreChanged)
                  Chip(
                    label: Text(
                      'Score: ${item.previousScore}% → ${job.score}%',
                    ),
                  ),
                if (item.isViewed)
                  const Chip(
                    label: Text('Visualizada'),
                    avatar: Icon(Icons.done_rounded, size: 17),
                  ),
              ],
            ),
            const SizedBox(height: 10),
            if (!item.isViewed)
              Row(
                children: [
                  TextButton.icon(
                    onPressed: () async {
                      await ref
                          .read(jobActionsServiceProvider)
                          .open(job);
                    },
                    icon: const Icon(Icons.open_in_new_rounded),
                    label: const Text('Abrir vaga'),
                  ),
                  const Spacer(),
                  FilledButton.tonalIcon(
                    onPressed: () async {
                      await ref
                          .read(jobRepositoryProvider)
                          .markNewsViewed(item.job);
                      ref.invalidate(newsProvider);
                    },
                    icon: const Icon(Icons.done_rounded),
                    label: const Text('Marcar visualizada'),
                  ),
                ],
              ),
          ],
        ),
      ),
    );
  }
}
