using RazorEngine;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ExamTask
{
    internal class Http
    {
        private Thread _serverThread;
        private string _siteDirectory;

        private HttpListener _listener;   
        private int _port;
        List<Task> tasks = new List<Task>() ;
       
        public Http(string path, int port)

        {
            tasks=JsonSerializer.Deserialize<List<Task>>(File.ReadAllText("../../../tasks.json")) ;
            this.Initialize(path, port);

        }
        private void Initialize(string path, int port)

        {

            _siteDirectory = path;

            _port = port;

            _serverThread = new Thread(Listen);

            _serverThread.Start();

            Console.WriteLine($"Сервер запущен на порту: {port}");

            Console.WriteLine($"Файлы сайта лежат в папке: {path}");

        }
        public void Stop()

        {
            _serverThread.Abort();

            _listener.Stop();
        }
        private void Listen()

        {
            _listener = new HttpListener();

            _listener.Prefixes.Add("http://localhost:" + _port.ToString() + "/");

            _listener.Start();

            while (true)
            {

                try

                {

                    HttpListenerContext context = _listener.GetContext();



                    Process(context);

                }

                catch (Exception e)

                {

                    Console.WriteLine(e.Message);

                }

            }

        }
        private void Process(HttpListenerContext context)

        {

            string filename = context.Request.Url.AbsolutePath;

            Console.WriteLine(filename);

            filename = filename.Substring(1);

            filename = Path.Combine(_siteDirectory, filename);

            string content = "";

            string query = context.Request.Url.Query;

            if (filename.Contains("html"))

            {

                if (context.Request.HttpMethod == "POST" && filename.Contains("index"))
                {
                    StreamReader reader = new StreamReader(context.Request.InputStream);
                    string postansw = reader.ReadToEnd();
                    
                    string[] split = postansw.Split('&');
                    int posH = split[0].IndexOf("=");
                    int posA = split[1].IndexOf("=");
                    int posD = split[2].IndexOf("=");
                    string Header = split[0].Substring(posH + 1);
                    string Name = split[1].Substring(posA + 1);
                    string Description = split[2].Substring(posD + 1);
                    Task addTask = new Task(tasks.Count + 1, Header, Name, Description);
                    tasks.Add(addTask);
                    content = BuildHtml(filename, tasks);
                    File.WriteAllText("C:/Users/imank/Desktop/exam6/Exam6/tasks.json", JsonSerializer.Serialize(tasks));
                    tasks = JsonSerializer.Deserialize<List<Task>>(File.ReadAllText("../../../tasks.json"));
                    context.Response.Headers.Add(HttpResponseHeader.Location, "/index.html");

                }
                if (context.Request.HttpMethod != "POST" && filename.Contains("index") && context.Request.Url.Query=="")
                {
                   content= BuildHtml(filename, tasks);

                }

                if(context.Request.HttpMethod == "GET" && filename.Contains("task") && (!context.Request.Url.Query.Contains("done") || !context.Request.Url.Query.Contains("dlt")))
                { 
                    string query1 = context.Request.Url.Query;
                    
                    int  a= Convert.ToInt32(query.Substring(query.IndexOf("?") + 1));
                    content = BuildHtml(filename, tasks[a-1]);

                }
                if (context.Request.HttpMethod == "GET" && filename.Contains("index")&& context.Request.Url.Query.Contains("done") || context.Request.Url.Query.Contains("dlt"))
                {
                    string query1 = context.Request.Url.Query.Replace("?","");
                    
                    string b = query.Substring(query.IndexOf("=") +1);
                    int a = Convert.ToInt32(query1.Substring(0, query1.IndexOf("=")));
                    int oper = 0;
                    if (b == "dlt")
                    {
                        for (int i = 0; i < tasks.Count; i++)
                        {
                            if (tasks[i].Id == a)
                            {
                                oper = i;
                            }
                        }
                        tasks.Remove(tasks[oper]);
                    }
                    else
                    {
                        foreach (var item in tasks)
                        {
                            if (item.Id == a)
                            {
                                item.Status="done";
                            }
                        }
                    }
                    File.WriteAllText("C:/Users/imank/Desktop/exam6/Exam6/tasks.json", JsonSerializer.Serialize(tasks));
                    tasks = JsonSerializer.Deserialize<List<Task>>(File.ReadAllText("../../../tasks.json"));
                    context.Response.Headers.Add(HttpResponseHeader.Location, "/index.html");
                    content = BuildHtml(filename, tasks);
                }

            }

            else

            {  
                content = File.ReadAllText(filename);

            }

            if (File.Exists(filename))

            {

                try

                {

                    byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes(content);

                    Stream fileStream = new MemoryStream(htmlBytes);


                    context.Response.ContentType = GetContentType(filename);



                    context.Response.ContentLength64 = fileStream.Length;


                    byte[] buffer = new byte[16 * 1024];


                    int dataLength;


                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    do

                    {

                        dataLength = fileStream.Read(buffer, 0, buffer.Length);


                        context.Response.OutputStream.Write(buffer, 0, dataLength);

                    } while (dataLength > 0);


                    fileStream.Close();

                    context.Response.OutputStream.Flush();

                }

                catch (Exception e)

                {

                    Console.WriteLine(e.Message);

                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                }
            }

            else

            {

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            }


            context.Response.OutputStream.Close();

        }
        private string GetContentType(string filename)

        {

            var dictionary = new Dictionary<string, string> {

            {".css",  "text/css"},

            {".html", "text/html"},

            {".ico",  "image/x-icon"},

            {".js",   "application/x-javascript"},

            {".json", "application/json"},

            {".png",  "image/png"}

        }; string contentType = "";

            string fileExtension = Path.GetExtension(filename);

            dictionary.TryGetValue(fileExtension, out contentType);

            return contentType;

        }
        private string BuildHtml(string filename, object result)

        {

            string html = "";

            string layoutPath = Path.Combine(_siteDirectory, "layout.html");

            string filePath = Path.Combine(_siteDirectory, filename);

            var razorService = Engine.Razor; 


            if (!razorService.IsTemplateCached("layout", null)) 

                razorService.AddTemplate("layout", File.ReadAllText(layoutPath)); 

            if (!razorService.IsTemplateCached(filename, null))

            {

                razorService.AddTemplate(filename, File.ReadAllText(filePath));

                razorService.Compile(filename);

            }
            int isNull=1;
            if (result == null )
            {
                isNull = 0;
                result = new int[] {1,2,3,4,5};
            }
            
            html = razorService.Run(filename, null, new
            {
                Isnull = isNull,
                Result = result
            });

            return html;

        }
    }
}

