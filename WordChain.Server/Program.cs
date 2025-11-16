
using System.Net;
using WordChain.Server;

Console.Title = "WordChain Server ";
Console.WriteLine("=== WordChain TCP Server  ===");

var host = args.Length > 0 ? args[0] : "0.0.0.0";
var port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 5000;
var dictPath = args.Length > 2 ? args[2] : Path.Combine(AppContext.BaseDirectory, "dictionary.txt");

var server = new GameServer(IPAddress.Parse(host), port, dictPath);
server.Start();

Console.WriteLine($"Listening at {host}:{port}");
Console.WriteLine($"Dictionary: {dictPath}");
Console.WriteLine("Press ENTER to exit...");
Console.ReadLine();

server.Stop();
