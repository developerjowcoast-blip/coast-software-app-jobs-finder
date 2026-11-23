import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_theme.dart';
import '../../providers/app_providers.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/page_frame.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  @override
  Widget build(BuildContext context) => PageFrame(
        title: 'Configurações',
        subtitle: 'Ajuste a experiência do Job Finder ao seu jeito.',
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ref.watch(settingsProvider).when(
                  loading: LoadingState.new,
                  error: (error, _) => ErrorState(
                    message: friendlyError(error),
                    retry: () => ref.invalidate(settingsProvider),
                  ),
                  data: (settings) =>
                      _SettingsCard(settings: settings, onChanged: () => setState(() {})),
                ),
            const SizedBox(height: 18),
            ref.watch(sourcesProvider).when(
                  loading: LoadingState.new,
                  error: (error, _) => ErrorState(
                    message: friendlyError(error),
                    retry: () => ref.invalidate(sourcesProvider),
                  ),
                  data: (sources) => Card(
                    child: Padding(
                      padding: const EdgeInsets.all(22),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Fontes monitoradas',
                              style: TextStyle(
                                  fontSize: 17,
                                  fontWeight: FontWeight.w700)),
                          const SizedBox(height: 10),
                          ...sources.map((source) => SwitchListTile(
                                contentPadding: EdgeInsets.zero,
                                title: Text('${source['name'] ?? 'Fonte'}'),
                                subtitle: Text(
                                  '${source['description'] ?? ''}',
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                ),
                                value: source['enabled'] as bool? ?? false,
                                onChanged: (enabled) async {
                                  await ref
                                      .read(jobRepositoryProvider)
                                      .setSource('${source['name']}', enabled);
                                  ref.invalidate(sourcesProvider);
                                  ref.invalidate(dashboardProvider);
                                },
                              )),
                        ],
                      ),
                    ),
                  ),
                ),
          ],
        ),
      );
}

class _SettingsCard extends ConsumerStatefulWidget {
  const _SettingsCard({required this.settings, required this.onChanged});
  final Map<String, dynamic> settings;
  final VoidCallback onChanged;

  @override
  ConsumerState<_SettingsCard> createState() => _SettingsCardState();
}

class _SettingsCardState extends ConsumerState<_SettingsCard> {
  int? interval;
  int? limit;
  String? theme;

  @override
  void initState() {
    super.initState();
    interval =
        (widget.settings['refreshIntervalMinutes'] as num?)?.toInt() ?? 60;
    limit = (widget.settings['maximumRecords'] as num?)?.toInt() ?? 500;
    theme = '${widget.settings['theme'] ?? 'system'}';
  }

  @override
  Widget build(BuildContext context) {
    final intervals = <int>{5, 15, 30, 60, 120, 360, 720, 1440, interval!}
      ..removeWhere((value) => value < 5);
    final recordLimits = <int>{100, 250, 500, 1000, 2000, limit!};
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(22),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Preferências',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700)),
            const SizedBox(height: 20),
            DropdownButtonFormField<int>(
              initialValue: interval,
              decoration:
                  const InputDecoration(labelText: 'Intervalo de atualização'),
              items: (intervals.toList()..sort())
                  .map((value) => DropdownMenuItem(
                        value: value,
                        child: Text(value < 60
                            ? 'A cada $value minutos'
                            : 'A cada ${value ~/ 60} hora(s)'),
                      ))
                  .toList(),
              onChanged: (value) => setState(() => interval = value),
            ),
            const SizedBox(height: 14),
            DropdownButtonFormField<int>(
              initialValue: limit,
              decoration:
                  const InputDecoration(labelText: 'Limite de vagas armazenadas'),
              items: (recordLimits.toList()..sort())
                  .map((value) => DropdownMenuItem(
                      value: value, child: Text('$value registros')))
                  .toList(),
              onChanged: (value) => setState(() => limit = value),
            ),
            const SizedBox(height: 14),
            DropdownButtonFormField<String>(
              initialValue: theme,
              decoration: const InputDecoration(labelText: 'Tema do aplicativo'),
              items: const [
                ('system', 'Automático'),
                ('light', 'Claro'),
                ('dark', 'Escuro'),
              ].map((item) => DropdownMenuItem(
                    value: item.$1,
                    child: Text(item.$2),
                  )).toList(),
              onChanged: (value) {
                if (value == null) return;
                setState(() => theme = value);
                ref.read(themeModeProvider.notifier).state =
                    themeModeFromName(value);
              },
            ),
            const SizedBox(height: 18),
            FilledButton.icon(
              onPressed: () async {
                try {
                  await ref.read(jobRepositoryProvider).saveSettings({
                    'refreshIntervalMinutes': interval,
                    'maximumRecords': limit,
                    'theme': theme,
                  });
                  ref.invalidate(settingsProvider);
                  if (context.mounted) {
                    showAppMessage(context, 'Configurações salvas.');
                  }
                } catch (error) {
                  if (context.mounted) {
                    showAppMessage(context, friendlyError(error));
                  }
                }
              },
              icon: const Icon(Icons.save_outlined),
              label: const Text('Salvar preferências'),
            ),
          ],
        ),
      ),
    );
  }
}
