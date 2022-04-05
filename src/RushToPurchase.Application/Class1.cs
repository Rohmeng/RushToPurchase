namespace RushToPurchase.Application;
public class Class1
{
    // 应用服务(Application Service): 应用服务是为实现用例的无状态服务.展现层调用应用服务获取DTO.应用服务调用多个领域服务实现用例.用例通常被视为一个工作单元.
    // 数据传输对象(DTO): DTO是一个不含业务逻辑的简单对象,用于应用服务层与展现层间的数据传输.
    // 工作单元(UOW): 工作单元是事务的原子操作.UOW内所有操作,当成功时全部提交,失败时全部回滚.
}
