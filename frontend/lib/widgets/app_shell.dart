import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../providers/app_providers.dart';
import '../utils/ui_messages.dart';

typedef AppNavItem = ({String path, String label, IconData icon});

const appNavItems = <AppNavItem>[
  (path: '/dashboard', label: 'Dashboard', icon: Icons.space_dashboard_outlined),
  (path: '/jobs', label: 'Vagas', icon: Icons.work_outline),
  (path: '/news', label: 'Novidades', icon: Icons.new_releases_outlined),
  (path: '/filters', label: 'Filtros', icon: Icons.tune_rounded),
  (path: '/favorites', label: 'Favoritas', icon: Icons.bookmark_border_rounded),
  (path: '/applications', label: 'Candidaturas', icon: Icons.track_changes_outlined),
  (path: '/statistics', label: 'Estatísticas', icon: Icons.bar_chart_rounded),
  (path: '/settings', label: 'Configurações', icon: Icons.settings_outlined),
];

class AppShell extends ConsumerWidget {
  const AppShell({required this.location, required this.child, super.key});
  final String location;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final wide = MediaQuery.sizeOf(context).width >= 900;
    final selected = appNavItems.indexWhere((item) => item.path == location).clamp(0, appNavItems.length - 1);
    return Scaffold(
      body: Row(
        children: [
          if (wide) _SideNavigation(location: location, selected: selected),
          Expanded(
            child: Column(
              children: [
                AppTopBar(onCollect: () => _collect(ref, context)),
                Expanded(child: child),
              ],
            ),
          ),
        ],
      ),
      bottomNavigationBar: wide
          ? null
          : NavigationBar(
              selectedIndex: selected,
              onDestinationSelected: (index) =>
                  context.go(appNavItems[index].path),
              destinations: appNavItems
                  .map((item) => NavigationDestination(
                        icon: Icon(item.icon),
                        label: item.label,
                      ))
                  .toList(),
            ),
    );
  }

  Future<void> _collect(WidgetRef ref, BuildContext context) async {
    try {
      await ref.read(jobRepositoryProvider).collect();
      refreshJobData(ref);
      if (context.mounted) {
        showAppMessage(context, 'Busca de novas vagas iniciada.');
      }
    } catch (error) {
      if (context.mounted) {
        showAppMessage(context, friendlyError(error));
      }
    }
  }
}

class _SideNavigation extends StatelessWidget {
  const _SideNavigation({required this.location, required this.selected});
  final String location;
  final int selected;

  @override
  Widget build(BuildContext context) => Container(
        width: 238,
        color: Theme.of(context).colorScheme.surface,
        child: SafeArea(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Padding(
                padding: EdgeInsets.fromLTRB(22, 24, 16, 28),
                child: Row(
                  children: [
                    CircleAvatar(
                      radius: 21,
                      backgroundColor: Color(0xFF246B5B),
                      child: Icon(Icons.work_rounded, color: Colors.white),
                    ),
                    SizedBox(width: 12),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Job Finder',
                            style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
                        Text('ADS • Desktop',
                            style: TextStyle(fontSize: 11, color: Colors.grey)),
                      ],
                    ),
                  ],
                ),
              ),
              for (var index = 0; index < appNavItems.length; index++)
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 2),
                  child: _NavigationItem(
                    item: appNavItems[index],
                    selected: selected == index,
                  ),
                ),
            ],
          ),
        ),
      );
}

class _NavigationItem extends StatelessWidget {
  const _NavigationItem({required this.item, required this.selected});
  final AppNavItem item;
  final bool selected;

  @override
  Widget build(BuildContext context) => ListTile(
        dense: true,
        selected: selected,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        selectedTileColor: Theme.of(context).colorScheme.primaryContainer.withValues(alpha: .65),
        leading: Icon(item.icon, size: 20),
        title: Text(item.label,
            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
        onTap: () => context.go(item.path),
      );
}

class AppTopBar extends StatelessWidget {
  const AppTopBar({required this.onCollect, super.key});
  final VoidCallback onCollect;

  @override
  Widget build(BuildContext context) => Container(
        height: 74,
        padding: const EdgeInsets.symmetric(horizontal: 30),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          border: Border(
            bottom: BorderSide(
              color: Theme.of(context).dividerColor.withValues(alpha: .12),
            ),
          ),
        ),
        child: Row(
          children: [
            Text(
              'Seu próximo passo começa aqui',
              style: TextStyle(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
                fontSize: 13,
              ),
            ),
            const Spacer(),
            IconButton(
              tooltip: 'Notificações',
              onPressed: () =>
                  showAppMessage(context, 'Você está em dia com suas notificações.'),
              icon: const Icon(Icons.notifications_none_rounded),
            ),
            const SizedBox(width: 6),
            FilledButton.icon(
              onPressed: onCollect,
              icon: const Icon(Icons.sync_rounded, size: 18),
              label: const Text('Buscar vagas'),
            ),
          ],
        ),
      );
}
