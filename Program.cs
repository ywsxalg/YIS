// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Web;
using Microsoft.Win32;
using System.Text.Json;
try
{
    Console.WriteLine("正在启动 YIS 服务…");
    string rootPath = Environment.CurrentDirectory;
    Settings s = new();
    if(!File.Exists(rootPath + "/settings.json"))
    {
        Console.WriteLine("未找到配置文件。进行配置。");
    se1:;
        Console.WriteLine("侦听端口号：");
        int p = -1;
        if (int.TryParse(Console.ReadLine(), out p)) s.port = p;
        else goto se1;
    se2:;
        Console.WriteLine("要求 SSL 加密：");
        bool b = false;
        if (bool.TryParse(Console.ReadLine(), out b)) s.forceSsl = b;
        else goto se2;
    se3:;
        Console.WriteLine("调试模式：");
        if (bool.TryParse(Console.ReadLine(), out b)) s.debug = b;
        else goto se3;
        Console.WriteLine("异常处理 404 文件：");
        s.file404 = Console.ReadLine();
        File.WriteAllText(rootPath + "/settings.json", JsonSerializer.Serialize(s));
    }
    else s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(rootPath + "/settings.json"));
    if(!Directory.Exists(rootPath + "/html/"))
    {
        Console.WriteLine("目录不存在。已经自动创建。");
        Directory.CreateDirectory(rootPath + "/html/");
    }
    HttpListener h = new();
    h.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
    if (s.forceSsl) h.Prefixes.Add($"https://+:{s.port}/");
    else h.Prefixes.Add($"http://+:{s.port}/");
    h.Start();
    Console.WriteLine("服务已启动。");
    HttpListenerContext l;
    while (true)
    {
        Task<HttpListenerContext> t = h.GetContextAsync();
        await t;
        l = t.Result;
        HttpListenerRequest q = l.Request;
        HttpListenerResponse r = l.Response;
        string uri = q.RawUrl;
        if (uri == null || !File.Exists(rootPath + "/html" + HttpUtility.UrlDecode(uri)))
        {
            r.StatusCode = 404;
            if (File.Exists(rootPath + "/html/" + s.file404))
            {
                r.ContentType = GetMimeMapping(s.file404);
                Task.Run(() => Write(File.OpenRead(rootPath + "/html/" + s.file404), r.OutputStream));
            }
            else
            {
                r.ContentType = "text/plain";
                r.OutputStream.Close();
            }
            if(s.debug)
                Console.WriteLine(DateTime.Now.ToString() + "|接收：" + uri + "，" + q.RemoteEndPoint.ToString() + "，404。");
        }
        else
        {
            uri = HttpUtility.UrlDecode(uri);
            r.StatusCode = 200;
            r.ContentType = GetMimeMapping(uri);
            Task.Run(() => Write(File.OpenRead(rootPath + "/html" + uri), r.OutputStream));
            if (s.debug)
                Console.WriteLine(DateTime.Now.ToString() + "|接收：" + uri + "，" + q.RemoteEndPoint.ToString() + "，200。");
        }
    }
}
catch(Exception ex)
{
    Console.WriteLine($"发生了错误：“{ex.Message}”。");
}

void Write(Stream read, Stream write)
{
    try
    {
        using (read) read.CopyTo(write);
        write.Close();
    }
    catch
    {

    }
}

string GetMimeMapping(string fileName)
{
    string mimeType = "application/octet-stream";
    string ext = Path.GetExtension(fileName).ToLower();
    RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
    if (regKey != null && regKey.GetValue("Content Type") != null)
        mimeType = regKey.GetValue("Content Type").ToString();
    return mimeType;
}

public class Settings
{
    public bool forceSsl { get; set; }
    public bool debug { get; set; }
    public int port { get; set; }
    public string file404 { get; set; }
}