using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quobject.SocketIoClientDotNet.Client;

namespace Sockettest
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = IO.Socket("http://arielsocketredis.azurewebsites.net");
            socket.On(Socket.EVENT_CONNECT, (data) =>
            {
                Console.WriteLine(data);

            });



            socket.On("update", data => {

             
                Console.WriteLine("Analysis stream data:\n");
                Console.WriteLine(data);
               

            });

            socket.Emit("join", "fen ky");
            Console.ReadKey();
            socket.Disconnect();
        }
    }
}
