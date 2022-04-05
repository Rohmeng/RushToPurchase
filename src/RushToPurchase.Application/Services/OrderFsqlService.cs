using EasyCaching.Core;
using FreeSql;
using RushToPurchase.Domain.Entities;
using RushToPurchase.Domain.Interfaces;
using RushToPurchase.Domain.SharedKernel.Enums;
using RushToPurchase.Infra.Data.Mq;
using Serilog;

namespace RushToPurchase.Application.Services;

public class OrderFsqlService : IOrderService
{
    private readonly IFreeSql _fsql;
    private readonly UnitOfWorkManager _unit;
    private readonly IRedisCachingProvider _cachingProvider;
    private readonly IRabbitmqClient _mqProducer;

    public OrderFsqlService(IFreeSql fsql, UnitOfWorkManager unit, IRedisCachingProvider cachingProvider, IRabbitmqClient mqProducer)
    {
        _fsql = fsql;
        _unit = unit;
        _cachingProvider = cachingProvider;
        _mqProducer = mqProducer;
    }

    public async Task<int> CreateWrongOrder(int sid)
    {
        int row = 0;
        Log.Information("----------OrderFsqlService----------");
        try
        {
            //校验库存
            Stock stock = await CheckStock(sid);
            //扣库存
            await SaleStock(stock);
            //创建订单
            row = CreateOrder(stock);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
        return row;
    }

    public async Task<int> CreateOptimisticOrder(int sid)
    {
        //校验库存
        Stock stock = await CheckStock(sid);
        //乐观锁更新库存
        bool success = SaleStockOptimistic(stock);
        if (!success)
        {
            throw new Exception("过期库存值，更新失败");
        }

        //创建订单
        return CreateOrder(stock);
    }

    public async Task<int> CreatePessimisticOrder(int sid)
    {
        //校验库存(悲观锁for update)
        // using (_fsql.CreateUnitOfWork())
        using IUnitOfWork unitOfWork = _unit.Begin();
        var stock = await unitOfWork.Orm.Select<Stock>().ForUpdate().Where(x => x.Id == sid).ToOneAsync();
        //更新库存
        await unitOfWork.Orm.Update<Stock>(stock).Set(x => x.Sale, stock.Sale + 1)
            .Set(x => x.Version, stock.Version + 1).ExecuteAffrowsAsync();
        //创建订单
        var result = await unitOfWork.Orm
            .Insert<StockOrder>(new StockOrder() {Name = stock.Name, Sid = stock.Id, CreateTime = DateTime.Now})
            .ExecuteAffrowsAsync();

        unitOfWork.Commit();
        return result;
    }

    public async Task<int> CreateVerifiedOrder(int sid, int userId, string verifyHash)
    {
        // 验证是否在抢购时间内
        Log.Information("请自行验证是否在抢购时间内,假设此处验证成功");

        // 验证hash值合法性
        String hashKey = CacheKey.EshopUserHash + "_" + sid + "_" + userId;
        Console.WriteLine(hashKey);
        String verifyHashInRedis = _cachingProvider.StringGet(hashKey);
        if (!verifyHash.Equals(verifyHashInRedis))
        {
            throw new Exception("hash值与Redis中不符合");
        }

        Log.Information("验证hash值合法性成功");

        // 检查用户合法性
        var user = await _fsql.Select<User>().Where(x => x.Id == userId).FirstAsync();
        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        Log.Information("用户信息验证成功：[{user}]", user.ToString());

        // 检查商品合法性
        Stock? stock = await GetStockById(sid);
        if (stock == null)
        {
            throw new Exception("商品不存在");
        }

        Log.Information("商品信息验证成功：[{stock}]", stock.ToString());

        //乐观锁更新库存
        bool success = SaleStockOptimistic(stock);
        if (!success)
        {
            throw new Exception("过期库存值，更新失败");
        }

        Log.Information("乐观锁更新库存成功");

        //创建订单
        CreateOrderWithUserInfoInDb(stock, userId);
        Log.Information("创建订单成功");

        return stock.Count - (stock.Sale + 1);
    }

    public void CreateOrderByMq(int sid, int userId)
    {
        _mqProducer.CreateOrder(sid, userId);
    }

    public void CreateOrderByMqConsumer(int sid, int userId)
    {
        //模拟拥堵
        Thread.Sleep(5000);

        //校验库存
        var stock = _fsql.Select<Stock>().Where(x=>x.Id == sid).First();
        if (stock.Count <= stock.Sale)
        {
            Log.Information("库存不足！");
            return;
        }
        //乐观锁更新库存
        var updateStock = SaleStockOptimistic(stock);
        if (!updateStock) {
            Log.Warning("扣减库存失败，库存已经为0");
            return;
        }

        Log.Information("扣减库存成功，剩余库存：[{count}]", stock.Count - stock.Sale - 1);
        DelStockCountCache(sid);
        Log.Information("删除库存缓存");

        //创建订单
        Log.Information("写入订单至数据库");
        CreateOrderWithUserInfoInDb(stock, userId);
        Log.Information("写入订单至缓存供查询");
        CreateOrderWithUserInfoInCache(stock, userId);
        Log.Information("下单完成");
    }

    public bool CheckUserOrderInfoInCache(int sid, int userId)
    {
        throw new NotImplementedException();
    }

    /**
     * 检查库存
     * @param sid
     * @return
     */
    private async Task<Stock> CheckStock(int sid)
    {
        Log.Information("开始检查库存...");
        Stock stock = await GetStockById(sid);
        if (stock != null && stock.Sale >= stock.Count)
        {
            throw new Exception("库存不足");
        }

        return stock;
    }

    /**
     * 更新库存
     * @param stock
     */
    private async Task SaleStock(Stock stock)
    {
        stock.Sale += 1;
        await UpdateStockById(stock);
    }

    /**
     * 更新库存 乐观锁
     * @param stock
     */
    private bool SaleStockOptimistic(Stock stock)
    {
        Log.Information("查询数据库，尝试更新库存");
        int count = UpdateStockByOptimistic(stock);
        return count != 0;
    }

    /**
     * 创建订单
     * @param stock
     * @return
     */
    private int CreateOrder(Stock stock)
    {
        StockOrder order = new StockOrder
        {
            Sid = stock.Id,
            Name = stock.Name,
            CreateTime = DateTime.Now
        };
        return _fsql.Insert<StockOrder>(order).ExecuteAffrows();
    }

    /**
     * 创建订单：保存用户订单信息到数据库
     * @param stock
     * @return
     */
    private int CreateOrderWithUserInfoInDb(Stock stock, int userId)
    {
        StockOrder order = new StockOrder
        {
            Sid = stock.Id,
            Name = stock.Name,
            UserId = userId,
            CreateTime = DateTime.Now
        };
        return _fsql.Insert<StockOrder>(order).ExecuteAffrows();
    }

    /**
     * 创建订单：保存用户订单信息到缓存
     * @param stock
     * @return 返回添加的个数
     */
    private long CreateOrderWithUserInfoInCache(Stock stock, int userId)
    {
        try
        {
            string key = CacheKey.EshopUserHasOrder + "_" + stock.Id.ToString();
            Log.Information("写入用户订单数据Set：[{key}] [{userid}]", key, userId.ToString());
            var arr = new int[] {userId};
            return _cachingProvider.SAdd(key, arr);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
        return 0;
    }


    #region stock

    public int? GetStockCount(int sid)
    {
        int? stockLeft = GetStockCountByCache(sid);
        Log.Information("缓存中取得库存数：[{stock}]", stockLeft);
        if (stockLeft == null)
        {
            stockLeft = GetStockCountByDb(sid);
            Log.Information("缓存未命中，查询数据库，并写入缓存");
            SetStockCountCache(sid, stockLeft);
        }
        return stockLeft;

    }

    public int? GetStockCountByDb(int sid)
    {
        var stock = _fsql.Select<object>().WithSql("SELECT (count - sale) count FROM stock WHERE id = @id", new {id = sid})
            .ToList<int>("count")
            .First();
        return stock;
    }

    public int? GetStockCountByCache(int id)
    {
        string hashKey = CacheKey.EshopStockCount + "_" + id;
        string countStr = _cachingProvider.StringGet(hashKey);
        if (countStr != null)
        {
            return Convert.ToInt32(countStr);
        }
        else
        {
            return null;
        }
    }

    public void SetStockCountCache(int id, int? count)
    {
        if(count == null) return;
        string hashKey = CacheKey.EshopStockCount + "_" + id;
        string countStr = count.ToString();
        Log.Information("写入商品库存缓存: [{key}] [{count}]", hashKey, countStr);
        _cachingProvider.StringSet(hashKey, countStr, TimeSpan.FromSeconds(3600));
    }

    public void DelStockCountCache(int id, bool mq = false)
    {
        string hashKey = CacheKey.EshopStockCount + "_" + id;
        if (mq)
        {
            _mqProducer.DeleteCacheKey(hashKey);
        }
        else
        {
            _cachingProvider.KeyDel(hashKey);
        }
        Log.Information("删除商品id：[{id}] 缓存", id);
    }

    public Task<Stock> GetStockById(int id)
    {
        return _fsql.Select<Stock>().Where(x => x.Id == id).FirstAsync();
    }

    public Stock GetStockByIdForUpdate(int id)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateStockById(Stock stock)
    {
        await _fsql.Update<Stock>(stock).ExecuteAffrowsAsync();
    }

    public int UpdateStockByOptimistic(Stock stock)
    {
        // 或者通过指定乐观锁属性 [Column(IsVersion = true)] 
        return _fsql.Select<Stock>().Where(x => x.Id == stock.Id && x.Version == stock.Version).ToUpdate()
            .Set(x => x.Sale, stock.Sale + 1).Set(x => x.Version, stock.Version + 1)
            .ExecuteAffrows();
    }

    public void KeyDelMessage(string key)
    {
        _cachingProvider.KeyDel(key);
        // we just print this message   
        Log.Information($"[KeyDelMessage] consumer received key:{key}");
    }

    #endregion
}