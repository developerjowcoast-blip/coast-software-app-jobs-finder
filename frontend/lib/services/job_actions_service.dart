import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';

import '../models/job.dart';
import '../providers/app_providers.dart';
import '../repositories/job_repository.dart';

class JobActionsService {
  const JobActionsService(this.repository);
  final JobRepository repository;

  Future<void> toggleFavorite(Job job) => repository.favorite(job);
  Future<void> updateStatus(Job job, String status) =>
      repository.status(job, status);
  Future<void> hide(Job job) => repository.hide(job);
  Future<void> open(Job job) async {
    final url = await repository.open(job.id);
    final uri = Uri.tryParse(url);
    if (uri == null ||
        !await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      throw Exception('Não foi possível abrir o link da vaga.');
    }
  }
}

final jobActionsServiceProvider = Provider<JobActionsService>(
  (ref) => JobActionsService(ref.watch(jobRepositoryProvider)),
);
