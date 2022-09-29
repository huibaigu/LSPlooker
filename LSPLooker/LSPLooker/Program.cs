using HTTPServerLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LSPLooker
{
    public class ExampleServer : HTTPServerLib.HttpServer
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public ExampleServer(string ipAddress, int port)
            : base(ipAddress, port)
        {

        }

        public override void OnPost(HttpRequest request, HttpResponse response)
        {
            //获取客户端传递的参数
            string data = request.Params == null ? "" : string.Join(";", request.Params.Select(x => x.Key + "=" + x.Value).ToArray());

            //设置返回信息
            string content = string.Format("这是通过Post方式返回的数据:{0}", data);

            //构造响应报文
            response.SetContent(content);
            response.Content_Encoding = "utf-8";
            response.StatusCode = "200";
            response.Content_Type = "text/html; charset=UTF-8";
            response.Headers["Server"] = "ExampleServer";

            //发送响应
            response.Send();
        }

        public override void OnGet(HttpRequest request, HttpResponse response)
        {
            string requestURL = request.URL;
            requestURL = requestURL.Replace("/", @"\").Replace("\\..", "").TrimStart('\\');
            string requestFile = Path.Combine(ServerRoot, requestURL); ;
            string extension = Path.GetExtension(requestFile);
            if (extension != "")
            {
                //从文件中返回HTTP响应
                if (extension == ".get")
                {
                    requestFile = requestFile.Replace(".get", "");
                    extension = Path.GetExtension(requestFile);
                    if (extension == ".mp4"|| extension == ".avi")
                    {
                        response = response.FromText(movieget(requestFile));
                        response.Content_Type = "text/html; charset=UTF-8";
                    }
                    else if (extension == ".jpg" || extension == ".png" || extension == ".gif")
                    {
                        response = response.FromText(picget(requestFile));
                        response.Content_Type = "text/html; charset=UTF-8";
                    }
                }
                else if (extension == ".mp4" || extension == ".avi")
                {
                    response.FromFile(request, requestFile);
                }
                else if (extension == ".jpg" || extension == ".png" || extension == ".gif")
                {
                    response.FromFile(request, requestFile);
                }
            }
            else
            {
                if (Directory.Exists(requestFile))
                {
                    response = response.FromText(direcget(requestFile, requestURL));
                    response.Content_Type = "text/html; charset=UTF-8";
                }
            }
            response.Send();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public override void OnDefault(HttpRequest request, HttpResponse response)
        {

        }
        private string ConvertPath(string[] urls,int pd)
        {
            int BIG = 0;
            string TM = string.Empty;
            string html = string.Empty;
            int length = ServerRoot.Length;
            foreach (var url in urls)
            {
                var s = url.StartsWith("..") ? url : url.Substring(length).TrimEnd('\\').TrimStart('\\');
                string requestFile = Path.Combine(ServerRoot, s.Replace("/", @"\").Replace("\\..", "").TrimStart('\\')); ;
                if (pd==1)
                {
                    FileInfo aa = new FileInfo(requestFile);
                    BIG = (int)aa.Length/1000;
                    TM = aa.LastWriteTime.ToString();
                    s=s + ".get";
                }
                else
                {
                    DirectoryInfo aa = new DirectoryInfo(requestFile);
                    TM = aa.LastWriteTime.ToString();
                }
                html += String.Format("<tr><td><span><a href=\"\\{0}\">{1}</a></span></td><td><span>{2}</span></td><td><span>{3}</span></td></tr>", s, s.Split('\\')[s.Split('\\').Length-1],(pd==1?BIG.ToString()+"KB":"NULL"),TM);
            }

            return html;
        }
        private string movieget(string requestFile)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("<!DOCTYPE html><html><head><meta charset=\"utf-8\"> <title>{0}</title></head><body><video width=\"320\" height=\"240\"  controls><source src=\"{0}\" type=\"video/mp4\"><source src=\"{0}\" type=\"video/ogg\"><source src=\"{0}\" type=\"video/webm\"></video></body></html>", requestFile.Split('\\')[requestFile.Split('\\').Length-1]));
            return builder.ToString();
        }
        private string picget(string requestFile)
        {
            StringBuilder builder = new StringBuilder();
            DirectoryInfo dd = new FileInfo(requestFile).Directory;
            builder.Append(string.Format("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{0}</title></head><body><h4>{1}</h4>", requestFile.Split('\\')[requestFile.Split('\\').Length - 2], requestFile.Split('\\')[requestFile.Split('\\').Length - 2]));
            var getFile = dd.GetFiles();
            try
            {
                getFile = getFile.OrderBy(s => int.Parse(Regex.Match(s.Name, @"\d+").Value)).ToArray();
            }
            catch(Exception)
            {
                getFile = dd.GetFiles();
            }
            foreach (var aa in getFile)
            {
                builder.Append(string.Format("<img src=\"{0}\" alt=\"ERROR\" style=\"width:100%\"><br>",aa.Name));
            }
            builder.Append(string.Format("</body></html>"));
            return builder.ToString();
        }
        private string direcget(string requestDirectory, string requestURL)
        {
            var folders = requestURL.Length > 1 ? new string[] { "../" } : new string[] { };
            folders = folders.Concat(Directory.GetDirectories(requestDirectory)).ToArray();
            var foldersList = ConvertPath(folders,0);

            //列举文件
            var files = Directory.GetFiles(requestDirectory);
            var filesList = ConvertPath(files,1);
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("<!DOCTYPE html><html><head><meta charset=\"utf-8\"> <title>{0}</title></head><body><table cellspacing=\"0\" width=\"800px\"><tbody><tr><td><p>索引 {1}</p></td></tr><tr><td><span>名称</span></td><td><span>大小</span></td><td><span>修改日期</span></td></tr>", requestURL,requestDirectory));
            builder.Append(string.Format("{0}{1}</tbody></table></body></html> ", foldersList, filesList));
            //构造HTML
            return builder.ToString();
        }
    }
    public class ConsoleLogger : ILogger
    {
        public void Log(object message)
        {
            Console.WriteLine(message);
        }
    }
    class Program
    {
        static bool BUG = false;
        static void Main(string[] args)
        {
            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces()
                .Where(a => a.SupportsMulticast)
                .Where(a => a.OperationalStatus == OperationalStatus.Up)
                .Where(a => a.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(a => a.GetIPProperties().GetIPv4Properties() != null)
                .Where(a => a.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork))
                .Where(a => a.GetIPProperties().UnicastAddresses.Any(ua => ua.IsDnsEligible))
            )
            {

                Console.WriteLine("Network Interface: {0}", netif.Name);
                IPInterfaceProperties properties = netif.GetIPProperties();
                foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    Console.WriteLine("\tUniCast: {0}", unicast.Address);
            }
            string ip = "127.0.0.1";
            int port = 8080;
            string RootDirectory = "C:\\"; 
            using (System.IO.StreamReader file = System.IO.File.OpenText("config.json"))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    if(!(bool)o["service"]["Enable"])return;
                    ip = (string)o["service"]["IPaddress"];
                    port = (int)o["service"]["Port"];
                    BUG = (bool)o["service"]["Debug"];
                    RootDirectory = (string)o["service"]["RootDirectory"];
                }
            }
            ExampleServer server = new ExampleServer(ip, port);
            server.SetRoot(RootDirectory);
            server.Logger = new ConsoleLogger();
            server.Start();
            return;
        }
    }
}
