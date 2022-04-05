using Microsoft.AspNetCore.Mvc;
using RushToPurchase.Domain.Interfaces;
using Serilog;

namespace RushToPurchase.WebApi.Controllers;

[ApiController]
[Route("/[controller]/[action]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IUserService _userService;

    public OrderController(IEnumerable<IOrderService> orderServices, IEnumerable<IUserService> userServices)
    {
        this._orderService = orderServices.Last();
        this._userService = userServices.Last();
    }

    /// <summary>
    /// 下单接口：会导致超卖
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<int> CreateWrongOrder(int sid)
    {
        Log.Information("请求路径：{Path}", HttpContext.Request.Path.Value);
        int id = 0;
        try
        {
            id = await _orderService.CreateWrongOrder(sid);
            Log.Information("创建订单id: [{id}]", id);
        }
        catch (Exception e)
        {
            Log.Error("Exception: {message}", e.Message);
        }

        return id;
    }

    /// <summary>
    /// 下单接口：version 乐观锁更新库存 + 限流
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOptimisticOrder(int sid)
    {
        int id;
        try
        {
            id = await _orderService.CreateOptimisticOrder(sid);
            Log.Information("购买成功，剩余库存为: [{id}]", id);
        }
        catch (Exception e)
        {
            Log.Error("购买失败：[{m}]", e.Message);
            return "购买失败，库存不足";
        }

        return id.ToString();
    }

    /// <summary>
    /// 下单接口：悲观锁更新库存 事务for update更新库存
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreatePessimisticOrder(int sid)
    {
        int id;
        try
        {
            id = await _orderService.CreatePessimisticOrder(sid);
            Log.Information("购买成功，剩余库存为: [{id}]", id);
        }
        catch (Exception e)
        {
            Log.Error("购买失败：[{message}]", e.Message);
            return "购买失败，库存不足";
        }

        return id.ToString();
    }

    /// <summary>
    /// 验证接口：下单前用户获取验证值
    /// </summary>
    /// <param name="sid"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> GetVerifyHash(int sid, int userId)
    {
        string hash;
        try
        {
            hash = await _userService.GetVerifyHash(sid, userId);
        }
        catch (Exception e)
        {
            Log.Error("获取验证hash失败，原因：[{message}]", e.Message);
            return "获取验证hash失败";
        }

        return hash;
    }

    /// <summary>
    /// 下单接口：要求用户验证的抢购接口
    /// </summary>
    /// <param name="sid"></param>
    /// <param name="userId"></param>
    /// <param name="verifyHash"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOrderWithVerifiedUrl(int sid, int userId, string verifyHash)
    {
        int stockLeft;
        try
        {
            stockLeft = await _orderService.CreateVerifiedOrder(sid, userId, verifyHash);
            Log.Information("购买成功，剩余库存为: [{stock}]", stockLeft);
        }
        catch (Exception e)
        {
            Log.Error("购买失败：[{m}]", e.Message);
            return e.Message;
        }

        return $"购买成功，剩余库存为：{stockLeft}";
    }

    /// <summary>
    /// 下单接口：要求验证的抢购接口 + 单用户限制访问频率
    /// </summary>
    /// <param name="sid"></param>
    /// <param name="userId"></param>
    /// <param name="verifyHash"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOrderWithVerifiedUrlAndLimit(int sid, int userId, string verifyHash)
    {
        int stockLeft;
        try
        {
            var count = await _userService.AddUserCount(userId);
            Log.Information("用户截至该次的访问次数为: [{count}]", count);
            bool isBanned = _userService.GetUserIsBanned(userId);
            if (isBanned)
            {
                return "购买失败，超过频率限制";
            }

            stockLeft = await _orderService.CreateVerifiedOrder(sid, userId, verifyHash);
            Log.Information("购买成功，剩余库存为: [{stock}]", stockLeft);
        }
        catch (Exception e)
        {
            Log.Error("购买失败：[{e}]", e.Message);
            return e.Message;
        }

        return $"购买成功，剩余库存为：{stockLeft}";
    }
    
    /// <summary>
    /// 下单接口：先删除缓存，再更新数据库
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOrderWithCacheV1(int sid) {
        int count = 0;
        try {
            // 删除库存缓存
            _orderService.DelStockCountCache(sid);
            // 完成扣库存下单事务
            count = await _orderService.CreatePessimisticOrder(sid);
        } catch (Exception e) {
            Log.Error("购买失败：[{e}]", e.Message);
            return "购买失败，库存不足";
        }
        Log.Information(count == 1 ? "购买成功" : "购买失败");
        return count == 1 ? "购买成功" : "购买失败";
    }
    /// <summary>
    /// 下单接口：先更新数据库,再删除缓存
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOrderWithCacheV2(int sid) {
        int count = 0;
        try {
            // 完成扣库存下单事务
            count = await _orderService.CreatePessimisticOrder(sid);
            // 删除库存缓存
            _orderService.DelStockCountCache(sid);
        } catch (Exception e) {
            Log.Error("购买失败：[{e}]", e.Message);
            return "购买失败，库存不足";
        }
        Log.Information(count == 1 ? "购买成功" : "购买失败");
        return count == 1 ? "购买成功" : "购买失败";
    }
    
    /// <summary>
    /// 下单接口：先删除缓存，再更新数据库, 再删缓存(延时双删)
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOrderWithCacheV3(int sid) {
        int count = 0;
        try {
            // 删除库存缓存
            _orderService.DelStockCountCache(sid);
            // 完成扣库存下单事务
            count = await _orderService.CreatePessimisticOrder(sid);

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(300);
                _orderService.DelStockCountCache(sid);
            });
        } catch (Exception e) {
            Log.Error("购买失败：[{e}]", e.Message);
            return "购买失败，库存不足";
        }
        Log.Information(count == 1 ? "购买成功" : "购买失败");
        return count == 1 ? "购买成功" : "购买失败";
    }
    
    /// <summary>
    /// 下单接口：先删除缓存，再更新数据库, 再删缓存(延时双删)失败则通知消息队列
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<string> CreateOrderWithCacheV4(int sid) {
        int count = 0;
        try {
            // 删除库存缓存
            _orderService.DelStockCountCache(sid);
            // 完成扣库存下单事务
            count = await _orderService.CreatePessimisticOrder(sid);

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(300);
                _orderService.DelStockCountCache(sid);
            });
        } catch (Exception e) {
            Log.Error("购买失败：[{e}]", e.Message);
            return "购买失败，库存不足";
        }
        Log.Information(count == 1 ? "购买成功" : "购买失败");
        return count == 1 ? "购买成功" : "购买失败";
    }
    
    [HttpGet]
    public string CreateOrderWithMq(int sid, int userId) {
        try {
            // 检查缓存中商品是否还有库存
            var count = _orderService.GetStockCount(sid);
            if (count == null || count == 0) {
                return "秒杀请求失败，库存不足.....";
            }

            // 有库存，则将用户id和商品id封装为消息体传给消息队列处理
            // 注意这里的有库存和已经下单都是缓存中的结论，存在不可靠性，在消息队列中会查表再次验证
            Log.Information("有库存：[{c}]", count);
            _orderService.CreateOrderByMq(sid, userId);
            return "秒杀请求提交成功";
        } catch (Exception e) {
            Log.Error("下单接口：异步处理订单异常：{e}", e.Message);
            return "秒杀请求失败，服务器正忙.....";
        }
    }
}