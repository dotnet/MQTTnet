// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;

namespace MQTTnet.Benchmarks;

public static class Program
{
    static int _selectedBenchmarkIndex;
    static List<Type> _benchmarks;

    public static void Main(string[] arguments)
    {
        _benchmarks = CollectBenchmarks();
        HandleArguments(arguments);

        while (true)
        {
            RenderMenu();

            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.DownArrow)
            {
                _selectedBenchmarkIndex++;
                if (_selectedBenchmarkIndex > _benchmarks.Count - 1)
                {
                    _selectedBenchmarkIndex = 0;
                }
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                _selectedBenchmarkIndex--;
                if (_selectedBenchmarkIndex < 0)
                {
                    _selectedBenchmarkIndex = _benchmarks.Count - 1;
                }
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                Environment.Exit(0);
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
        }

        BenchmarkRunner.Run(_benchmarks[_selectedBenchmarkIndex]);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press any key to exit");
        Console.ReadLine();
    }

    static List<Type> CollectBenchmarks()
    {
        var benchmarks = new List<Type>();

        var types = typeof(Program).Assembly.GetExportedTypes();
        foreach (var type in types)
        {
            if (typeof(BaseBenchmark).IsAssignableFrom(type))
            {
                if (type != typeof(BaseBenchmark))
                {
                    benchmarks.Add(type);
                }
            }
        }

        return benchmarks.OrderBy(b => b.Name).ToList();
    }

    static void HandleArguments(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        // Allow for preselection to avoid developer frustration.

        if (int.TryParse(arguments[0], out var benchmarkIndex))
        {
            _selectedBenchmarkIndex = benchmarkIndex;
            return;
        }

        _selectedBenchmarkIndex = _benchmarks.FindIndex(b => b.Name.Equals(arguments[0]));

        if (_selectedBenchmarkIndex < 0)
        {
            _selectedBenchmarkIndex = 0;
        }

        if (_selectedBenchmarkIndex > _benchmarks.Count - 1)
        {
            _selectedBenchmarkIndex = _benchmarks.Count - 1;
        }
    }

    static void RenderMenu()
    {
        Console.Clear();

        Console.WriteLine($"MQTTnet - Benchmarks");
        Console.WriteLine("-----------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press arrow keys for benchmark selection");
        Console.WriteLine("Press Enter for benchmark execution");
        Console.WriteLine("Press Esc for exit");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();

        for (var index = 0; index < _benchmarks.Count; index++)
        {
            var benchmark = _benchmarks[index];

            if (_selectedBenchmarkIndex == index)
            {
                Console.Write("-> ");
            }
            else
            {
                Console.Write("   ");
            }

            Console.WriteLine(benchmark.Name);
        }

        Console.WriteLine();
    }
}