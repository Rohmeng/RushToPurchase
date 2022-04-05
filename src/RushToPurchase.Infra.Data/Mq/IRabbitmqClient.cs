namespace RushToPurchase.Infra.Data.Mq;

public interface IRabbitmqClient
{
    void DeleteCacheKey(string key);
    void CreateOrder(int sid, int uid);
}