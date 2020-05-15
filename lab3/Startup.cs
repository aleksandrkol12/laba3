using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;
using System.Text;

namespace lab3
{
    public class Startup
    {
        List<Student> students;

        public Startup()
        {
            students = new List<Student>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });

                endpoints.MapGet("/students", async context =>
                {
                    await context.Response.WriteAsync(PrintStudents(students));
                });

                endpoints.MapPost("/students", async context =>
                {
                    await context.Response.WriteAsync(PostStudentAsync(context, students).Result);
                });

                endpoints.MapGet("/students/{id}", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(PrintStudent(students, context));
                });

                endpoints.MapDelete("/students/{id}", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(DeleteStudent(students, context));
                });

                endpoints.MapPut("/students/{id}", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(PutStudent(students, context).Result);
                });
            });
        }

        public async Task<string> PutStudent(List<Student> students, HttpContext context)
        {
            string res = "";

            string json = null;

            int id = Int32.Parse(context.Request.Path.ToString().Substring(context.Request.Path.ToString().LastIndexOf('/') + 1));

            int lastId = students.Count > 0 ? students[students.Count - 1].Id.Value : 0;

            if (id <= lastId && id > 0 && lastId != 0)
            {
                using (StreamReader rdr = new StreamReader(context.Request.Body))
                {
                    json = await rdr.ReadToEndAsync();
                }

                Student putStudent = JsonSerializer.Deserialize<Student>(json);

                if (putStudent.FirstName != null)
                {
                    students.Where(p => p.Id == id).First().FirstName = putStudent.FirstName;
                    students.Where(p => p.Id == id).First().UpdatedAt = DateTime.UtcNow;
                    res += "The 'FirstName' field has been edited\n";
                }

                if (putStudent.LastName != null)
                {
                    students.Where(p => p.Id == id).First().LastName = putStudent.LastName;
                    students.Where(p => p.Id == id).First().UpdatedAt = DateTime.UtcNow;
                    res += "The 'LastName' filed has been edited\n";
                }

                if (putStudent.Group != null)
                {
                    students.Where(p => p.Id == id).First().Group = putStudent.Group;
                    students.Where(p => p.Id == id).First().UpdatedAt = DateTime.UtcNow;
                    res += "The 'Group' field has been edited\n";
                }
            }
            else
                res = "error";

            return res;
        }

        public async Task<string> PostStudentAsync(HttpContext context, List<Student> students)
        {
            string json = "";

            using (StreamReader rdr = new StreamReader(context.Request.Body))
            {
                json = await rdr.ReadToEndAsync();
            }

            Student student = JsonSerializer.Deserialize<Student>(json);
            student.CreatedAt = DateTime.UtcNow;

            if (students.Count == 0)
                student.Id = 1;
            else
                student.Id = students[students.Count - 1].Id + 1;

            students.Add(student);

            return "ok";
        }

        public string PrintStudents(List<Student> students)
        {
            string res = "";

            foreach (Student student in students)
                res += JsonSerializer.Serialize<Student>(student, new JsonSerializerOptions
                {
                    IgnoreNullValues = false,
                    WriteIndented = true
                });

            return JsonPrint(res);
        }

        public string PrintStudent(List<Student> students, HttpContext context)
        {
            int id = Int32.Parse(context.Request.Path.ToString().Substring(context.Request.Path.ToString().LastIndexOf('/') + 1));

            string res = "";

            int lastId = students.Count > 0 ? students[students.Count - 1].Id.Value : 0;

            if (id <= lastId && id > 0 && lastId != 0)
            {
                Student student = students.Where(p => p.Id == id).First();
                res = JsonPrint(JsonSerializer.Serialize<Student>(student, new JsonSerializerOptions
                {
                    IgnoreNullValues = false,
                    WriteIndented = true
                }));
            }

            return res;
        }

        public string DeleteStudent(List<Student> students, HttpContext context)
        {
            int id = Int32.Parse(context.Request.Path.ToString().Substring(context.Request.Path.ToString().LastIndexOf('/') + 1));

            string res = null;

            int lastId = students[students.Count - 1].Id.Value;

            if (id <= lastId && id > 0)
            {
                students.Remove(students.Where(p => p.Id == id).First());
                res = "The student was removed";
            }
            else
                res = "The student is not find";

            return res;
        }

        public string JsonPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}