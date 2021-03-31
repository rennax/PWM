using System;
using SocketIOSharp.Server;
using SocketIOSharp.Server.Client;
using SocketIOSharp.Common;
using EngineIOSharp.Common.Enum;
using SocketIOSharp.Client;
using Newtonsoft.Json.Linq;


namespace PWM
{
    class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Starting server on localhost port 9001");
            SocketIOServer server = new SocketIOServer(new SocketIOServerOption(9001));

            server.OnConnection((SocketIOSocket socket) => {
                Console.WriteLine("Client connected!");

                socket.On("ping", (Data) => {
                    foreach (JToken Token in Data)
                    {
                        Console.Write(Token + " ");
                    }
                    socket.Emit("pong", Data);
                });

                socket.On("join_lobby", (JToken[] Data) => { 
                    //TODO
                });

                socket.On("create_lobby", (JToken[] Data) => { 
                    //TODO
                });

                socket.On("score_receive", (JToken[] Data) => {
                    //TODO
                });


                socket.On("select_level", (JToken[] Data) => {
                    //TODO
                    //
                    server.Emit("select_level", Data);
                });

                socket.On("start_game", (JToken[] Data) => {
                    //TODO
                    //Select lobby
                    //Create recurring task for transmission
                    //transmit start game after delay
                    server.Emit("start_game", Data);
                });

                socket.On(SocketIOEvent.ERROR, (JToken[] Data) => {
                    if (Data != null && Data.Length > 0 && Data[0] != null)
                    {
                        Console.WriteLine("Error : " + Data[0]);
                    }
                    else
                    {
                        Console.WriteLine("Unkown Error");
                    }
                });

                socket.On(SocketIOEvent.DISCONNECT, () =>
                {
                    Console.WriteLine("Client disconnected!");
                });

            });

            server.Start();


            Console.WriteLine("Press ESC to stop\n");
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
