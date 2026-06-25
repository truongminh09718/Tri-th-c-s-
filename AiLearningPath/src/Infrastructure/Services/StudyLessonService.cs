using AiLearningPath.Application.Progress;
using AiLearningPath.Application.Study;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

public sealed class StudyLessonService : IStudyLessonService
{
    private readonly AppDbContext _db;
    private readonly IProgressDashboardService _progress;

    public StudyLessonService(AppDbContext db, IProgressDashboardService progress)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
    }

    public async Task<TodayLessonView> GetTodayAsync(
        Guid userId,
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var hasAssessment = await _db.AssessmentResults.AsNoTracking()
            .AnyAsync(r => r.UserId == userId, cancellationToken);

        if (!hasAssessment)
        {
            return new TodayLessonView(
                TodayLessonStatuses.RequiresAssessment,
                "Hoàn thành bài đánh giá năng lực để mở bài học cá nhân hóa.",
                null);
        }

        var path = await _db.LearningPaths.AsNoTracking()
            .Include(p => p.Phases)
            .ThenInclude(p => p.Tasks)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (path is null)
        {
            return new TodayLessonView(
                TodayLessonStatuses.RequiresPath,
                "Bạn đã sẵn sàng. Hãy sinh lộ trình để hệ thống chọn bài học hôm nay.",
                null);
        }

        var ordered = path.Phases
            .OrderBy(p => p.MonthIndex)
            .ThenBy(p => p.WeekIndex)
            .SelectMany(p => p.Tasks.Select(t => new { Phase = p, Task = t }))
            .Where(x => !x.Task.Completed)
            .ToList();

        if (ordered.Count == 0)
        {
            return new TodayLessonView(
                TodayLessonStatuses.AllCompleted,
                "Bạn đã hoàn thành tất cả nhiệm vụ trong lộ trình hiện tại.",
                null);
        }

        var current = taskId.HasValue
            ? ordered.FirstOrDefault(x => x.Task.Id == taskId.Value)
            : ordered[0];
        if (current is null)
        {
            throw new StudyLessonTaskNotFoundException(taskId!.Value);
        }

        var currentContent = BuildLessonContent(path.LearningGoal, current.Task.Skill);
        var tasks = ordered.Take(5)
            .Select((x, index) => new LessonTaskItem(
                x.Task.Id,
                x.Task.Skill,
                BuildLessonContent(path.LearningGoal, x.Task.Skill).Description,
                x.Task.Id == current.Task.Id))
            .ToList();

        return new TodayLessonView(
            TodayLessonStatuses.Ready,
            "Bài học hôm nay đã sẵn sàng.",
            new LessonDetail(
                current.Task.Id,
                path.LearningGoal,
                current.Phase.Title,
                current.Task.Skill,
                currentContent.Description,
                current.Task.EstimatedHours,
                tasks,
                currentContent.Materials,
                currentContent.Quizzes));
    }

    public async Task<CompleteLessonResult> CompleteAsync(
        Guid userId,
        Guid taskId,
        CompleteLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var taskExists = await _db.LearningTasks.AsNoTracking()
            .AnyAsync(t => t.Id == taskId && t.PathPhase!.LearningPath!.UserId == userId, cancellationToken);
        if (!taskExists)
        {
            throw new StudyLessonTaskNotFoundException(taskId);
        }

        _db.StudySessions.Add(new StudySession
        {
            UserId = userId,
            StartedAt = DateTime.UtcNow.AddMinutes(-request.DurationMinutes),
            DurationHours = request.DurationMinutes / 60d
        });
        await _db.SaveChangesAsync(cancellationToken);

        var dashboard = await _progress.CompleteTaskAsync(userId, taskId, cancellationToken);
        return new CompleteLessonResult(
            taskId,
            request.DurationMinutes,
            request.Rating,
            request.Reflection?.Trim(),
            request.QuizCorrect,
            dashboard.CompletionRate,
            dashboard.LearningScore,
            "Đã hoàn thành bài học và cập nhật tiến độ.");
    }

    private static void Validate(CompleteLessonRequest request)
    {
        if (request.DurationMinutes is < 1 or > 240)
            throw new StudyLessonValidationException("Thời lượng học phải từ 1 đến 240 phút.");
        if (request.Rating is < 1 or > 5)
            throw new StudyLessonValidationException("Vui lòng đánh giá buổi học từ 1 đến 5 sao.");
        if (request.Reflection?.Length > 500)
            throw new StudyLessonValidationException("Nhận xét cuối buổi không được vượt quá 500 ký tự.");
    }

    private static LessonContent BuildLessonContent(string learningGoal, string skill)
    {
        LessonContent content;
        if (learningGoal.Equals("IELTS", StringComparison.OrdinalIgnoreCase))
        {
            content = skill.ToLowerInvariant() switch
            {
                "listening" => new LessonContent(
                    "Luyện nghe ý chính và thông tin chi tiết trong hội thoại IELTS Section 1; nhận biết từ nối và các cách diễn đạt lại.",
                    [
                        new("Bài đọc", "Chiến lược nghe ba bước", "Trước khi nghe, đọc câu hỏi và gạch chân từ khóa. Khi nghe, chú ý từ đồng nghĩa. Sau khi nghe, kiểm tra chính tả và giới hạn từ."),
                        new("Ghi chú", "Tín hiệu chuyển ý", "Các cụm 'moving on', 'however', 'the main point is' báo hiệu người nói chuyển ý hoặc nhấn mạnh thông tin quan trọng."),
                        new("Thực hành", "Bài tập dictation 5 phút", "Nghe một đoạn ngắn hai lần, ghi lại nguyên văn, sau đó so sánh và ghi ra ba lỗi nghe sai."),
                    ],
                    [
                        new LessonQuiz("Cụm 'Let's move on to the next point' báo hiệu điều gì?",
                            ["Người nói kết thúc bài", "Người nói chuyển sang ý mới", "Người nói phủ nhận ý trước"], 1,
                            "'Move on' là tín hiệu chuyển tiếp sang một chủ đề hoặc luận điểm mới."),
                        new LessonQuiz("Trước khi audio bắt đầu, bạn nên làm gì trước tiên?",
                            ["Dịch toàn bộ câu hỏi", "Gạch chân từ khóa và đoán loại từ", "Chọn sẵn một đáp án"], 1,
                            "Từ khóa và loại từ cần điền giúp bạn biết phải nghe thông tin nào."),
                        new LessonQuiz("Trong IELTS Listening, 'fifteen pounds fifty' tương ứng với giá nào?",
                            ["£15.05", "£15.50", "£50.15"], 1,
                            "'Fifteen pounds fifty' có nghĩa là 15 bảng và 50 xu: £15.50."),
                    ]),

                "grammar" => new LessonContent(
                    "Ôn thì quá khứ hoàn thành và mệnh đề thời gian để mô tả chính xác thứ tự các sự kiện.",
                    [
                        new("Bài đọc", "Past Perfect: had + V3", "Dùng Past Perfect cho hành động xảy ra trước một mốc quá khứ khác. Ví dụ: By the time we arrived, the film had started."),
                        new("Ghi chú", "Dấu hiệu thường gặp", "Chú ý các cụm by the time, before, after, already và never khi xác định thứ tự sự kiện."),
                        new("Thực hành", "Viết 5 câu theo dòng thời gian", "Tạo năm cặp sự kiện trong quá khứ và dùng Past Perfect cho sự kiện xảy ra trước."),
                    ],
                    [
                        new LessonQuiz("Chọn câu đúng: By the time we arrived, the film _____.",
                            ["has started", "had started", "starts"], 1,
                            "Bộ phim bắt đầu trước thời điểm chúng ta đến, vì vậy dùng Past Perfect: had started."),
                        new LessonQuiz("Chọn dạng đúng: She has lived here _____ 2010.",
                            ["for", "since", "during"], 1,
                            "Since đi với một mốc thời gian cụ thể; for đi với một khoảng thời gian."),
                        new LessonQuiz("If it _____ tomorrow, we will cancel the trip.",
                            ["rains", "rained", "will rain"], 0,
                            "Câu điều kiện loại 1 dùng hiện tại đơn trong mệnh đề if: if it rains."),
                    ]),

                "vocabulary" => new LessonContent(
                    "Học và vận dụng nhóm từ vựng học thuật dùng để mô tả xu hướng, nguyên nhân và kết quả.",
                    [
                        new("Từ vựng", "12 từ học thuật trọng tâm", "significant, substantial, demonstrate, indicate, decline, increase, fluctuate, consequence, factor, approach, evidence, proportion."),
                        new("Ghi chú", "Học theo collocation", "Ghi nhớ theo cụm: significant increase, substantial evidence, contributing factor, practical approach thay vì học từ rời."),
                        new("Thực hành", "Tạo thẻ từ có ngữ cảnh", "Với mỗi từ, viết một câu liên quan đến chủ đề giáo dục hoặc công nghệ và đọc lại thành tiếng."),
                    ],
                    [
                        new LessonQuiz("Từ nào gần nghĩa nhất với 'significant'?",
                            ["trivial", "important", "optional"], 1,
                            "Significant mang nghĩa quan trọng hoặc đáng kể; từ gần nghĩa là important."),
                        new LessonQuiz("Collocation nào tự nhiên nhất trong văn học thuật?",
                            ["substantial evidence", "heavy evidence", "big evidence"], 0,
                            "Substantial evidence là collocation học thuật phổ biến, nghĩa là bằng chứng đáng kể."),
                        new LessonQuiz("Từ nào có thể thay thế 'show' trong câu 'the data show a trend'?",
                            ["hide", "demonstrate", "guess"], 1,
                            "Demonstrate là động từ học thuật phù hợp để trình bày điều dữ liệu cho thấy."),
                    ]),

                "writing" => new LessonContent(
                    "Viết mở bài IELTS Writing Task 2 gồm câu paraphrase đề bài và thesis statement rõ ràng.",
                    [
                        new("Bài đọc", "Cấu trúc mở bài hai câu", "Câu 1 paraphrase chủ đề bằng từ đồng nghĩa. Câu 2 nêu lập trường và hai luận điểm chính sẽ triển khai."),
                        new("Ghi chú", "Tránh chép nguyên đề", "Thay đổi cả từ vựng lẫn cấu trúc câu, nhưng không làm thay đổi nghĩa gốc của đề bài."),
                        new("Thực hành", "Viết mở bài trong 8 phút", "Chọn chủ đề online learning, viết hai câu mở bài, sau đó kiểm tra lập trường có trả lời đúng câu hỏi hay không."),
                    ],
                    [
                        new LessonQuiz("Thành phần quan trọng nhất để thể hiện lập trường trong mở bài là gì?",
                            ["Thesis statement", "Danh mục tài liệu", "Câu kết luận"], 0,
                            "Thesis statement trả lời trực tiếp đề bài và cho người đọc biết lập trường của bài viết."),
                        new LessonQuiz("Cách paraphrase nào an toàn nhất?",
                            ["Thay từ đồng nghĩa và đổi cấu trúc nhưng giữ nguyên nghĩa", "Chép nguyên đề bài", "Thêm một ý kiến không có trong đề"], 0,
                            "Paraphrase tốt thay đổi cách diễn đạt nhưng không làm sai lệch nghĩa ban đầu."),
                        new LessonQuiz("IELTS Writing Task 2 thường yêu cầu người viết làm gì?",
                            ["Mô tả biểu đồ", "Trình bày và phát triển lập luận", "Viết thư thân mật"], 1,
                            "Task 2 đánh giá khả năng trình bày quan điểm và phát triển lập luận có dẫn chứng."),
                    ]),

                "reading" => new LessonContent(
                    "Luyện skimming để xác định ý chính và scanning để tìm nhanh tên riêng, con số, mốc thời gian trong bài IELTS Reading.",
                    [
                        new("Bài đọc", "Phân biệt Skimming và Scanning", "Skimming tập trung vào tiêu đề, câu chủ đề và từ lặp lại. Scanning di chuyển mắt nhanh để tìm một thông tin cụ thể."),
                        new("Ghi chú", "Nhận diện paraphrase", "Câu hỏi hiếm khi lặp nguyên văn bài đọc. Hãy ghép cặp: increase = rise, important = significant, cause = lead to."),
                        new("Thực hành", "Thử thách 6 phút", "Dành 2 phút skimming một bài 500 từ, ghi ý chính mỗi đoạn; dùng 4 phút còn lại để tìm năm thông tin cụ thể."),
                    ],
                    [
                        new LessonQuiz("Scanning được dùng chủ yếu để làm gì?",
                            ["Ghi nhớ toàn bộ bài", "Tìm thông tin cụ thể", "Phân tích phong cách tác giả"], 1,
                            "Scanning là kỹ thuật quét nhanh để tìm chi tiết cụ thể như ngày tháng, tên hoặc con số."),
                        new LessonQuiz("Skimming giúp người đọc làm gì?",
                            ["Hiểu nhanh ý chính", "Dịch từng câu", "Ghi nhớ mọi chi tiết"], 0,
                            "Skimming là đọc nhanh tiêu đề, câu chủ đề và từ khóa để nắm ý chính."),
                        new LessonQuiz("Trong bài đọc, 'a significant rise' có thể paraphrase thành cụm nào?",
                            ["a minor fall", "an important increase", "a stable level"], 1,
                            "Significant tương ứng important, còn rise tương ứng increase."),
                    ]),

                _ => BuildGenericContent(learningGoal, skill),
            };
        }
        else
        {
            content = BuildGenericContent(learningGoal, skill);
        }

        return content with { Quizzes = ExpandQuizzes(learningGoal, skill, content.Quizzes, 15) };
    }

    private static IReadOnlyList<LessonQuiz> ExpandQuizzes(
        string learningGoal,
        string skill,
        IReadOnlyList<LessonQuiz> curated,
        int count)
    {
        var result = curated.ToList();
        var knownPrompts = new HashSet<string>(
            result.Select(q => q.Question),
            StringComparer.OrdinalIgnoreCase);

        var bankQuestions = AssessmentQuestionBank.GetQuestions(learningGoal, 200)
            .Where(q => q.SkillArea.Equals(skill, StringComparison.OrdinalIgnoreCase));
        foreach (var question in bankQuestions)
        {
            if (!knownPrompts.Add(question.Prompt))
            {
                continue;
            }

            var correctOption = question.Options
                .Select((option, index) => new { option, index })
                .FirstOrDefault(x => x.option == question.CorrectOption)?.index ?? 0;
            result.Add(new LessonQuiz(
                question.Prompt,
                question.Options,
                correctOption,
                $"Đáp án đúng là “{question.CorrectOption}”. Hãy đối chiếu với kiến thức {skill} trong tài liệu phía trên."));
            if (result.Count == count)
            {
                return result;
            }
        }

        // Một số kỹ năng trong ngân hàng đánh giá có ít hơn 15 mẫu. Lặp lại theo vòng
        // dưới dạng câu luyện tập bổ sung để phiên học vẫn luôn đủ số lượng yêu cầu.
        var source = result.ToList();
        for (var index = 0; result.Count < count; index++)
        {
            var original = source[index % source.Count];
            result.Add(original with
            {
                Question = $"Luyện tập bổ sung {result.Count + 1}: {original.Question}"
            });
        }

        return result.Take(count).ToList();
    }

    private static LessonContent BuildGenericContent(string learningGoal, string skill) => new(
        $"Nắm kiến thức trọng tâm và hoàn thành một bài thực hành về {skill} trong lộ trình {learningGoal}.",
        [
            new("Bài đọc", $"Kiến thức nền tảng: {skill}", "Đọc khái niệm chính, ghi lại ba ý quan trọng và một phần còn chưa rõ."),
            new("Ghi chú", "Quy trình thực hành", "Chia bài thành các bước nhỏ, thực hiện từng bước và kiểm tra kết quả ngay sau đó."),
            new("Thực hành", "Tự tóm tắt cuối bài", "Giải thích lại nội dung bằng lời của bạn và ghi một ví dụ có thể áp dụng."),
        ],
        [
            new LessonQuiz($"Cách nào giúp ghi nhớ {skill} bền vững nhất?",
                ["Thực hành và tự giải thích", "Chỉ đọc lướt qua", "Bỏ qua phần chưa hiểu"], 0,
                "Thực hành kết hợp tự giải thích buộc người học chủ động truy xuất và kết nối kiến thức."),
            new LessonQuiz("Sau khi thực hành, bạn nên làm gì?",
                ["Kiểm tra kết quả và ghi lỗi sai", "Bỏ qua kết quả", "Chuyển ngay sang chủ đề khác"], 0,
                "Phản hồi ngay sau thực hành giúp nhận ra lỗ hổng và củng cố cách làm đúng."),
            new LessonQuiz("Cách tóm tắt nào hiệu quả nhất?",
                ["Dùng lời của bạn và nêu một ví dụ", "Chép nguyên tài liệu", "Chỉ ghi tiêu đề"], 0,
                "Tự diễn đạt và tạo ví dụ cho thấy bạn đã hiểu và có thể vận dụng kiến thức."),
        ]);

    private sealed record LessonContent(
        string Description,
        IReadOnlyList<LessonMaterial> Materials,
        IReadOnlyList<LessonQuiz> Quizzes);
}
