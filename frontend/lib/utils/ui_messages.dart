import 'package:dio/dio.dart';
import 'package:material_ui/material_ui.dart';

void showAppMessage(BuildContext context, String message) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(content: Text(message), behavior: SnackBarBehavior.floating),
    );
}

String friendlyError(Object error) {
  if (error is DioException) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout) {
      return 'Não foi possível conectar à API. Verifique se o backend está em execução.';
    }
    return error.response?.statusCode == 404
        ? 'O recurso solicitado não foi encontrado.'
        : 'A API retornou um erro. Tente novamente.';
  }
  return error.toString().replaceFirst('Exception: ', '');
}
