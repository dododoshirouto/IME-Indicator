using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using BarRaider.SdTools;

namespace ImeIndicator.Actions
{
    [PluginActionId("site.dodoneko.imeindicator.action")]
    public class ImeIndicatorAction : KeypadBase
    {
        private readonly System.Timers.Timer timer;
        private string lastTitle = string.Empty;

        private int lastState = -1;
        private const int STATE_A = 0;
        private const int STATE_JA = 1;

        public ImeIndicatorAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            // 250ms間隔でポーリング（必要ならプロパティで変更可能にしてね）
            timer = new System.Timers.Timer(250);
            timer.Elapsed += async (s, e) => await UpdateTitleAsync();
            timer.AutoReset = true;
            timer.Start();
        }

        public override void KeyPressed(KeyPayload payload) { }
        public override void KeyReleased(KeyPayload payload) { }
        public override void OnTick() { }

        public override void Dispose()
        {
            timer?.Stop();
            timer?.Dispose();
            // base.Dispose();
        }

        private async Task UpdateTitleAsync()
        {
            int state = GetImeState(); // 0 or 1 を返す
            if (state != lastState && state >= 0)
            {
                lastState = state;
                await Connection.SetStateAsync((uint)state);
                await Connection.SetTitleAsync(""); // 文字は消して画像だけに
            }
        }
        private int GetImeState()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return lastState;

            IntPtr hImc = ImmGetContext(hwnd);
            if (hImc == IntPtr.Zero) return lastState;

            bool open = ImmGetOpenStatus(hImc);
            int conv = 0, sent = 0;
            ImmGetConversionStatus(hImc, out conv, out sent);
            ImmReleaseContext(hwnd, hImc);

            if (!open) return STATE_A;
            return (conv & IME_CMODE_NATIVE) != 0 ? STATE_JA : STATE_A;
        }

        private string GetImeTitleSafe()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return lastTitle;

                IntPtr hImc = ImmGetContext(hwnd);
                if (hImc == IntPtr.Zero)
                {
                    // IMM32を取れないアプリの場合は直前値を維持
                    return lastTitle;
                }

                bool open = ImmGetOpenStatus(hImc);
                int conv = 0, sent = 0;
                ImmGetConversionStatus(hImc, out conv, out sent);
                ImmReleaseContext(hwnd, hImc);

                // シンプル運用：IME開＝"あ"、閉＝"A"
                if (!open)
                    return "A";

                // もう少し厳密に：ネイティブ入力（かな/漢字）で"あ"、それ以外は"A"
                if ((conv & IME_CMODE_NATIVE) != 0)
                    return "あ";

                return "A";
            }
            catch
            {
                // 例外時は前回値を維持
                return lastTitle;
            }
        }

        #region Win32/IMM32
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("imm32.dll", SetLastError = true)]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmGetOpenStatus(IntPtr hIMC);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmGetConversionStatus(IntPtr hIMC, out int fdwConversion, out int fdwSentence);

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            throw new NotImplementedException();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            throw new NotImplementedException();
        }

        private const int IME_CMODE_NATIVE = 0x0001;       // かな/漢字（ネイティブ）
        private const int IME_CMODE_KATAKANA = 0x0002;     // カタカナ
        private const int IME_CMODE_FULLSHAPE = 0x0008;    // 全角
        #endregion
    }
}
