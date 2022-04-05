using EasyCaching.Core;
using Microsoft.Extensions.Logging;
using RushToPurchase.Domain.Entities;
using RushToPurchase.Domain.Interfaces;
using RushToPurchase.Domain.SharedKernel.Enums;
using RushToPurchase.Domain.SharedKernel.Interfaces;
using Serilog;

namespace RushToPurchase.Application.Services;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;

    private readonly IRepository<StockOrder> _orderRepository;

    private readonly IReadRepository<User> _userRepository;
    
    private readonly IRepository<Stock> _stockRepository;

    //private readonly IStockService _stockService;

    private readonly IRedisCachingProvider _cachingProvider;

    public OrderService(ILogger<OrderService> logger, IRepository<StockOrder> orderRepository,
        IReadRepository<User> userRepository,
        IRepository<Stock> stockRepository, IRedisCachingProvider cachingProvider)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _stockRepository = stockRepository;
        _cachingProvider = cachingProvider;
        _userRepository = userRepository;
    }

    public async Task<int> CreateWrongOrder(int sid)
    {
        //校验库存
        Stock stock = await CheckStock(sid);
        //扣库存
        await SaleStock(stock);
        //创建订单
        StockOrder order = await CreateOrder(stock);
        return order.Id;
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
        await CreateOrder(stock);
        return stock.Count - (stock.Sale + 1);
    }

    public async Task<int> CreatePessimisticOrder(int sid)
    {
        //校验库存(悲观锁for update)
        Stock stock = CheckStockForUpdate(sid);
        //更新库存
        SaleStock(stock);
        //创建订单
        StockOrder order = await CreateOrder(stock);
        return order.Id;
        //return stock.Count - (stock.Sale + 1);
    }

    public async Task<int> CreateVerifiedOrder(int sid, int userId, string verifyHash)
    {
        // 验证是否在抢购时间内
        _logger.LogInformation("请自行验证是否在抢购时间内,假设此处验证成功");

        // 验证hash值合法性
        String hashKey = CacheKey.EshopUserHash + "_" + sid + "_" + userId;
        Console.WriteLine(hashKey);
        String verifyHashInRedis = _cachingProvider.StringGet(hashKey);
        if (!verifyHash.Equals(verifyHashInRedis))
        {
            throw new Exception("hash值与Redis中不符合");
        }

        _logger.LogInformation("验证hash值合法性成功");

        // 检查用户合法性
        User? user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        _logger.LogInformation("用户信息验证成功：[{user}]", user.ToString());

        // 检查商品合法性
        Stock? stock = await GetStockById(sid);
        if (stock == null)
        {
            throw new Exception("商品不存在");
        }

        _logger.LogInformation("商品信息验证成功：[{s}]", stock.ToString());

        //乐观锁更新库存
        bool success = SaleStockOptimistic(stock);
        if (!success)
        {
            throw new Exception("过期库存值，更新失败");
        }

        _logger.LogInformation("乐观锁更新库存成功");

        //创建订单
        await CreateOrderWithUserInfoInDb(stock, userId);
        _logger.LogInformation("创建订单成功");

        return stock.Count - (stock.Sale + 1);
    }

    public void CreateOrderByMq(int sid, int userId)
    {
        throw new NotImplementedException();
    }

    public void CreateOrderByMqConsumer(int sid, int userId)
    {
        throw new NotImplementedException();
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
     * 检查库存 ForUpdate
     * @param sid
     * @return
     */
    private Stock CheckStockForUpdate(int sid)
    {
        Stock stock = GetStockByIdForUpdate(sid);
        if (stock.Sale >= stock.Count)
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
        _logger.LogInformation("查询数据库，尝试更新库存");
        int count = UpdateStockByOptimistic(stock);
        return count != 0;
    }

    /**
     * 创建订单
     * @param stock
     * @return
     */
    private Task<StockOrder> CreateOrder(Stock stock)
    {
        StockOrder order = new StockOrder
        {
            Sid = stock.Id,
            Name = stock.Name,
            CreateTime = DateTime.Now
        };
        return _orderRepository.AddAsync(order);
    }

    /**
     * 创建订单：保存用户订单信息到数据库
     * @param stock
     * @return
     */
    private Task<StockOrder> CreateOrderWithUserInfoInDb(Stock stock, int userId)
    {
        StockOrder order = new StockOrder
        {
            Sid = stock.Id,
            Name = stock.Name,
            UserId = userId,
            CreateTime = DateTime.Now
        };
        return _orderRepository.AddAsync(order);
    }

    /**
     * 创建订单：保存用户订单信息到缓存
     * @param stock
     * @return 返回添加的个数
     */
    private long CreateOrderWithUserInfoInCache(Stock stock, int userId)
    {
        String key = CacheKey.EshopUserHasOrder + "_" + stock.Id.ToString();
        _logger.LogInformation("写入用户订单数据Set：[{k}] [{u}]", key, userId.ToString());
        return _cachingProvider.SAdd(key, new List<int>(userId));
    }


    #region stock 
     public int? GetStockCount(int sid)
    {
        int? stockLeft = GetStockCountByCache(sid);
        _logger.LogInformation("缓存中取得库存数：[{s}]", stockLeft);
        if (stockLeft == null) {
            stockLeft = GetStockCountByDb(sid);
            _logger.LogInformation("缓存未命中，查询数据库，并写入缓存");
            SetStockCountCache(sid, stockLeft);
        }
        return (int)stockLeft;

    }

    public int? GetStockCountByDb(int id)
    {
        Task<Stock?> stock = _stockRepository.GetByIdAsync(id);
        stock.Wait();
        if (stock.Result != null)
            return stock.Result.Count - stock.Result.Sale;
        else
            return 0;
    }

    public int? GetStockCountByCache(int id)
    {
        string hashKey = CacheKey.EshopStockCount + "_" + id;
        string countStr = _cachingProvider.StringGet(hashKey);
        if (countStr != null) {
            return Convert.ToInt32(countStr);
        } else {
            return null;
        }
    }

    public void SetStockCountCache(int id, int? count)
    {
        if (count == null)
        {
            return;
        }
        string hashKey = CacheKey.EshopStockCount + "_" + id;
        string countStr = count.ToString();
        _logger.LogInformation("写入商品库存缓存: [{}] [{}]", hashKey, countStr);
        _cachingProvider.StringSet(hashKey, countStr, TimeSpan.FromSeconds(3600));
    }

    public void DelStockCountCache(int id, bool mq = false)
    {
        string hashKey = CacheKey.EshopStockCount + "_" + id;
        _cachingProvider.KeyDel(hashKey);
        _logger.LogInformation("删除商品id：[{}] 缓存", id);
    }

    public Task<Stock> GetStockById(int id)
    {
        return _stockRepository.GetByIdAsync(id);
    }

    public Stock GetStockByIdForUpdate(int id)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateStockById(Stock stock)
    {
       await _stockRepository.UpdateAsync(stock);
    }

    public int UpdateStockByOptimistic(Stock stock)
    {
        return _stockRepository.ExecuteSqlRaw("update stock set sale = {0}, version = {1} where id = {2} and version = {3}",
            stock.Sale + 1, stock.Version + 1, stock.Id, stock.Version);
    }

    public void KeyDelMessage(string key)
    {
        throw new NotImplementedException();
    }

    #endregion
}