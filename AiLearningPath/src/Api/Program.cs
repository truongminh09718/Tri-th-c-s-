using System.Text;
using AiLearningPath.Api.Filters;
using AiLearningPath.Api.Middleware;
using AiLearningPath.Application.Ai;
using AiLearningPath.Application.Adaptive;
using AiLearningPath.Application.Auth;
using AiLearningPath.Application.Authorization;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.LearningDna;
using AiLearningPath.Application.Paths;
using AiLearningPath.Application.Profiles;
using AiLearningPath.Application.Progress;
using AiLearningPath.Application.Scheduling;
using AiLearningPath.Application.Study;
using AiLearningPath.Application.Twin;
using AiLearningPath.Infrastructure.Auth;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Dependency Injection container ---

// MVC Controllers + bộ lọc ánh xạ ngoại lệ nghiệp vụ sang phản hồi lỗi nhất quán.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ProfileExceptionFilter>();
    options.Filters.Add<LearningDnaExceptionFilter>();
    options.Filters.Add<AssessmentExceptionFilter>();
    options.Filters.Add<PathExceptionFilter>();
    options.Filters.Add<TwinExceptionFilter>();
    options.Filters.Add<CareerExceptionFilter>();
    options.Filters.Add<DashboardExceptionFilter>();
});

// EF Core DbContext. Lựa chọn nhà cung cấp theo cấu hình:
// - "UseInMemoryDatabase": true  → EF Core InMemory (dữ liệu chỉ tồn tại trong RAM, mất khi
//   restart). Chỉ phù hợp test nhanh, KHÔNG dùng cho demo cần giữ tài khoản đã đăng ký.
// - Có "ConnectionStrings:DefaultConnection" dạng SQL Server → dùng SQL Server (production).
// - Mặc định (Development) → SQLite lưu ra file app.db để tài khoản và dữ liệu học tập tồn
//   tại qua các lần khởi động lại, không cần cài SQL Server.
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var useSqlServer = builder.Configuration.GetValue<bool>("UseSqlServer");
var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection")
    ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("AiLearningPathDev");
    }
    else if (useSqlServer)
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        options.UseSqlite(sqliteConnection);
    }
});

// Profile Service (R3, R4).
builder.Services.AddScoped<IProfileService, ProfileService>();

// Learning DNA Engine (R6): xây dựng, cập nhật và truy xuất Learning DNA Profile.
builder.Services.AddScoped<ILearningDnaEngine, LearningDnaEngine>();

// Assessment Engine (R5): sinh bộ câu hỏi qua Gemini, chấm bài và lưu kết quả.
builder.Services.AddScoped<IAssessmentEngine, AssessmentEngine>();

// Path Generator (R7): kiểm tra tiên quyết, sinh nội dung lộ trình qua Gemini, xây
// cấu trúc tháng → tuần → ngày, ước lượng tính khả thi và lưu lộ trình.
builder.Services.AddScoped<IPathGenerator, PathGenerator>();

// Academic Twin Service (R9): mô phỏng và dự đoán xác suất đạt mục tiêu qua ML Service.
builder.Services.AddScoped<IAcademicTwinService, AcademicTwinService>();

// Career Path AI Service (R10): danh mục nghề nghiệp, sinh lộ trình kỹ năng kèm đề xuất
// chứng chỉ/dự án qua Gemini và lưu liên kết với hồ sơ sinh viên.
builder.Services.AddScoped<ICareerPathService, CareerPathService>();

// Progress Dashboard Service (R8): tổng hợp Learning Score, tỷ lệ hoàn thành, tổng giờ
// học và dữ liệu biểu đồ; đánh dấu nhiệm vụ hoàn thành và xử lý trạng thái rỗng.
builder.Services.AddScoped<IProgressDashboardService, ProgressDashboardService>();
builder.Services.AddScoped<IAiTutorService, AiTutorService>();
builder.Services.AddScoped<IAiInsightService, AiInsightService>();
builder.Services.AddScoped<IAiFeedbackService, AiFeedbackService>();
builder.Services.AddScoped<IStudyScheduleService, StudyScheduleService>();
builder.Services.AddScoped<IAdaptiveLearningService, AdaptiveLearningService>();
builder.Services.AddScoped<IStudyLessonService, StudyLessonService>();

// --- Dịch vụ ngoài (External Services) ---
// Trừu tượng hóa Gemini (sinh nội dung) và ML Service (dự đoán). Khi chưa cấu hình endpoint/
// khóa API thật trong appsettings, đăng ký triển khai placeholder xác định để ứng dụng vẫn
// khởi động và chạy được luồng end-to-end mà không phụ thuộc mạng. Khi triển khai production,
// cấu hình section "Gemini"/"MlService" và thay bằng adapter HTTP gọi dịch vụ thật.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.Configure<MlServiceOptions>(builder.Configuration.GetSection(MlServiceOptions.SectionName));
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddHttpClient();

// IContentGenerator (Gemini, R5.1/R7.1/R10/R17): sinh câu hỏi đánh giá, nội dung lộ trình học
// và lộ trình kỹ năng nghề nghiệp.
// Luôn đăng ký PlaceholderContentGenerator làm fallback xác định. Khi section "Gemini" đã
// cấu hình đầy đủ (ApiKey + Endpoint), bind IContentGenerator → ResilientContentGenerator bọc
// adapter Gemini thật với placeholder dự phòng; ngược lại giữ hành vi cũ dùng placeholder.
builder.Services.AddScoped<PlaceholderContentGenerator>();
builder.Services.AddHttpClient<GeminiContentGenerator>();
builder.Services.AddScoped<IAiGateway, AiGateway>();

var geminiOptions = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();
if (GeminiOptions.IsConfigured(geminiOptions))
{
    builder.Services.AddScoped<IContentGenerator, AiContentGeneratorAdapter>();
}
else
{
    builder.Services.AddScoped<IContentGenerator, AiContentGeneratorAdapter>();
}

// IPredictionService (ML, R9/R18): dự đoán xác suất đạt mục tiêu theo thời lượng học.
// Luôn đăng ký PlaceholderPredictionService làm fallback xác định. Khi section "MlService"
// đã cấu hình đầy đủ (Endpoint), bind IPredictionService → ResilientPredictionService bọc
// adapter ML thật với placeholder dự phòng; ngược lại giữ hành vi cũ dùng placeholder.
builder.Services.AddScoped<PlaceholderPredictionService>();

var mlOptions = builder.Configuration.GetSection(MlServiceOptions.SectionName).Get<MlServiceOptions>() ?? new MlServiceOptions();
if (MlServiceOptions.IsConfigured(mlOptions))
{
    builder.Services.AddHttpClient<MlPredictionService>();
    builder.Services.AddScoped<IPredictionService>(sp => new ResilientPredictionService(
        sp.GetRequiredService<MlPredictionService>(),
        sp.GetRequiredService<PlaceholderPredictionService>(),
        sp.GetRequiredService<ILogger<ResilientPredictionService>>()));
}
else
{
    builder.Services.AddScoped<IPredictionService, PlaceholderPredictionService>();
}

// Auth Service (R1, R2): đăng ký, đăng nhập và sinh/xác minh JWT.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT authentication configured from appsettings ("Jwt" section)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
ValidateProductionConfiguration(builder.Environment, jwtKey, geminiOptions, mlOptions);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 30;
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// Ownership-based authorization (R14): chỉ chủ sở hữu mới truy cập được tài nguyên của mình.
builder.Services.AddSingleton<IResourceAuthorizer, ResourceAuthorizer>();

// Swagger / OpenAPI with JWT bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Learning Path API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập JWT theo định dạng: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// Khởi tạo schema cơ sở dữ liệu. InMemory không có migration nên dùng EnsureCreated;
// SQLite/SQL Server dùng migration để database cũ được bổ sung bảng/cột mới mà không mất dữ liệu.
if (!useSqlServer)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useInMemory || builder.Environment.IsEnvironment("Testing"))
    {
        db.Database.EnsureCreated();
    }
    else
    {
        PrepareLegacySqliteMigrationHistory(db);
        db.Database.Migrate();
        EnsureSqliteSkillBreakdownColumn(db);
    }
}
else
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// --- HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Phục vụ giao diện web tĩnh (SPA một trang) từ wwwroot: "/" trả về index.html.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Phân quyền theo chủ sở hữu (R2.4, R14.2, R14.3): 401 khi JWT không hợp lệ/hết hạn,
// 403 khi truy cập tài nguyên của tài khoản khác. Phải đặt sau Authentication/Authorization.
app.UseOwnershipAuthorization();

app.MapControllers();

app.Run();

static void ValidateProductionConfiguration(
    IWebHostEnvironment environment,
    string jwtKey,
    GeminiOptions geminiOptions,
    MlServiceOptions mlOptions)
{
    if (!environment.IsProduction())
    {
        return;
    }

    if (jwtKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        Encoding.UTF8.GetByteCount(jwtKey) < 32)
    {
        throw new InvalidOperationException(
            "Production JWT key must be a non-placeholder secret of at least 256 bits.");
    }

    if (MlServiceOptions.IsConfigured(mlOptions) && string.IsNullOrWhiteSpace(mlOptions.ApiKey))
    {
        throw new InvalidOperationException(
            "Production ML service integration requires MlService:ApiKey for internal service authentication.");
    }

    if (GeminiOptions.IsConfigured(geminiOptions) &&
        geminiOptions.Endpoint!.Contains("key=", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Production Gemini endpoint must not include API keys in the URL; use Gemini:ApiKey instead.");
    }
}

static void PrepareLegacySqliteMigrationHistory(AppDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var check = connection.CreateCommand();
        check.CommandText = """
            SELECT
                EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Users'),
                EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory');
            """;
        using var reader = check.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(0) == 0 || reader.GetInt64(1) != 0)
        {
            return;
        }

        reader.Close();
        using var bootstrap = connection.CreateCommand();
        bootstrap.CommandText = """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260614180411_InitialCreate', '8.0.8');
            """;
        bootstrap.ExecuteNonQuery();
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static void EnsureSqliteSkillBreakdownColumn(AppDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var columns = connection.CreateCommand();
        columns.CommandText = "PRAGMA table_info('AssessmentResults');";
        using var reader = columns.ExecuteReader();
        var exists = false;
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "SkillBreakdownJson", StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        reader.Close();
        if (!exists)
        {
            using var addColumn = connection.CreateCommand();
            addColumn.CommandText =
                "ALTER TABLE \"AssessmentResults\" ADD \"SkillBreakdownJson\" TEXT NOT NULL DEFAULT '';";
            addColumn.ExecuteNonQuery();
        }
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

// Exposed for integration testing via WebApplicationFactory
public partial class Program { }
