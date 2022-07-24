using System;

using System.Diagnostics;
using System.Windows.Input;

using System.Threading;
using System.Security.Cryptography;
using System.Globalization;
using System.Numerics;

namespace ArteMiner
{

    public static class Program
    {
        public static Stopwatch sw = new Stopwatch();
        public static bool running = true;

        public static void Main(string[] args)
        {
            Welcome();

            Logger.LogMessage("Main", "Please insert amount of threads (insert the amount of cores your CPU has for optimal performance).");
            Console.Write("Threads >");
            string input = Console.ReadLine();
            int amount = 0;
            bool valid = int.TryParse(input, NumberStyles.Integer, System.Globalization.NumberFormatInfo.CurrentInfo, out amount);
            if(!valid)
            {
                Logger.LogMessage("Main", input + " is not a valid Integer");
                Environment.Exit(1);
            }

            Logger.LogMessage("Main", "Starting timer.");

            sw.Start();

            Logger.LogMessage("Main", "Starting keyboard thread.");

            new Thread(() =>
            {
                
                while(running)
                {
                    if(Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey();

                        switch (key.Key)
                        {
                            case ConsoleKey.Q:
                                Stop();
                                break;
                            default:
                                break;
                        }
                    }
                }

            }).Start();

            Logger.LogMessage("Main", "Starting mining on 4 threads.");
            MinerJobManager.CreateJobs(750000000, 4);

            while(running)
            {
                Thread.Sleep(10000);
            
                int count = 0;
                float added = 0;
                float avg = 0;

                foreach(MinerJob job in MinerJobManager.jobs)
                {
                    count++;

                    added += job.hashes;
                    job.hashes = 0;
                }

                avg = added / count;
                float hrate = avg / 1000;
                string hashrate;

                if(hrate >= 1000000000000000000)
                {
                    hashrate = Math.Round(hrate / 1000000000000000, 4) + " PH/S";
                }
                else if(hrate >= 100000000000000)
                {
                    hashrate = Math.Round(hrate / 1000000000000000, 4) + " PH/S";
                }
                else if(hrate >= 100000000000)
                {
                    hashrate = Math.Round(hrate / 1000000000000, 4) + " TH/S";
                }
                else if(hrate >= 100000000)
                {
                    hashrate = Math.Round(hrate / 1000000000, 4) + " GH/S";
                }
                else if(hrate >= 1000000)
                {
                    hashrate = Math.Round(hrate / 1000000, 4) + " MH/S";
                }
                else if(hrate >= 1000)
                {
                    hashrate = Math.Round(hrate / 1000, 4) + " KH/S";
                }
                else
                {
                    hashrate = Math.Round(hrate, 4) + " H/S";
                }

                Logger.LogMessage("Main", "Average hashrate: " + hashrate + " Total hashes: " + added + " Threads: " + count);
            }
        }

        public static void Stop()
        {
            Logger.LogMessage("Main", "Stopping mining threads");
            MinerJobManager.StopAll();
            Logger.LogMessage("Main", "Here is some info about this session:");

            Logger.LogMessage("Main", "Uptime: " + sw.Elapsed.TotalSeconds);
            Logger.LogMessage("Main", "");

            Int64 added = 0;
            int count = 0;

            foreach(MinerJob job in MinerJobManager.jobs)
            {
                count++;

                added += job.nonce;
            }

            Logger.LogMessage("Main", "Hashes Calculated: " + added);
            Logger.LogMessage("Main", "Exiting, Goodbye.");
            Environment.Exit(0);
        }
    
        public static void Welcome()
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Blue;

            // arteMiner

            Console.WriteLine("------------------------------");
            Console.WriteLine("----------arteMiner-----------");
            Console.WriteLine("------------------------------");
            Console.WriteLine("------------DDev--------------");
            Console.WriteLine("------------------------------");
            Console.WriteLine();
            Console.WriteLine("Welcome to arteMiner.");

            //Console.ResetColor();
        }
    
    }

    public static class Logger
    {

        public static void LogMessage(string source, string message)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Blue;

            Console.WriteLine(source + " => " + message);

            Console.ResetColor();
        }

    }

    public static class MinerInfo
    {
        //public static string blockStr = "02000000" + // Version
        //                                "b6ff0b1b1680a2862a30ca44d346d9e8" + //
        //                                "910d334beb48ca0c0000000000000000" + // Hash
        //                                "9d10aa52ee949386ca9385695f04ede2" + //
        //                                "70dda20810decd12bc9b048aaab31471" + // Merkle root
        //                                "";
    
        public static string blockStr = "01000000000000000000000000000000000000000000000000000000000000000000000" +
                                        "03ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4a29ab5f" +
                                        "49ffff001d1dac2b7c01010000000100000000000000000000000000000000000000000" +
                                        "00000000000000000000000ffffffff4d04ffff001d0104455468652054696d65732030" +
                                        "332f4a616e2f32303039204368616e63656c6c6f72206f6e20627266e6b206f66207365" +
                                        "636f6e64206261696c6f757420666f722062616e6b73ffffffff0100f2052a010000004" +
                                        "34104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649" +
                                        "f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac0000000";

        public static BigInteger blockInt;
        public static byte[] blockByte = Convert.FromHexString(blockStr);

        public static string targetStr = "00000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
        public static BigInteger target = BigInteger.Parse(targetStr, NumberStyles.HexNumber);
    }

    public static class Utils
    {

    }

    public static class MinerJobManager
    {

        public static Dictionary<string,Thread> threads = new Dictionary<string, Thread>();
        public static List<MinerJob> jobs = new List<MinerJob>();

        public static void CreateJobs(Int64 batchSize, int jobCount)
        {
            Logger.LogMessage("JobManager", "Total maximum nonce: " + batchSize * jobCount);

            int i = 0;
            for(i = 0; i < jobCount; i++)
            {
                Int64 start = i * batchSize;

                Logger.LogMessage("JobManager", "Starting job with nonce: " + start.ToString());
                StartJob(batchSize, start);
            }
        }

        public static void Solved(byte[] hash, Int64 nonce)
        {
            StopAll();
            Program.running = false;

            string h = "";
            foreach(byte b in hash)
                h += b.ToString("x");

            Logger.LogMessage("JobManager", "Solution found!");

            Logger.LogMessage("JobManager", "Hash: " + h);
            Logger.LogMessage("JobManager", "Nonce: " + nonce.ToString());

            Int64 added = 0;
            int count = 0;

            foreach(MinerJob job in MinerJobManager.jobs)
            {
                count++;

                added += job.nonce;
            }

            Logger.LogMessage("JobManager", "Hashes Calculated: " + added);

            Logger.LogMessage("JobManager", "Uptime: " + Program.sw.Elapsed.TotalSeconds);

            Logger.LogMessage("JobManager", "Exiting, Goodbye.");
        }

        [Obsolete]
        public static void StartAll()
        {
            foreach(MinerJob job in jobs)
            {
                job.Start();
            }
        }

        public static void StopAll()
        {
            foreach(MinerJob job in jobs)
            {
                job.Stop();
            }
        }

        public static void StartJob(Int64 batchSize, Int64 nonce)
        {
            Thread t = new Thread(() => 
            {
                Thread.CurrentThread.IsBackground = false; 

                MinerJob job = new MinerJob();

                job.batchSize = batchSize;
                job.startNonce = nonce;   

                jobs.Add(job);

                job.Start();

            });

            threads.Add(nonce.ToString(), t);

            t.Start();
        }
    }

    public class MinerJob
    {
        public Int64 batchSize { get; set;}
        public Int64 startNonce { get; set; }

        public Int64 nonce { get; private set; }

        public int hashes { get; set; }

        public TimeSpan startTime;

        public MinerJob()
        {
            // Create job
            hashes = 0;

            startTime = DateTime.Now.TimeOfDay;
        }

        private bool canCompute = true;

        public void Start()
        {
            canCompute = true;
            Compute();
        }

        public void Stop()
        {
            canCompute = false;
        }

        private void AnnounceSolution(byte[] hash)
        {
            canCompute = false;
            MinerJobManager.Solved(hash, nonce);
        }

        private SHA256 sha = SHA256.Create();

        public byte[] SHA(byte[] input)
        {
            return sha.ComputeHash(input);
        }

        public byte[] Hash(byte[] input)
        {
            return SHA(SHA(input));
        }

        public bool Validate(byte[] hash)
        {
            BigInteger hashInt = BitConverter.ToInt64(hash);
            return hashInt >= MinerInfo.target;
        }

        public void Compute()
        {
            bool foundSolution = false;

            byte[] currentHash;

            while(canCompute && nonce < batchSize)
            {
                currentHash = Hash((MinerInfo.blockInt + (startNonce +nonce)).ToByteArray());
                hashes++;

                foundSolution = Validate(currentHash);

                if(foundSolution)
                    AnnounceSolution(currentHash);
                else
                    nonce++;
            }

            //Console.WriteLine("Finished");
        }

    }

}

