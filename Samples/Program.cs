// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

Console.BackgroundColor = ConsoleColor.White;
Console.BackgroundColor = ConsoleColor.Red;
Console.Title = "Samples - MQTTnet";
Console.WriteLine("----------------------------------------------");
Console.WriteLine("-         Welcome to MQTTnet samples!        -");
Console.WriteLine("----------------------------------------------");
Console.ResetColor();
Console.WriteLine();

var sampleClasses = Assembly.GetExecutingAssembly().GetExportedTypes().OrderBy(c => c.Name).ToList();

var index = 0;
foreach (var sampleClass in sampleClasses)
{
    Console.WriteLine($"{index} = {sampleClass.Name}");
    index++;
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.Write("Please choose sample class (press Enter to continue): ");
Console.ResetColor();

var input = Console.ReadLine();
var selectedIndex = int.Parse(input ?? "0", CultureInfo.InvariantCulture);
var selectedSampleClass = sampleClasses[selectedIndex];
var sampleMethods = selectedSampleClass.GetMethods(BindingFlags.Static | BindingFlags.Public).OrderBy(m => m.Name).ToList();

index = 0;
foreach (var sampleMethod in sampleMethods)
{
    Console.WriteLine($"{index} = {sampleMethod.Name}");
    index++;
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.Write("Please choose sample (press Enter to continue): ");
Console.ResetColor();

input = Console.ReadLine();
selectedIndex = int.Parse(input ?? "0", CultureInfo.InvariantCulture);
var selectedSampleMethod = sampleMethods[selectedIndex];

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("Executing sample...");
Console.ResetColor();
Console.WriteLine();

try
{
    if (selectedSampleMethod.Invoke(null, null) is Task task)
    {
        await task;
    }
}
catch (Exception exception)
{
    Console.WriteLine(exception.ToString());
}

Console.WriteLine();
Console.WriteLine("Press Enter to exit.");
Console.ReadLine();