using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using FinologyQuest.Data;
using FinologyQuest.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wkhtmltopdf.NetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using FinologyQuest.MiddleWare;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Extensions;

namespace FinologyQuest
{
    public class Startup
    {
        private IConfiguration _config;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public Startup(IConfiguration config)
        {
            _config = config;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            //Configure MVC with Session based Tempdata instead of cookies
            services.AddControllersWithViews()
                    .AddSessionStateTempDataProvider();

            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "Finology.XSRF";
                options.FormFieldName = "Finology-XSRF";
                options.HeaderName = "XSRF-TOKEN";
            });

            services.AddDbContext<FinologyQuestContext>(option =>
            {
                option.UseSqlServer(_config.GetConnectionString("finConn"));
                //.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            });

            //services.AddHttpClient<IAuthService, AuthService>();
            services.AddTransient<IAuthService, AuthService>();

            // File Download Service using AWS S3 links only
            services.AddHttpClient<IDownloadService, DownloadService>();

            services.AddTransient<ILogsService, LogsService>();
            services.AddTransient<ILogsCheckService, LogsCheckService>();
            services.AddTransient<IPDFService, PDFService>();
            services.AddTransient<ISubscriptionCheck, SubscriptionCheck>();
            services.AddWkhtmltopdf("Rotativa");

            services.AddRouting(o => o.LowercaseUrls = true);
            
            //Configure Custom Data Protection for Shared Cookies
            services.AddDataProtection()
                    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(_config.GetSection("Security:KeyPath").Value))
                    .SetApplicationName("FinologyOne");

            //Configure Custom Cookie Authentication with Challenge, Google, Facebook
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(option =>
            {
                option.SlidingExpiration = true;
                //option.LoginPath = "/login";
                option.Cookie.Name = "Finology.SharedAuth";
                option.Cookie.Path = "/";
                option.Cookie.HttpOnly = true;
                option.Cookie.SameSite = SameSiteMode.Lax;
                option.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                option.Cookie.Domain = _config.GetSection("cookiedomain").Value;
                option.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (context) =>
                    {
                        context.HttpContext.Response.Redirect(_config.GetSection("URLs:OneURL").Value + "/login?ReturnUrl=" + context.Request.GetDisplayUrl());
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddSession(options =>
            {
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "Finology.Session";
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
                app.UseDeveloperExceptionPage();
            //}
            app.UseHttpsRedirection();
            app.UseHsts();
            app.UseStaticFiles();
            app.UseMiddleware<IPAddressVerification>();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();

                //area routing
                endpoints.MapAreaControllerRoute(
                    name: "Dashboard",
                    areaName: "App",
                    pattern: "/app",
                    defaults: new {controller = "Dashboard", action = "Index"}
                );

                //endpoints.MapAreaControllerRoute(
                //    name: "quizAnswer",
                //    areaName: "App",
                //    pattern: "/app/quiz/{QuizName}/answer",
                //    defaults: new { controller = "QuizController", action = "Answer" }
                //);

                //endpoints.MapAreaControllerRoute(
                //    name: "takeQuiz",
                //    areaName: "App",
                //    pattern: "App/Quiz/{QuizName}",
                //    defaults: new { controller = "Quiz", action = "TakeTest" }
                //);

                endpoints.MapAreaControllerRoute(
                    name: "feedbackForm",
                    areaName: "App",
                    pattern: "/app/feedback",
                    defaults: new { controller = "Feedback", action = "Index" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "userRacetrackDetails",
                    areaName: "App",
                    pattern: "/app/raceTrack/{RaceTrackName}",
                    defaults: new { controller = "RaceTrack", action = "RaceTrackDetails" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "userCourseDetails",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}",
                    defaults: new { controller = "Course", action = "CourseDetails" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "courseIndex",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/syllabus",
                    defaults: new { controller = "CourseSyllabus", action = "Index" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "userCertificationDetails",
                    areaName: "App",
                    pattern: "/app/certification/{CertificationName}",
                    defaults: new { controller = "Certification", action = "CertificationDetails" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "userCertificate",
                    areaName: "App",
                    pattern: "/app/certification/{CertificationName}/view",
                    defaults: new { controller = "Certification", action = "CertificateView" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "CoursesBookmark",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/bookmark",
                    defaults: new { controller = "BookmarkCourse", action = "Index" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "ReadCourse",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/{ChapterName}/read",
                    defaults: new { controller = "CourseRead", action = "Index"}
                );

                endpoints.MapAreaControllerRoute(
                    name: "CourseDownloads",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/download",
                    defaults: new { controller = "DownloadCourse", action = "Index" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "ChapterDownloads",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/{ChapterName}/download",
                    defaults: new { controller = "DownloadChapter", action = "Index" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "CourseDownloads",
                    areaName: "App",
                    pattern: "/app/download/{DownloadId}",
                    defaults: new { controller = "Download", action = "File" }
                );

                endpoints.MapAreaControllerRoute(
                    name: "CourseFlashcards",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/flashcard",
                    defaults: new { controller = "Flashcard", action = "Index" }
                );

                //endpoints.MapAreaControllerRoute(
                //    name: "ChapterAssessment",
                //    areaName: "App",
                //    pattern: "/App/Course/{CourseName}/{ChapterName}/Assessment",
                //    defaults: new { controller = "AssessmentChapter", action = "Index" }
                //);

                //endpoints.MapAreaControllerRoute(
                //    name: "ChapterAssessmentResult",
                //    areaName: "App",
                //    pattern: "/App/Course/{CourseName}/{ChapterName}/Assessment/Result",
                //    defaults: new { controller = "AssessmentChapter", action = "Result" }
                //);

                endpoints.MapAreaControllerRoute(
                    name: "ChapterAssessmentAnswer",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/{ChapterName}/assessment/answer",
                    defaults: new { controller = "AssessmentChapter", action = "Answer" }
                );

                //endpoints.MapAreaControllerRoute(
                //    name: "CourseAssessment",
                //    areaName: "App",
                //    pattern: "/app/course/{CourseName}/Assessment",
                //    defaults: new { controller = "AssessmentCourse", action = "Index"}
                //);

                //endpoints.MapAreaControllerRoute(
                //    name: "CourseAssessmentResult",
                //    areaName: "App",
                //    pattern: "/App/Course/{CourseName}/Assessment/Result",
                //    defaults: new { controller = "AssessmentCourse", action = "Result" }
                //);

                endpoints.MapAreaControllerRoute(
                    name: "CourseAssessmentAnswer",
                    areaName: "App",
                    pattern: "/app/course/{CourseName}/assessment/answer",
                    defaults: new { controller = "AssessmentCourse", action = "Answer" }
                );

                endpoints.MapControllerRoute(
                    name: "MyAreaIndex",
                    pattern: "{area=App}/{controller}/{action=Index}");

                //public routing
                endpoints.MapControllerRoute(
                        name: "CourseDetails",
                        pattern: "/courses/{CourseName}",
                        defaults: new { controller = "Courses", action = "CourseDetails" }
                    );

                endpoints.MapControllerRoute(
                        name: "CourseEnroll",
                        pattern: "/courses/{CourseName}/enroll",
                        defaults: new { controller = "Courses", action = "CourseEnroll" }
                    );

                endpoints.MapControllerRoute(
                    name: "RaceTrackDetails",
                    pattern: "/raceTracks/{RaceTrackUrl}",
                    defaults: new { controller = "RaceTracks", action = "RaceTrackDetails" }
                );

                endpoints.MapControllerRoute(
                    name: "CertificationDetails",
                    pattern: "/certifications/{CertificationName}",
                    defaults: new { controller = "Certifications", action = "CertificationDetails" }
                );
            });            
        }
    }
}
