using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwitchIRC.Client;
using TwitchIRC.Common;
using TwitchIRC.Interfaces;

namespace TwitchIRC_POC
{
    class Program
    {
        static void Main(string[] args)
        {
            string username;
            string oAuth;
            string host;
            ITwitchIRCClient client;

            username = "";
            oAuth = "";
            host = "irc.chat.twitch.tv";

            client = new TwitchIRCClient(host, 6667, oAuth, username);

            //client.OnRawMessage += (object sender, RawMessageEventArgs e)=>{ Console.WriteLine(e.Content); };

            client.OnConnect += (object sender, ConnectEventArgs e) =>
            {
                if (e.Connected)
                {
                    Console.WriteLine("-----Connected-----");
                }else
                {
                    Console.WriteLine("-----Disconnected-----");
                }
            };

            client.OnMessage += (object sender, MessageEventArgs e) =>
            {
                var m = e.Message;
                if (m.Type == MessageType.ChatMessage)
                {
                    string displayName = "";
                    if (m.Tags.ContainsKey("display-name"))
                    {
                        displayName = m.Tags["display-name"];
                    }
                    else
                    {
                        Regex usernameparse = new Regex(@"[:](\S+)[!]");
                        var usernamematch = usernameparse.Match(m.Prefix);
                        if (usernamematch.Success)
                        {
                            displayName = usernamematch.Groups[1].Value;
                        }
                    }

                    string chatLine = "";
                    chatLine += displayName;
                    chatLine += ": ";
                    chatLine += m.Payload;

                    Console.WriteLine(chatLine);
                }
            };

            Console.WriteLine("Press CTRL+C To Close Connection.");
            client.Start().Wait();
            var channel = client.JoinChannel(username.ToLowerInvariant()).Result;
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => { cts.Cancel(); };
            channel.SendToChat("Omni bot was turned on").Wait();
            while (!cts.Token.IsCancellationRequested)
            {
                Task.Delay(500).Wait();
            }
            client.Stop().Wait();
            Console.WriteLine("Any key to exit...");
            Console.ReadKey();
        }
    }
}
