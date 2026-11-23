import 'package:material_ui/material_ui.dart';

String readableValue(String value) => switch (value) {
      'OnSite' => 'Presencial',
      'Remote' => 'Remoto',
      'Hybrid' => 'Híbrido',
      'Unseen' => 'Não visto',
      'Interesting' => 'Interessante',
      'Favorited' => 'Favoritada',
      'Applied' => 'Candidatado',
      'InReview' => 'Em análise',
      'Interview' => 'Entrevista',
      'Offer' => 'Oferta',
      'Rejected' => 'Recusada',
      'Closed' => 'Encerrada',
      _ => value,
    };

String formatPublishedDate(DateTime date) {
  final days = DateTime.now().difference(date).inDays;
  if (days <= 0) return 'Hoje';
  if (days == 1) return 'Ontem';
  if (days < 7) return 'Há $days dias';
  return '${date.day.toString().padLeft(2, '0')}/'
      '${date.month.toString().padLeft(2, '0')}/${date.year}';
}

String formatVisit(DateTime value) {
  final local = value.toLocal();
  return '${local.day.toString().padLeft(2, '0')}/'
      '${local.month.toString().padLeft(2, '0')} às '
      '${local.hour.toString().padLeft(2, '0')}:'
      '${local.minute.toString().padLeft(2, '0')}';
}

Color scoreColor(int score) => score >= 85
    ? const Color(0xFF16835D)
    : score >= 65
        ? const Color(0xFFBE8617)
        : const Color(0xFF667085);
