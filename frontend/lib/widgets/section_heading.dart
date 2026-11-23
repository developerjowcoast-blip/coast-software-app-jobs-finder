import 'package:material_ui/material_ui.dart';

class SectionHeading extends StatelessWidget {
  const SectionHeading({required this.title, this.action, super.key});
  final String title;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Text(
            title,
            style: Theme.of(context)
                .textTheme
                .titleLarge
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const Spacer(),
          ?action,
        ],
      );
}
