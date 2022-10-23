using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System;

namespace PointyPortScan
{

    class PPS_Program
    {

    static bool stop = false;
    static int firstPort;
    static int lastPort;

    static List<int> portsOpen = new List<int>();

    static object consoleLock = new object();

    static int waitingForResponses;

    static int maxConcurrentQueries = 100;

    static void Main(string[] args)
    {
    begin:
        Console.WriteLine("Enter target's IP Address: ");
        string ip = Console.ReadLine();

        IPAddress ipAddress;

        if(!IPAddress.TryParse(ip, out ipAddress))
            goto begin;

    firstP:
        Console.WriteLine("Enter the first port in the range you want to scan: ");
        string fp = Console.ReadLine();

        if(!int.TryParse(fp, out firstPort))
            goto firstP;

    lastP:
        Console.WriteLine("Enter the last port in the range you want to scan: ");
        string lp = Console.ReadLine();

        if(!int.TryParse(lp, out lastPort))
            goto lastP;

        Console.WriteLine(".............\n\n\n");
        Console.WriteLine("Stop scan by pressing any key!");
        Console.WriteLine(".............\n\n\n");

        ThreadPool.QueueUserWorkItem(StartScan, ipAddress);

        Console.ReadKey();

        stop = true;

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

    }
    

    static void StartScan(object x)
    {
        IPAddress ipAddress = x as IPAddress;

        for(int i = firstPort; i < lastPort; i++)
        {
            lock(consoleLock)
            {
                int top = Console.CursorTop;

                Console.CursorTop = 7;
                Console.WriteLine("Scanning port {0}  ", i);
                Console.CursorTop = top;
            }
            while(waitingForResponses >= maxConcurrentQueries)
                Thread.Sleep(0);
            if(stop)
                break;

            try
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    s.BeginConnect(new IPEndPoint(ipAddress, i), EndConnect, s);

                    Interlocked.Increment(ref waitingForResponses);
                }
                catch (Exception)
                {

                }
            }
        }

        static void EndConnect(IAsyncResult ar)
        {
            try
            {
                DecrementResponses();

                Socket s = ar.AsyncState as Socket;

                s.EndConnect(ar);

                if (s.Connected)
                {
                    int openPort = Convert.ToInt32(s.RemoteEndPoint.ToString().Split(':')[1]);

                    portsOpen.Add(openPort);
                    
                    lock (consoleLock)
                    {
                        Console.WriteLine("Connected TCP on port: {0}", openPort);
                    }                

                    s.Disconnect(true);
                }
            }
            catch (Exception)
            {
                
            }
        }

        static void IncrementResponses()
        {
            Interlocked.Increment(ref waitingForResponses);
            PrintWaitingForResponses();
        }

        static void DecrementResponses()
        {
            Interlocked.Decrement(ref waitingForResponses);

            PrintWaitingForResponses();
        }

        static void PrintWaitingForResponses()
        {
            lock (consoleLock)
            {
                int top = Console.CursorTop;

                Console.CursorTop = 8;
                Console.WriteLine("Waiting for responses from {0} sockets ", waitingForResponses);
                Console.CursorTop = top;
            }
        }
    }
}
    



