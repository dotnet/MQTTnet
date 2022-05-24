// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

Console.WriteLine("Welcome to MQTTnet samples!");
Console.WriteLine();

var sampleClasses = Assembly.GetExecutingAssembly().GetExportedTypes().OrderBy(c => c.Name).ToList();

var index = 0;
foreach (var sampleClass in sampleClasses)
{
    Console.WriteLine($"{index} = {sampleClass.Name}");
    index++;
}

Console.Write("Please choose sample class (press Enter to continue): ");
var input = Console.ReadLine();
var selectedIndex = int.Parse(input ?? "0");
var selectedSampleClass = sampleClasses[selectedIndex];
var sampleMethods = selectedSampleClass.GetMethods(BindingFlags.Static | BindingFlags.Public).OrderBy(m => m.Name).ToList();

index = 0;
foreach (var sampleMethod in sampleMethods)
{
    Console.WriteLine($"{index} = {sampleMethod.Name}");
    index++;
}

Console.Write("Please choose sample (press Enter to continue): ");
input = Console.ReadLine();
selectedIndex = int.Parse(input ?? "0");
var selectedSampleMethod = sampleMethods[selectedIndex];

Console.WriteLine("Executing sample...");
Console.WriteLine();

try
{
    var task = selectedSampleMethod.Invoke(null, null) as Task;
    task?.Wait();
}
catch (Exception exception)
{
    Console.WriteLine(exception.ToString());
}