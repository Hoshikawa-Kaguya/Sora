namespace Sora.Command
{
    /// <summary>
    /// 用于快捷的创建对于CQ码适用的正则表达式
    /// </summary>
    public static class RegexBuilder
    {
        /// <summary>
        /// 用于匹配图片CQ码
        /// </summary>
        public static string Image()
            => @"^\[CQ:image,file=[a-z0-9]+\.image\]$";

        /// <summary>
        /// 用于匹配图片CQ码
        /// </summary>
        public static string Image(string imgId)
            => $@"^\[CQ:image,file={imgId}\]$";
    }
}