import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:local_notifier/local_notifier.dart';

import '../models/job.dart';
import '../providers/app_providers.dart';
import '../services/job_actions_service.dart';
import '../utils/formatters.dart';
import '../utils/ui_messages.dart';

class JobCard extends ConsumerStatefulWidget {
  const JobCard({required this.job, super.key});
  final Job job;

  @override
  ConsumerState<JobCard> createState() => _JobCardState();
}

class _JobCardState extends ConsumerState<JobCard> {
  Job get job => widget.job;

  @override
  void initState() {
    super.initState();
    if (job.score >= 85) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _notify());
    }
  }

  Future<void> _notify() async {
    try {
      final preferences = await SharedPreferences.getInstance();
      final key = 'notified-${job.id}';
      if (preferences.getBool(key) ?? false) return;
      await LocalNotification(
        title: 'Vaga com alta compatibilidade',
        subtitle: '${job.score}% de compatibilidade',
        body: '${job.title} • ${job.company}',
      ).show();
      await preferences.setBool(key, true);
    } catch (_) {}
  }

  Future<void> _run(Future<void> Function() action) async {
    try {
      await action();
      refreshJobData(ref);
      if (mounted) showAppMessage(context, 'Alteração salva.');
    } catch (error) {
      if (mounted) showAppMessage(context, friendlyError(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final actions = ref.read(jobActionsServiceProvider);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(19),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CircleAvatar(
                  backgroundColor: colors.primaryContainer,
                  foregroundColor: colors.primary,
                  child: Text(job.company.isEmpty
                      ? '?'
                      : job.company[0].toUpperCase()),
                ),
                const SizedBox(width: 13),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(job.title,
                          style: const TextStyle(
                              fontSize: 16, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 3),
                      Text(
                        '${job.company}  •  ${job.location}',
                        style: TextStyle(
                            color: colors.onSurfaceVariant, fontSize: 13),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: scoreColor(job.score).withValues(alpha: .12),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    '${job.score}% match',
                    style: TextStyle(
                        color: scoreColor(job.score),
                        fontSize: 12,
                        fontWeight: FontWeight.w700),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  tooltip: job.favorite ? 'Remover dos favoritos' : 'Favoritar',
                  onPressed: () =>
                      _run(() => actions.toggleFavorite(job)),
                  icon: Icon(
                    job.favorite ? Icons.bookmark : Icons.bookmark_border,
                    color: job.favorite ? colors.primary : null,
                  ),
                ),
              ],
            ),
            if (job.summary.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 14),
                child: Text(
                  job.summary,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                      color: colors.onSurfaceVariant, height: 1.45),
                ),
              ),
            const SizedBox(height: 14),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _Tag(
                    icon: Icons.work_outline,
                    label: readableValue(job.workMode)),
                if (job.salary?.isNotEmpty ?? false)
                  _Tag(icon: Icons.payments_outlined, label: job.salary!),
                _Tag(icon: Icons.language, label: job.source),
                _Tag(
                  icon: Icons.calendar_today_outlined,
                  label: formatPublishedDate(job.publishedAt),
                ),
                OutlinedButton.icon(
                  onPressed: () => _showStatusDialog(context, actions),
                  icon: const Icon(Icons.track_changes, size: 17),
                  label: Text(readableValue(job.status)),
                ),
                TextButton.icon(
                  onPressed: () => _run(() => actions.open(job)),
                  icon: const Icon(Icons.open_in_new, size: 17),
                  label: const Text('Abrir vaga'),
                ),
                IconButton(
                  tooltip: 'Ocultar vaga',
                  onPressed: () => _run(() => actions.hide(job)),
                  icon: const Icon(Icons.visibility_off_outlined, size: 19),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showStatusDialog(
    BuildContext context,
    JobActionsService actions,
  ) async {
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Atualizar candidatura'),
        content: DropdownButtonFormField<String>(
          initialValue: job.status,
          decoration: const InputDecoration(labelText: 'Status'),
          items: const [
            'Unseen',
            'Interesting',
            'Favorited',
            'Applied',
            'InReview',
            'Interview',
            'Offer',
            'Rejected',
            'Closed',
          ].map((status) => DropdownMenuItem(
                value: status,
                child: Text(readableValue(status)),
              )).toList(),
          onChanged: (value) {
            Navigator.pop(dialogContext);
            if (value != null) {
              _run(() => actions.updateStatus(job, value));
            }
          },
        ),
      ),
    );
  }
}

class _Tag extends StatelessWidget {
  const _Tag({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Chip(
        visualDensity: VisualDensity.compact,
        avatar: Icon(icon, size: 14),
        label: Text(label, style: const TextStyle(fontSize: 11)),
        side: BorderSide.none,
        backgroundColor: Theme.of(context)
            .colorScheme
            .surfaceContainerHighest
            .withValues(alpha: .55),
      );
}
