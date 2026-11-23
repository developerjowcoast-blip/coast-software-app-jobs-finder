import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../providers/app_providers.dart';
import '../../widgets/page_frame.dart';

class FiltersScreen extends ConsumerStatefulWidget {
  const FiltersScreen({super.key});

  @override
  ConsumerState<FiltersScreen> createState() => _FiltersScreenState();
}

class _FiltersScreenState extends ConsumerState<FiltersScreen> {
  final _query = TextEditingController();
  final _company = TextEditingController();
  final _location = TextEditingController();
  final _category = TextEditingController();
  String _mode = '';
  double _score = 0;
  bool _spanish = false;
  bool _inclusive = false;

  @override
  void initState() {
    super.initState();
    final filters = ref.read(jobFilterProvider);
    _query.text = '${filters['q'] ?? ''}';
    _company.text = '${filters['company'] ?? ''}';
    _location.text = '${filters['location'] ?? ''}';
    _category.text = '${filters['category'] ?? ''}';
    _mode = '${filters['workMode'] ?? ''}';
    _score = ((filters['minimumScore'] as num?) ?? 0).toDouble();
    _spanish = filters['requiresSpanish'] as bool? ?? false;
    _inclusive = filters['inclusiveEmployer'] as bool? ?? false;
  }

  @override
  void dispose() {
    _query.dispose();
    _company.dispose();
    _location.dispose();
    _category.dispose();
    super.dispose();
  }

  void _apply() {
    ref.read(jobFilterProvider.notifier).state = {
      if (_query.text.trim().isNotEmpty) 'q': _query.text.trim(),
      if (_company.text.trim().isNotEmpty) 'company': _company.text.trim(),
      if (_location.text.trim().isNotEmpty) 'location': _location.text.trim(),
      if (_category.text.trim().isNotEmpty) 'category': _category.text.trim(),
      if (_mode.isNotEmpty) 'workMode': _mode,
      if (_score > 0) 'minimumScore': _score.round(),
      if (_spanish) 'requiresSpanish': true,
      if (_inclusive) 'inclusiveEmployer': true,
    };
    context.go('/jobs');
  }

  void _clear() {
    ref.read(jobFilterProvider.notifier).state = {};
    _query.clear();
    _company.clear();
    _location.clear();
    _category.clear();
    setState(() {
      _mode = '';
      _score = 0;
      _spanish = false;
      _inclusive = false;
    });
  }

  @override
  Widget build(BuildContext context) => PageFrame(
        title: 'Filtros',
        subtitle: 'Personalize sua busca para descobrir as vagas certas.',
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                TextField(
                  controller: _query,
                  decoration: const InputDecoration(
                    labelText: 'Cargo ou palavra-chave',
                    prefixIcon: Icon(Icons.search),
                  ),
                ),
                const SizedBox(height: 15),
                TextField(
                  controller: _company,
                  decoration: const InputDecoration(
                    labelText: 'Empresa',
                    prefixIcon: Icon(Icons.business_outlined),
                  ),
                ),
                const SizedBox(height: 15),
                TextField(
                  controller: _location,
                  decoration: const InputDecoration(
                    labelText: 'Localização',
                    prefixIcon: Icon(Icons.place_outlined),
                  ),
                ),
                const SizedBox(height: 15),
                TextField(
                  controller: _category,
                  decoration: const InputDecoration(
                    labelText: 'Categoria',
                    prefixIcon: Icon(Icons.category_outlined),
                  ),
                ),
                const SizedBox(height: 15),
                DropdownButtonFormField<String>(
                  initialValue: _mode,
                  decoration: const InputDecoration(labelText: 'Modalidade'),
                  items: const [
                    ('', 'Todas'),
                    ('Remote', 'Remoto'),
                    ('Hybrid', 'Híbrido'),
                    ('OnSite', 'Presencial'),
                  ].map((item) => DropdownMenuItem(
                        value: item.$1,
                        child: Text(item.$2),
                      )).toList(),
                  onChanged: (value) => setState(() => _mode = value ?? ''),
                ),
                const SizedBox(height: 18),
                Row(
                  children: [
                    const Text('Compatibilidade mínima'),
                    const Spacer(),
                    Text('${_score.round()}%'),
                  ],
                ),
                Slider(
                  value: _score,
                  min: 0,
                  max: 100,
                  divisions: 20,
                  label: '${_score.round()}%',
                  onChanged: (value) => setState(() => _score = value),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Exige espanhol'),
                  value: _spanish,
                  onChanged: (value) => setState(() => _spanish = value),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Empregador inclusivo'),
                  value: _inclusive,
                  onChanged: (value) => setState(() => _inclusive = value),
                ),
                const SizedBox(height: 18),
                Wrap(
                  spacing: 10,
                  children: [
                    FilledButton.icon(
                      onPressed: _apply,
                      icon: const Icon(Icons.check),
                      label: const Text('Aplicar filtros'),
                    ),
                    OutlinedButton(onPressed: _clear, child: const Text('Limpar')),
                  ],
                ),
              ],
            ),
          ),
        ),
      );
}
