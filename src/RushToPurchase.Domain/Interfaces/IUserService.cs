namespace RushToPurchase.Domain.Interfaces;

public interface IUserService
{
    /**
     * 获取用户验证Hash
     * @param sid
     * @param userId
     * @return
     * @throws Exception
     */
    public Task<string> GetVerifyHash(int sid, int userId);

    /**
     * 添加用户访问次数
     * @param userId
     * @return
     * @throws Exception
     */
    public Task<long> AddUserCount(int userId);

    /**
     * 检查用户是否被禁
     * @param userId
     * @return
     */
    public bool GetUserIsBanned(int userId);
}