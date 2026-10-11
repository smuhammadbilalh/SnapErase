<div align="center">

# ✂️ SnapErase

### Remove image backgrounds on Windows — 100% offline, powered by on-device AI

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0a0a0a?style=for-the-badge)](#-requirements)
[![WinUI 3](https://img.shields.io/badge/WinUI%203-Windows%20App%20SDK-0a0a0a?style=for-the-badge&logo=dotnet&logoColor=fafafa)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-0a0a0a?style=for-the-badge&logo=dotnet&logoColor=fafafa)](https://dotnet.microsoft.com/)
[![ONNX Runtime](https://img.shields.io/badge/ONNX%20Runtime-U%C2%B2--Net-0a0a0a?style=for-the-badge&logo=onnx&logoColor=fafafa)](https://onnxruntime.ai/)
[![Stars](https://img.shields.io/github/stars/smuhammadbilalh/SnapErase?style=for-the-badge&color=0a0a0a)](https://github.com/smuhammadbilalh/SnapErase/stargazers)

*Your photos never leave your PC. No accounts, no uploads, no credits, no internet required.*

</div>

---

## ✨ Why SnapErase?

Most background removers send your images to a cloud server, cap you at a few free images, and need an internet connection. **SnapErase runs the [U²-Net](https://github.com/xuebinqin/U-2-Net) salient-object-detection model locally with ONNX Runtime**, so you get studio-quality cutouts instantly and privately — even on a plane.

## 🚀 Features

| | Feature | Details |
|---|---|---|
| 🧠 | **On-device AI** | Bundled `u2net.onnx` model runs through ONNX Runtime — no API keys, no cloud |
| 📋 | **Paste to erase** | Press <kbd>Ctrl</kbd> + <kbd>V</kbd> anywhere in the window to process an image from the clipboard |
| 🖱️ | **Drag & drop** | Drop any image file straight onto the app |
| 📂 | **File picker** | Classic *Open* dialog for browsing to a photo |
| 🪄 | **Transparent PNG output** | Clean alpha-matted result ready for design work, product listings or avatars |
| 💾 | **Save & copy** | Export to PNG or copy the result back to the clipboard |
| 🎨 | **Modern Windows UI** | Fluent design, custom title bar, light/dark theme aware (WinUI 3) |
| 🖥️ | **x86 · x64 · ARM64** | Native builds for every Windows architecture, MSIX-packaged |

## 🧩 How It Works

```
Image ──▶ Resize to 320×320 ──▶ Normalize ──▶ U²-Net (ONNX Runtime) ──▶ Saliency mask
                                                                           │
Transparent PNG ◀── Alpha-blend original with mask ◀── Upscale mask ◀──────┘
```

- **Inference:** `Microsoft.ML.OnnxRuntime` executes the U²-Net graph on the CPU.
- **Image processing:** `SixLabors.ImageSharp` handles decoding, resizing, normalization and compositing.
- **UI & I/O:** WinUI 3 / Windows App SDK with `StorageFile`, clipboard and drag-drop APIs.

## 🛠️ Tech Stack

- **Language:** C# 12 / .NET 8
- **UI:** WinUI 3 (Windows App SDK), XAML, Fluent Design
- **AI:** ONNX Runtime + U²-Net model
- **Imaging:** SixLabors.ImageSharp
- **Packaging:** MSIX (single-project)

## 📋 Requirements

- Windows 10 version 1809 (build 17763) or later, or Windows 11
- To build: **Visual Studio 2022** with the *.NET Desktop Development* and *Windows App SDK* workloads

## ⚡ Getting Started

```bash
git clone https://github.com/smuhammadbilalh/SnapErase.git
cd SnapErase
```

1. Open `Snap Erase.sln` in Visual Studio 2022.
2. Select your platform (`x64` is recommended) and press **F5**.
3. Paste, drop or open an image — the background disappears in seconds.

> **Note:** The `u2net.onnx` model ships inside the repository, so the app works out of the box with no extra downloads.

## 🗺️ Roadmap

- [ ] GPU acceleration (DirectML execution provider)
- [ ] Batch processing of multiple images
- [ ] Background colour / replacement image options
- [ ] Microsoft Store listing

## 🤝 Contributing

Issues and pull requests are welcome. If you have an idea, open an issue and let's talk about it.

---

## 👨‍💻 Author

<div align="center">

**Muhammad Bilal** — IT Specialist & App Developer · Gujrat, Pakistan

[![GitHub](https://img.shields.io/badge/GitHub-smuhammadbilalh-0a0a0a?style=for-the-badge&logo=github)](https://github.com/smuhammadbilalh)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-muhammadbilaltech-0a0a0a?style=for-the-badge)](https://www.linkedin.com/in/muhammadbilaltech)
[![Email](https://img.shields.io/badge/Email-smuhammadbilalh%40gmail.com-0a0a0a?style=for-the-badge&logo=gmail&logoColor=fafafa)](mailto:smuhammadbilalh@gmail.com)

⭐ If this project helped you, please consider giving it a star!

</div>
