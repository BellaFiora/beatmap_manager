// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BellaFioraUtils
{
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
                        Utils.PrintError("\nEmpty query");
                        continue;
                    }
                    Console.WriteLine("\nQuery: " + query);
                    if (!int.TryParse(parts[0], out int actionCode))
                    {
                        Utils.PrintError("Invalid actionCode");
                        continue;
                    }
                    int resultCode = 0;
                    Console.Write("Action: ");
                    switch (actionCode)
                    {
                        case 0:
                            Console.WriteLine("Stop");
                            running = false;
                            break;
                        case 1:
                            Console.WriteLine("JsonifyOsuFile(" + parts[1] + ", " + parts[2] + ")");
                            resultCode = Utils.JsonifyOsuFile(parts[1], parts[2]);
                            break;
                        default:
                            Console.WriteLine("Nothing");
                            break;
                    }
                    Console.WriteLine("Result code: " + resultCode);
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
            using (BellaFioraHost host = new BellaFioraHost())
            {
                try
                {
                    host.Start();
                    listenTask.GetAwaiter().GetResult();
                    Listener.Close();
                }
                finally
                {
                    host.Exit();
                }
            }
        }
    }
}
