using RushToPurchase.Domain.Entities;

namespace RushToPurchase.Domain.Interfaces;

public interface IOrderService
{
    
    /**
     * 创建错误订单
     * @param sid
     *  库存ID
     * @return
     *  订单ID
     */
    public Task<int> CreateWrongOrder(int sid);


    /**
     * 创建正确订单：下单乐观锁
     * @param sid
     * @return
     * @throws Exception
     */
    public Task<int> CreateOptimisticOrder(int sid);

    /**
     * 创建正确订单：下单悲观锁 for update
     * @param sid
     * @return
     * @throws Exception
     */
    public Task<int> CreatePessimisticOrder(int sid);

    /**
     * 创建正确订单：验证库存 + 用户 + 时间 合法性 + 下单乐观锁
     * @param sid
     * @param userId
     * @param verifyHash
     * @return
     * @throws Exception
     */
    public Task<int> CreateVerifiedOrder(int sid, int userId, string verifyHash);

    /**
     * 创建正确订单：验证库存 + 下单乐观锁 + 更新订单信息到缓存
     * @param sid
     * @param userId
     * @throws Exception
     */
    public void CreateOrderByMq(int sid, int userId);

    public void CreateOrderByMqConsumer(int sid, int userId);

    /**
     * 检查缓存中用户是否已经有订单
     * @param sid
     * @param userId
     * @return
     * @throws Exception
     */
    public bool CheckUserOrderInfoInCache(int sid, int userId);

    
    
    /**
     * 查询库存：通过缓存查询库存
     * 缓存命中：返回库存
     * 缓存未命中：查询数据库写入缓存并返回
     * @param id
     * @return
     */
    int? GetStockCount(int id);

    /**
     * 获取剩余库存：查数据库
     * @param id
     * @return
     */
    int? GetStockCountByDb(int id);

    /**
     * 获取剩余库存: 查缓存
     * @param id
     * @return
     */
    int? GetStockCountByCache(int id);

    /**
     * 将库存插入缓存
     * @param id
     * @return
     */
    void SetStockCountCache(int id, int? count);

    /**
     * 删除库存缓存
     * @param id
     */
    void DelStockCountCache(int id, bool mq = false);

    /**
     * 根据库存 ID 查询数据库库存信息
     * @param id
     * @return
     */
    Task<Stock> GetStockById(int id);

    /**
     * 根据库存 ID 查询数据库库存信息（悲观锁）
     * @param id
     * @return
     */
    Stock GetStockByIdForUpdate(int id);

    /**
     * 更新数据库库存信息
     * @param stock
     * return
     */
    Task UpdateStockById(Stock stock);

    /**
     * 更新数据库库存信息（乐观锁）
     * @param stock
     * @return
     */
    int UpdateStockByOptimistic(Stock stock);

    void KeyDelMessage(string key);

}