using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomTaskScheduler.Example
{
    sealed class Program
    {
        const int NumOfThreads = 4;
        const int NumOfTasks = 5;

        static readonly TaskFactory _taskFactory;

        static Program()
        {
            var scheduler = new CustomTaskScheduler(Program.NumOfThreads);
            Program._taskFactory = new TaskFactory(scheduler);
        }

        static void Main(string[] args)
        {
            var taskList = new List<Task>();

            // Create 5 independent tasks:
            for (var i = 1; i <= Program.NumOfTasks; i++)
            {
                var counter = i;

                taskList.Add(Program._taskFactory.StartNew(() =>
                {
                    Console.WriteLine($"Task {counter} started...");

                    // Wait for 5 seconds:
                    Thread.Sleep(5000);

                    Console.WriteLine($"Task {counter} completed.");
                }));
            }

            Console.ReadKey();
        }
    }
}
