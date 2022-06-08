namespace Sora.Entities.Info;

/// <summary>
/// 个人资料
/// </summary>
public struct ProfileDetail
{
    /// <summary>
    /// 
    /// </summary>
    public ProfileDetail()
    {
    }

    /// <summary>
    /// 昵称
    /// </summary>
    public string Nick { get; init; } = string.Empty;

    /// <summary>
    /// 公司
    /// </summary>
    public string Company { get; init; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 学校
    /// </summary>
    public string College { get; init; } = string.Empty;

    /// <summary>
    /// 个人说明
    /// </summary>
    public string PersonalNote { get; init; } = string.Empty;
}