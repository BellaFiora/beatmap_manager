#pragma warning disable CS0162

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.IO.Serialization;

namespace BellaFioraUtils
{
    public static class Utils
    {
        public static int JsonifyOsuFile(string osu_path, string json_path)
        {
            using (var stream = File.OpenRead(osu_path))
            using (var reader = new LineBufferedReader(stream))
            {
                var decoder = new LegacyBeatmapDecoder();
                var beatmap = decoder.Decode(reader);
                try
                {
                    string json = beatmap.Serialize();
                    File.WriteAllText(json_path, json);
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine("JsonifyOsuFile: Error serializing beatmap: " + e);
                    return 1;
                }
            }
        }
    }

    public class Program
    {
        public static HttpListener Listener = new HttpListener();
        public static string Url = "http://localhost:8080/";

        public static async Task HandleIncomingConnections()
        {
            HttpListenerContext ctx;
            HttpListenerRequest req;
            HttpListenerResponse resp;
            string query;
            bool running = true;
            while (running)
            {
                ctx = await Listener.GetContextAsync().ConfigureAwait(true);
                req = ctx.Request;
                resp = ctx.Response;
                using (StreamReader reader = new StreamReader(req.InputStream, req.ContentEncoding))
                {
                    query = reader.ReadToEnd();
                    string[] parts = query.Split(new char[] { ',' }, StringSplitOptions.TrimEntries);
                    if (parts.Length == 0)
                    {
                        Console.WriteLine("\nEmpty query");
                        continue;
                    }
                    Console.WriteLine("\nQuery: " + query);
                    if (!int.TryParse(parts[0], out int actionCode))
                    {
                        Console.WriteLine("Invalid actionCode");
                        continue;
                    }
                    int resultCode = 0;
                    Console.Write("Action: ");
                    switch (actionCode)
                    {
                        case 0:
                            running = false;
                            Console.WriteLine("Stop");
                            break;
                        case 1:
                            resultCode = Utils.JsonifyOsuFile(parts[1], parts[2]);
                            Console.WriteLine("JsonifyOsuFile(" + parts[1] + ", " + parts[2] + ") -> " + resultCode);
                            break;
                        default:
                            Console.WriteLine("Nothing");
                            break;
                    }
                    // resultCode is in range 0-255
                    byte[] data = Encoding.ASCII.GetBytes(resultCode.ToString());
                    resp.ContentType = "text/plain";
                    resp.ContentEncoding = Encoding.ASCII;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(true);
                    resp.OutputStream.Close();
                }
                resp.Close();
            }
        }

        public static void Main(string[] args)
        {
            Listener.Prefixes.Add(Url);
            Listener.Start();
            Console.WriteLine("Listening for queries on {0}", Url);
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
            Listener.Close();
        }
    }
}
