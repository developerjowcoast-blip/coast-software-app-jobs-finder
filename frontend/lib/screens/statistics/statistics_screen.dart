import 'package:fl_chart/fl_chart.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../providers/app_providers.dart';
import '../../utils/ui_messages.dart';
import '../../widgets/async_states.dart';
import '../../widgets/page_frame.dart';

class StatisticsScreen extends ConsumerWidget {
  const StatisticsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => PageFrame(
        title: 'Estatísticas',
        subtitle: 'Entenda sua busca e acompanhe o mercado de oportunidades.',
        child: ref.watch(statisticsProvider).when(
              loading: LoadingState.new,
              error: (error, _) => ErrorState(
                message: friendlyError(error),
                retry: () => ref.invalidate(statisticsProvider),
              ),
              data: (data) => Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ChartCard(title: 'Vagas por fonte', data: _map(data['bySource'])),
                  const SizedBox(height: 16),
                  ChartCard(title: 'Vagas por categoria', data: _map(data['byCategory'])),
                  const SizedBox(height: 16),
                  ChartCard(title: 'Modalidade de trabalho', data: _map(data['byWorkMode'])),
                  const SizedBox(height: 16),
                  ChartCard(title: 'Vagas por mês', data: _map(data['monthly'])),
                ],
              ),
            ),
      );

  Map<String, int> _map(dynamic value) {
    if (value is! Map) return {};
    return value.map((key, val) => MapEntry('$key', (val as num).toInt()));
  }
}

class ChartCard extends StatelessWidget {
  const ChartCard({required this.title, required this.data, super.key});
  final String title;
  final Map<String, int> data;

  @override
  Widget build(BuildContext context) {
    final entries = data.entries.toList()
      ..sort((a, b) => b.value.compareTo(a.value));
    final shown = entries.take(8).toList();
    if (shown.isEmpty) {
      return Card(
        child: Padding(
          padding: const EdgeInsets.all(22),
          child: Text('$title — sem dados disponíveis'),
        ),
      );
    }
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(22),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title,
                style:
                    const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
            const SizedBox(height: 22),
            SizedBox(
              height: 230,
              child: BarChart(
                BarChartData(
                  maxY: (shown.map((entry) => entry.value).reduce((a, b) => a > b ? a : b) * 1.2).clamp(1, double.infinity),
                  gridData:
                      const FlGridData(show: true, drawVerticalLine: false),
                  borderData: FlBorderData(show: false),
                  titlesData: FlTitlesData(
                    topTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false)),
                    rightTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false)),
                    leftTitles: const AxisTitles(
                        sideTitles:
                            SideTitles(showTitles: true, reservedSize: 34)),
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 40,
                        getTitlesWidget: (value, meta) {
                          final index = value.toInt();
                          return index >= 0 && index < shown.length
                              ? SideTitleWidget(
                                  meta: meta,
                                  child: Text(shown[index].key,
                                      style: const TextStyle(fontSize: 9),
                                      overflow: TextOverflow.ellipsis),
                                )
                              : const SizedBox.shrink();
                        },
                      ),
                    ),
                  ),
                  barGroups: List.generate(
                    shown.length,
                    (index) => BarChartGroupData(
                      x: index,
                      barRods: [
                        BarChartRodData(
                          toY: shown[index].value.toDouble(),
                          color: Theme.of(context).colorScheme.primary,
                          width: 23,
                          borderRadius: BorderRadius.circular(5),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
