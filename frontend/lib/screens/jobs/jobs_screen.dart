import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../providers/app_providers.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/job_card.dart';
import '../../widgets/page_frame.dart';

class JobsScreen extends ConsumerStatefulWidget {
  const JobsScreen({super.key});

  @override
  ConsumerState<JobsScreen> createState() => _JobsScreenState();
}

class _JobsScreenState extends ConsumerState<JobsScreen> {
  final _search = TextEditingController();
  int _page = 1;

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  void _applySearch(String value) {
    ref.read(jobFilterProvider.notifier).state = {
      ...ref.read(jobFilterProvider),
      if (value.trim().isNotEmpty) 'q': value.trim(),
    };
    setState(() => _page = 1);
  }

  @override
  Widget build(BuildContext context) {
    final filters = {
      ...ref.watch(jobFilterProvider),
      'page': _page,
      'pageSize': 20,
    };
    return PageFrame(
      title: 'Vagas',
      subtitle: 'Encontre oportunidades alinhadas com seus objetivos.',
      trailing: OutlinedButton.icon(
        onPressed: () => context.go('/filters'),
        icon: const Icon(Icons.tune),
        label: const Text('Filtros'),
      ),
      child: Column(
        children: [
          TextField(
            controller: _search,
            onSubmitted: _applySearch,
            decoration: InputDecoration(
              prefixIcon: const Icon(Icons.search),
              hintText: 'Cargo, empresa ou palavra-chave',
              suffixIcon: IconButton(
                icon: const Icon(Icons.arrow_forward),
                onPressed: () => _applySearch(_search.text),
              ),
            ),
          ),
          const SizedBox(height: 18),
          ref.watch(jobsProvider(filters)).when(
                loading: LoadingState.new,
                error: (error, _) => ErrorState(
                  message: friendlyError(error),
                  retry: () => ref.invalidate(jobsProvider),
                ),
                data: (page) => page.items.isEmpty
                    ? const EmptyState(
                        title: 'Nenhuma vaga encontrada',
                        message: 'Ajuste os filtros ou busque novas vagas.',
                      )
                    : Column(
                        children: [
                          ...page.items.map(
                            (job) => Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: JobCard(job: job),
                            ),
                          ),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              IconButton(
                                onPressed: _page > 1
                                    ? () => setState(() => _page--)
                                    : null,
                                icon: const Icon(Icons.chevron_left),
                              ),
                              Text('Página $_page  •  ${page.total} vagas'),
                              IconButton(
                                onPressed: _page * 20 < page.total
                                    ? () => setState(() => _page++)
                                    : null,
                                icon: const Icon(Icons.chevron_right),
                              ),
                            ],
                          ),
                        ],
                      ),
              ),
        ],
      ),
    );
  }
}
