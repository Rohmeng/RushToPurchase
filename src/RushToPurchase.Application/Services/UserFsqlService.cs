using System.Text;
using EasyCaching.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using RushToPurchase.Domain.Entities;
using RushToPurchase.Domain.Interfaces;
using RushToPurchase.Domain.SharedKernel.ConfigOption;
using RushToPurchase.Domain.SharedKernel.Enums;
using RushToPurchase.Domain.SharedKernel.Interfaces;
using Serilog;

namespace RushToPurchase.Application.Services;

public class UserFsqlService : IUserService
{
    private static readonly string SALT = "randomString";
    private static readonly int ALLOW_COUNT = 10;
    
    private readonly IFreeSql _fsql;

    private readonly IRedisCachingProvider _provider;

    public UserFsqlService(IFreeSql fsql, IRedisCachingProvider provider)
    {
        _fsql = fsql;
        _provider = provider;
    }
    
    public async Task<string> GetVerifyHash(int sid, int userId)
    {
        Log.Information("请自行验证是否在抢购时间内");
        
        // 检验用户合法性
        var user = await _fsql.Select<User>().Where(x => x.Id == userId).FirstAsync();
        if (user == null)
        {
            throw new Exception("用户不存在");
        }
        Log.Information("用户信息：[{user}]", user.ToString());

        var stock = await _fsql.Select<Stock>().Where(x => x.Id == sid).FirstAsync();
        if (stock == null)
        {
            throw new Exception("商品不存在");
        }
        Log.Information("商品信息：[{s}]", stock.ToString());
        
        // 生成hash
        string verify = SALT + sid + userId;
        Blake2b blake2B = new Blake2b();
        byte[] verifyBytes = blake2B.Hash(Encoding.UTF8.GetBytes(verify));
        string verifyHash = BitConverter.ToString(verifyBytes);
        // todo 对比两种哈希算法的性能(blake2b/MD5)
        // var hashed = EncryptProvider.Md5(verify);
        
        // 将hash和用户商品信息存入redis
        string hashKey = CacheKey.EshopUserHash + "_" + sid + "_" + userId;
        await _provider.StringSetAsync(hashKey, verifyHash, TimeSpan.FromSeconds(3600));
        Log.Information("Redis写入：[{hash}] [{vhash}]", hashKey, verifyHash);
        return verifyHash;
    }

    public async Task<long> AddUserCount(int userId)
    {
        string limitKey = CacheKey.EshopUserLimit + "_" + userId;
        await _provider.StringSetAsync(limitKey, "0", TimeSpan.FromSeconds(3600), "nx");
        return await _provider.IncrByAsync(limitKey);
    }

    public bool GetUserIsBanned(int userId)
    {
        string limitKey = CacheKey.EshopUserLimit + "_" + userId;
        string limitNum = _provider.StringGet(limitKey);
        if (limitNum == null) {
            Log.Error("该用户没有访问申请验证值记录，疑似异常");
            return true;
        }
        return Convert.ToInt32(limitNum) > ALLOW_COUNT;
    }
}