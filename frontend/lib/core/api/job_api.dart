import 'package:dio/dio.dart';

class JobApi {
  JobApi({String? baseUrl})
    : dio = Dio(
        BaseOptions(
          baseUrl:
              baseUrl ??
              const String.fromEnvironment(
                'JOB_FINDER_API_URL',
                defaultValue: 'http://localhost:5000',
              ),
          connectTimeout: const Duration(seconds: 8),
          receiveTimeout: const Duration(seconds: 15),
          headers: {'Accept': 'application/json'},
        ),
      );

  final Dio dio;

  Future<Map<String, dynamic>> getMap(
    String path, {
    Map<String, dynamic>? query,
  }) async => Map<String, dynamic>.from(
    (await dio.get<dynamic>(path, queryParameters: query)).data as Map,
  );

  Future<List<dynamic>> getList(String path) async =>
      List<dynamic>.from((await dio.get<dynamic>(path)).data as List);

  Future<void> put(String path, Map<String, dynamic> body) async {
    await dio.put<void>(path, data: body);
  }
}
