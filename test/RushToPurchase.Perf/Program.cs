// See https://aka.ms/new-console-template for more information

using System;
using BenchmarkDotNet.Running;

Console.WriteLine("Hello, BenchmarkDotNet!");
var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
Console.ReadLine();