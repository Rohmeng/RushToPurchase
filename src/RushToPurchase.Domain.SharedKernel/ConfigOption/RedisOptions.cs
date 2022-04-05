namespace RushToPurchase.Domain.SharedKernel.ConfigOption;

public class RedisOptions
{
    public bool RedisIsEnabled { get; set; }

    /// <summary>
    /// Gets or sets redis connection string
    /// </summary>
    public string? ConnectionString { get; set; }
}