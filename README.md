# ファイル構成

```bash
ImeIndicator/
├─ manifest.json
├─ ImeIndicator.csproj
├─ Program.cs
└─ Actions/
   └─ ImeIndicatorAction.cs
```

---

## manifest.json

```json
{
  "Actions": [
    {
      "Icon": "images/icon",
      "Name": "IME Indicator",
      "States": [
        {
          "Image": "images/icon",
          "TitleAlignment": "middle",
          "FontSize": "18"
        }
      ],
      "SupportedInMultiActions": true,
      "Tooltip": "Shows current IME mode (A/あ)",
      "UUID": "site.dodoneko.imeindicator.action"
    }
  ],
  "Author": "dodo",
  "Category": "Utilities",
  "CategoryIcon": "images/icon",
  "CodePathWin": "ImeIndicator.exe",
  "Description": "Displays current input mode: A (IME off / alnum) or あ (IME native)",
  "Name": "IME Indicator (A/あ)",
  "Icon": "images/icon",
  "URL": "",
  "Version": "1.0.0",
  "OS": [
    {
      "Platform": "windows",
      "MinimumVersion": "10"
    }
  ],
  "SDKVersion": 2
}
```

---

## ImeIndicator.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWindowsForms>false</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>ImeIndicator</AssemblyName>
    <RootNamespace>ImeIndicator</RootNamespace>
    <Platforms>x64</Platforms>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BarRaider.SdTools" Version="3.8.1" />
  </ItemGroup>
</Project>
```

> BarRaider.SdTools のバージョンは手元の環境に合わせて更新してね。

---

## Program.cs

```csharp
using BarRaider.SdTools;

namespace ImeIndicator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            SDWrapper.Run(args);
        }
    }
}
```

---

## Actions/ImeIndicatorAction.cs

```csharp
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
        private readonly Timer timer;
        private string lastTitle = string.Empty;

        public ImeIndicatorAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            // 250ms間隔でポーリング（必要ならプロパティで変更可能にしてね）
            timer = new Timer(250);
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
            base.Dispose();
        }

        private async Task UpdateTitleAsync()
        {
            string title = GetImeTitleSafe();
            if (title != lastTitle && !string.IsNullOrEmpty(title))
            {
                lastTitle = title;
                await Connection.SetTitleAsync(title);
            }
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

        private const int IME_CMODE_NATIVE = 0x0001;       // かな/漢字（ネイティブ）
        private const int IME_CMODE_KATAKANA = 0x0002;     // カタカナ
        private const int IME_CMODE_FULLSHAPE = 0x0008;    // 全角
        #endregion
    }
}
```

---

## つかいかた（超要約）

1. Visual Studio 2022 で新規ソリューションに上記ファイルを配置、NuGetで `BarRaider.SdTools` を入れる。
2. `Release | x64` でビルドして `ImeIndicator.exe` を得る。
3. `manifest.json` と `ImeIndicator.exe`、`images/` をフォルダにまとめ、フォルダ名を `site.dodoneko.imeindicator.sdPlugin` にする。
4. `site.dodoneko.imeindicator.sdPlugin` を `%appdata%/Elgato/StreamDeck/Plugins/` に配置（Stream Deckアプリ再起動）。
5. キーに「IME Indicator」を置くと、タスクのフォアグラウンドに応じて A / あ を表示。

---

## 注意・拡張メモ

- **精度**: `ImmGetOpenStatus` は多くのアプリで機能するけど、UWPや一部モダンアプリでは `HIMC` が取れないことがある。その場合は直前値を維持。必要なら `ImmGetDefaultIMEWnd` + `WM_IME_CONTROL(IMC_GETOPENSTATUS)` のフォールバック、あるいは TSF の `GUID_COMPARTMENT_KEYBOARD_OPENCLOSE` をCOMで読むv2を検討。
- **更新間隔**: 250msは目視で遅延を感じにくい実用値。バッテリー・CPU配慮で 500ms でもOK。
- **タイトル以外**: `SetImageAsync` でA/あアイコンを切替も可。DPIに注意。
- **表示の厳密化**: かな/カナ/英数の判別までやるなら `IME_CMODE_KATAKANA` 等を見て「カ」「あ」「A」で出し分け。
- **トグル**: キー押下で `PostMessage` による IME ON/OFF トグル（Alt+` 相当）も実装できるけど、アプリごとに挙動差が出るので既定は表示のみ推奨。
