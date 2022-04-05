using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NETCore.Encrypt;
using NSec.Cryptography;

namespace RushToPurchase.Perf;

[HtmlExporter]
[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser, RankColumn]
public class HashTest
{
    private readonly string verify = "SALT$%^0^_348453_56745674563453007";

    [Benchmark]
    public string BlakeTest()
    {
        Blake2b blake2B = new Blake2b();
        byte[] verifyBytes = blake2B.Hash(Encoding.UTF8.GetBytes(verify));
        return BitConverter.ToString(verifyBytes);
    }
    
    [Benchmark]
    public string Md5Test()
    {
        return EncryptProvider.Md5(verify);
    }
    
}