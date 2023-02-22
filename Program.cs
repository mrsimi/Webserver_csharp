﻿using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Webserver_csharp
{
    class Program
    {

        private static TcpListener myListener;
        private static int port = 5050;
        private static IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        private static string WebServerPath = @"WebServer";
        private static string serverEtag = Guid.NewGuid().ToString("N");

        static void Main(string[] args)
        {
            try
            {

                myListener = new TcpListener(localAddr, port);
                myListener.Start();
                Console.WriteLine($"Web Server Running on {localAddr.ToString()} on port {port}... Press ^C to Stop...");
                Thread th = new Thread(new ThreadStart(StartListen));
                th.Start();
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        private static void StartListen()
        {
            while (true)
            {
             
                TcpClient client = myListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                //read request 
                byte[] requestBytes = new byte[1024];
                int bytesRead = stream.Read(requestBytes, 0, requestBytes.Length);

                string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                var requestHeaders = ParseHeaders(request);

                string[] requestFirstLine = requestHeaders.requestType.Split(" ");
                string httpVersion = requestFirstLine.LastOrDefault();
                string contentType = requestHeaders.headers.GetValueOrDefault("Accept");
                string contentEncoding = requestHeaders.headers.GetValueOrDefault("Acept-Encoding");

                if (!request.StartsWith("GET"))
                {
                    SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0, ref stream);
                }
                else
                {
                    var requestedPath = requestFirstLine[1];
                    var fileContent = GetContent(requestedPath);
                    if(fileContent is not null)
                    {
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                        stream.Write(fileContent, 0, fileContent.Length);
                    }
                    else
                    {
                        SendHeaders(httpVersion, 404, "Page Not Found", contentType, contentEncoding, 0, ref stream);
                    }
                }

                client.Close();
            }
        }

        private static byte[] GetContent(string requestedPath)
        {
            if (requestedPath == "/") requestedPath = "default.html";
            string filePath = Path.Join(WebServerPath, requestedPath);

            if (!File.Exists(filePath)) return null;

            else
            {
                byte[] file = System.IO.File.ReadAllBytes(filePath);
                return file;
            }
        }

        private static void SendHeaders(string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding,
            int byteLength, ref NetworkStream networkStream)
        {
            string responseHeaderBuffer = "";

            responseHeaderBuffer = $"HTTP/1.1 {statusCode} {statusMsg}\r\n" +
                $"Connection: Keep-Alive\r\n" +
                $"Date: {DateTime.UtcNow.ToString()}\r\n" +
                $"Server: MacOs PC \r\n" +
                $"Etag: \"{serverEtag}\"\r\n" +
                $"Content-Encoding: {contentEncoding}\r\n" +
                "X-Content-Type-Options: nosniff"+
                $"Content-Type: application/signed-exchange;v=b3\r\n\r\n";

            byte[] responseBytes = Encoding.UTF8.GetBytes(responseHeaderBuffer);
            networkStream.Write(responseBytes, 0, responseBytes.Length);
        }

      

        private static (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();
            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }
            return (headerValues, firstLine);
        }
    }
}