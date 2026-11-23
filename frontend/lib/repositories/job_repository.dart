import '../core/api/job_api.dart';
import '../models/job.dart';

class JobRepository {
  JobRepository(this.api);
  final JobApi api;

  Future<Map<String, dynamic>> dashboard() => api.getMap('/api/dashboard');
  Future<JobPage> jobs([Map<String, dynamic> filters = const {}]) async =>
      JobPage.fromJson(await api.getMap('/api/jobs', query: filters));
  Future<List<Job>> favorites() async {
    final page = await api.getMap('/api/favorites');
    return (page['items'] as List<dynamic>? ?? [])
        .map((item) => Job.fromJson(Map<String, dynamic>.from(item as Map)))
        .toList();
  }
  Future<List<Map<String, dynamic>>> sources() async =>
      (await api.getList('/api/sources'))
          .map((item) => Map<String, dynamic>.from(item as Map))
          .toList();
  Future<Map<String, dynamic>> settings() => api.getMap('/api/settings');
  Future<Map<String, dynamic>> statistics() => api.getMap('/api/statistics');
  Future<NewsPage> news() async =>
      NewsPage.fromJson(await api.getMap('/api/news'));
  Future<void> recordNewsVisit() async {
    await api.dio.post<void>('/api/news/visit');
  }
  Future<void> markNewsViewed(Job job) =>
      api.put('/api/news/jobs/${job.id}/viewed', {});
  Future<void> favorite(Job job) =>
      api.put('/api/jobs/${job.id}/favorite', {'favorite': !job.favorite});
  Future<void> status(Job job, String value) =>
      api.put('/api/jobs/${job.id}/status', {'status': value});
  Future<void> hide(Job job) =>
      api.put('/api/jobs/${job.id}/hidden', {'hidden': !job.hidden});
  Future<String> open(String id) async {
    final response = await api.dio.post<dynamic>('/api/jobs/$id/open');
    return (response.data as Map<String, dynamic>)['url'] as String;
  }
  Future<void> setSource(String name, bool enabled) => api.put(
        '/api/sources/${Uri.encodeComponent(name)}/enabled',
        {'enabled': enabled},
      );
  Future<Map<String, dynamic>> saveSettings(
    Map<String, dynamic> settings,
  ) async =>
      Map<String, dynamic>.from(
        (await api.dio.put<dynamic>('/api/settings', data: settings)).data
            as Map,
      );
  Future<void> collect() => api.dio.post<void>('/api/collect');
}
