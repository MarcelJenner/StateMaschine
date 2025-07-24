using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Utils;

namespace StateMachineDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddConsole())
                .AddSingleton<DemoStateMachine>();

            using var serviceProvider = services.BuildServiceProvider();
            var stateMachine = serviceProvider.GetService<DemoStateMachine>();

            Console.WriteLine("enter commands to transition states");
            
            Console.WriteLine("- initialise");
            Console.WriteLine("- finish");
            Console.WriteLine("- done");
            
            while (true)
            {
                var command = Console.ReadLine();

                if (Enum.TryParse<StateMachineBaseCommand>(command, true, out var cmd))
                {
                    if(cmd == StateMachineBaseCommand.Cancel)
                        stateMachine.Cancel();
                    stateMachine.EnqueueTransition(cmd);
                }
                    
                if (Enum.TryParse<Command>(command, true, out var cmd2))
                {
                    stateMachine.EnqueueTransition(cmd2);
                }

                if (string.Equals(command, "exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
            }
        }
    }
}