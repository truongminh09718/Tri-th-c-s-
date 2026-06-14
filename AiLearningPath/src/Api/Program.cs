using System.Text;
using AiLearningPath.Api.Filters;
using AiLearningPath.Api.Middleware;
using AiLearningPath.Application.Auth;
using AiLearningPath.Application.Authorization;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.LearningDna;
using AiLearningPath.Application.Paths;
using AiLearningPath.Application.Profiles;
using AiLearningPath.Application.Progress;
using AiLearningPath.Application.Twin;
using AiLearningPath.Infrastructure.Auth;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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

// EF Core DbContext. Mặc định dùng SQL Server (production). Khi cấu hình
// "UseInMemoryDatabase": true (mặc định ở Development), dùng EF Core InMemory để
// chạy được end-to-end mà không cần SQL Server cài sẵn — phục vụ demo giao diện.
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("AiLearningPathDev");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
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

// --- Dịch vụ ngoài (External Services) ---
// Trừu tượng hóa Gemini (sinh nội dung) và ML Service (dự đoán). Khi chưa cấu hình endpoint/
// khóa API thật trong appsettings, đăng ký triển khai placeholder xác định để ứng dụng vẫn
// khởi động và chạy được luồng end-to-end mà không phụ thuộc mạng. Khi triển khai production,
// cấu hình section "Gemini"/"MlService" và thay bằng adapter HTTP gọi dịch vụ thật.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.Configure<MlServiceOptions>(builder.Configuration.GetSection(MlServiceOptions.SectionName));

// IContentGenerator (Gemini, R5.1/R7.1/R10): sinh câu hỏi đánh giá, nội dung lộ trình học
// và lộ trình kỹ năng nghề nghiệp.
builder.Services.AddScoped<IContentGenerator, PlaceholderContentGenerator>();

// IPredictionService (ML, R9): dự đoán xác suất đạt mục tiêu theo thời lượng học.
builder.Services.AddScoped<IPredictionService, PlaceholderPredictionService>();

// Auth Service (R1, R2): đăng ký, đăng nhập và sinh/xác minh JWT.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT authentication configured from appsettings ("Jwt" section)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

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

// Phân quyền theo chủ sở hữu (R2.4, R14.2, R14.3): 401 khi JWT không hợp lệ/hết hạn,
// 403 khi truy cập tài nguyên của tài khoản khác. Phải đặt sau Authentication/Authorization.
app.UseOwnershipAuthorization();

app.MapControllers();

app.Run();

// Exposed for integration testing via WebApplicationFactory
public partial class Program { }
