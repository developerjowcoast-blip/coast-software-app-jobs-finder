class Job {
  const Job({
    required this.id,
    required this.title,
    required this.company,
    required this.location,
    required this.workMode,
    required this.source,
    required this.url,
    required this.publishedAt,
    required this.score,
    required this.summary,
    required this.category,
    required this.status,
    required this.favorite,
    required this.hidden,
    this.salary,
    this.reasons = const [],
  });

  final String id;
  final String title;
  final String company;
  final String location;
  final String workMode;
  final String source;
  final String url;
  final String summary;
  final String category;
  final String status;
  final DateTime publishedAt;
  final int score;
  final bool favorite;
  final bool hidden;
  final String? salary;
  final List<String> reasons;

  factory Job.fromJson(Map<String, dynamic> json) => Job(
        id: '${json['id'] ?? ''}',
        title: '${json['title'] ?? 'Vaga sem título'}',
        company: '${json['company'] ?? 'Empresa não informada'}',
        location: '${json['location'] ?? 'Local não informado'}',
        workMode: '${json['workMode'] ?? 'Unspecified'}',
        source: '${json['source'] ?? 'Desconhecida'}',
        url: '${json['url'] ?? ''}',
        publishedAt:
            DateTime.tryParse('${json['publishedAt'] ?? ''}') ?? DateTime.now(),
        score: (json['compatibilityScore'] as num?)?.toInt() ?? 0,
        summary: '${json['summary'] ?? ''}',
        category: '${json['category'] ?? 'Outros'}',
        status: '${json['status'] ?? 'Unseen'}',
        favorite: json['isFavorite'] as bool? ?? false,
        hidden: json['isHidden'] as bool? ?? false,
        salary: json['salary'] as String?,
        reasons: (json['compatibilityReasons'] as List<dynamic>? ?? [])
            .map((item) => '$item')
            .toList(),
      );
}

class JobPage {
  const JobPage(this.items, this.total);
  final List<Job> items;
  final int total;

  factory JobPage.fromJson(Map<String, dynamic> json) => JobPage(
        (json['items'] as List<dynamic>? ?? [])
            .map((item) => Job.fromJson(Map<String, dynamic>.from(item as Map)))
            .toList(),
        (json['total'] as num?)?.toInt() ?? 0,
      );
}

class NewsJob {
  const NewsJob({
    required this.job,
    required this.isNewSinceLastVisit,
    required this.collectedLast24Hours,
    required this.scoreChanged,
    required this.previousScore,
    required this.scoreChangedAt,
    required this.isViewed,
    required this.viewedAt,
  });

  final Job job;
  final bool isNewSinceLastVisit;
  final bool collectedLast24Hours;
  final bool scoreChanged;
  final int? previousScore;
  final DateTime? scoreChangedAt;
  final bool isViewed;
  final DateTime? viewedAt;

  factory NewsJob.fromJson(Map<String, dynamic> json) => NewsJob(
        job: Job.fromJson(Map<String, dynamic>.from(json['job'] as Map)),
        isNewSinceLastVisit: json['isNewSinceLastVisit'] as bool? ?? false,
        collectedLast24Hours: json['collectedLast24Hours'] as bool? ?? false,
        scoreChanged: json['scoreChanged'] as bool? ?? false,
        previousScore: (json['previousScore'] as num?)?.toInt(),
        scoreChangedAt: DateTime.tryParse('${json['scoreChangedAt'] ?? ''}'),
        isViewed: json['isViewed'] as bool? ?? false,
        viewedAt: DateTime.tryParse('${json['viewedAt'] ?? ''}'),
      );
}

class NewsPage {
  const NewsPage({
    required this.items,
    required this.lastVisitAt,
    required this.currentVisitAt,
    required this.total,
  });

  final List<NewsJob> items;
  final DateTime? lastVisitAt;
  final DateTime currentVisitAt;
  final int total;

  factory NewsPage.fromJson(Map<String, dynamic> json) => NewsPage(
        items: (json['items'] as List<dynamic>? ?? [])
            .map(
              (item) =>
                  NewsJob.fromJson(Map<String, dynamic>.from(item as Map)),
            )
            .toList(),
        lastVisitAt: DateTime.tryParse('${json['lastVisitAt'] ?? ''}'),
        currentVisitAt:
            DateTime.tryParse('${json['currentVisitAt'] ?? ''}') ?? DateTime.now(),
        total: (json['total'] as num?)?.toInt() ?? 0,
      );
}
