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
    if (File.Exists(rootPath + "/settings.json"))
        s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(rootPath + "/settings.json"));
    List<string> banIps = s.banIps.ToList();
    File.WriteAllText(rootPath + "/settings.json", JsonSerializer.Serialize<Settings>(s));
    {
        Console.WriteLine("服务器当前配置：");
        Console.WriteLine($"_强制 SSL 加密：{s.forceSsl}；");
        Console.WriteLine($"_调试模式：{s.debug}；");
        Console.WriteLine($"_端口：{s.port}；");
        Console.WriteLine($"_404 返回：{s.file404}；");
        Console.WriteLine($"_403 返回：{s.file403}；");
        Console.WriteLine($"_拒绝访问地址：");
        if (s.banIps != null && s.banIps.Length > 0)
            foreach (string i in s.banIps)
                Console.WriteLine(i);
        Console.WriteLine("按任意键以继续。");
        Console.ReadKey();
    }
    if (!Directory.Exists(rootPath + "/html/"))
    {
        Directory.CreateDirectory(rootPath + "/html/");
        Console.WriteLine("目录不存在。已经自动创建。");
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
        if(q.HttpMethod.ToLower() == "get")
        {
            if (banIps.Contains(q.RemoteEndPoint.Address.ToString()))
            {
                r.StatusCode = 403;
                if (File.Exists(rootPath + "/html/" + s.file403))
                {
                    r.ContentType = GetMimeMapping(s.file403);
                    Task.Run(() => Write(File.OpenRead(rootPath + "/html/" + s.file403), r.OutputStream));
                }
                else
                {
                    r.ContentType = "text/plain";
                    r.OutputStream.Close();
                }
                if (s.debug)
                    Console.WriteLine(DateTime.Now.ToString() + "|接收：" + uri + "，" + q.RemoteEndPoint.ToString() + "，403。");
            }
            else if (uri == null || !File.Exists(rootPath + "/html" + HttpUtility.UrlDecode(uri)))
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
                if (s.debug)
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
}
catch (Exception ex)
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
    public string file404{ get; set; }
    public string file403 { get; set; }
    public string[] banIps { get; set; }

    public Settings()
    {
        forceSsl = false;
        debug = false;
        port = 80;
        file404 = "";
        file403 = "";
        banIps = new string[] { };
    }
}