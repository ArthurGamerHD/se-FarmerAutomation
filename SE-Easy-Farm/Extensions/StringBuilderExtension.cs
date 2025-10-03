using System.Text;
using VRage;

namespace EasyFarming.Extensions
{
    public static class StringBuilderExtension
    {
        public static void AppendError(this StringBuilder sb, string text)
        {
            sb.AppendError($"{MyTexts.GetString("AIBlocks_HudMessage_Error")}", text);
        }
        
        public static void AppendWarning(this StringBuilder sb, string text)
        {
            sb.AppendWarning($"{MyTexts.GetString("MessageBox_Caption_NotFullyGrownWarning")}", text);
        }
        
        public static void AppendError(this StringBuilder sb, string title, string text)
        {
            sb.AppendLine($"[Color=#FFFF0000]{title}: {text}[/Color]");
        }
        
        public static void AppendWarning(this StringBuilder sb, string title, string text)
        {
            sb.AppendLine($"[Color=#FFFFFF00]{title}: {text}[/Color]");
        }
    }
}