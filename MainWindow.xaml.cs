using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using WinRT.Interop;


namespace Snap_Erase
{
    public sealed partial class MainWindow : Window
    {
        private StorageFile currentImageFile;
        private MemoryStream processedImageStream;

        public MainWindow()
        {
            InitializeComponent();
            SetupClipboardAndDragDrop();

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var titleBar = appWindow.TitleBar;

            // Hide the icon (and system menu if desired)
            titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            // Optional: Also hide title text
            appWindow.Title = "";

            // Optional: Extend content into title bar for full control (hides default rendering)
            titleBar.ExtendsContentIntoTitleBar = true;
        }

        private void SetupClipboardAndDragDrop()
        {
            // Setup keyboard handling for Ctrl+V on the main content
            ContentGrid.KeyDown += ContentGrid_KeyDown;
            ContentGrid.IsTabStop = true; // Make it focusable to receive key events

            // Setup drag and drop for the main content area
            ContentGrid.AllowDrop = true;
            ContentGrid.DragEnter += ContentGrid_DragEnter;
            ContentGrid.DragOver += ContentGrid_DragOver;
            ContentGrid.DragLeave += ContentGrid_DragLeave;
            ContentGrid.Drop += ContentGrid_Drop;
        }

        private async void ContentGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle Ctrl+V for paste
            if (e.Key == VirtualKey.V &&
                (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)))
            {
                await HandlePasteFromClipboard();
                e.Handled = true;
            }
        }

        private async System.Threading.Tasks.Task HandlePasteFromClipboard()
        {
            try
            {
                var clipboardContent = Clipboard.GetContent();

                if (clipboardContent.Contains(StandardDataFormats.Bitmap))
                {
                    // Handle bitmap from clipboard
                    var bitmapReference = await clipboardContent.GetBitmapAsync();
                    using var stream = await bitmapReference.OpenReadAsync();

                    // Create temporary file for processing
                    var tempFile = await CreateTempFileFromStream(stream);
                    if (tempFile != null)
                    {
                        currentImageFile = tempFile;
                        await LoadAndDisplayImage(tempFile);

                        // Show success message
                        await ShowInfoDialog("Image pasted from clipboard!");
                    }
                }
                else if (clipboardContent.Contains(StandardDataFormats.StorageItems))
                {
                    // Handle file from clipboard (e.g., copied file from explorer)
                    var items = await clipboardContent.GetStorageItemsAsync();
                    var imageFile = items.OfType<StorageFile>()
                        .FirstOrDefault(f => IsImageFile(f.FileType));

                    if (imageFile != null)
                    {
                        currentImageFile = imageFile;
                        await LoadAndDisplayImage(imageFile);
                        await ShowInfoDialog("Image pasted from clipboard!");
                    }
                    else
                    {
                        await ShowErrorDialog("No supported image found in clipboard!");
                    }
                }
                else
                {
                    await ShowErrorDialog("No image found in clipboard!");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error pasting from clipboard: {ex.Message}");
            }
        }

        private void ContentGrid_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the dragged content contains files or images
            if (e.DataView.Contains(StandardDataFormats.StorageItems) ||
                e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;

                // Visual feedback - you could add a visual indicator here
                ContentGrid.Opacity = 0.8;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void ContentGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems) ||
                e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void ContentGrid_DragLeave(object sender, DragEventArgs e)
        {
            // Reset visual feedback
            ContentGrid.Opacity = 1.0;
        }

        private async void ContentGrid_Drop(object sender, DragEventArgs e)
        {
            // Reset visual feedback
            ContentGrid.Opacity = 1.0;

            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    // Handle dropped files
                    var items = await e.DataView.GetStorageItemsAsync();
                    var imageFile = items.OfType<StorageFile>()
                        .FirstOrDefault(f => IsImageFile(f.FileType));

                    if (imageFile != null)
                    {
                        currentImageFile = imageFile;
                        await LoadAndDisplayImage(imageFile);
                        await ShowInfoDialog("Image loaded successfully!");
                    }
                    else
                    {
                        await ShowErrorDialog("Please drop a supported image file (JPG, PNG, BMP)!");
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.Bitmap))
                {
                    // Handle dropped bitmap
                    var bitmapReference = await e.DataView.GetBitmapAsync();
                    using var stream = await bitmapReference.OpenReadAsync();

                    var tempFile = await CreateTempFileFromStream(stream);
                    if (tempFile != null)
                    {
                        currentImageFile = tempFile;
                        await LoadAndDisplayImage(tempFile);
                        await ShowInfoDialog("Image loaded successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error processing dropped content: {ex.Message}");
            }
        }

        private bool IsImageFile(string fileType)
        {
            var supportedTypes = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
            return supportedTypes.Contains(fileType.ToLower());
        }

        private async System.Threading.Tasks.Task<StorageFile> CreateTempFileFromStream(IRandomAccessStreamWithContentType stream)
        {
            try
            {
                // Create temporary file
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var tempFile = await tempFolder.CreateFileAsync(
                    $"clipboard_image_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    CreationCollisionOption.GenerateUniqueName);

                // Copy stream to temp file
                using var fileStream = await tempFile.OpenStreamForWriteAsync();
                using var inputStream = stream.AsStreamForRead();
                await inputStream.CopyToAsync(fileStream);

                return tempFile;
            }
            catch
            {
                return null;
            }
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create file picker
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".bmp");

                // Get window handle for picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // Pick file
                currentImageFile = await picker.PickSingleFileAsync();

                if (currentImageFile != null)
                {
                    await LoadAndDisplayImage(currentImageFile);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error selecting image: {ex.Message}");
            }
        }

        private async void CaptureImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use the professional built-in CameraCaptureUI
                var cameraCaptureUI = new Microsoft.Windows.Media.Capture.CameraCaptureUI(this.AppWindow.Id);

                // Configure photo settings
                cameraCaptureUI.PhotoSettings.Format = Microsoft.Windows.Media.Capture.CameraCaptureUIPhotoFormat.Jpeg;
                cameraCaptureUI.PhotoSettings.AllowCropping = true; // Allows user to crop after capture

                // Capture photo with built-in preview, proceed/retake UI
                StorageFile photo = await cameraCaptureUI.CaptureFileAsync(Microsoft.Windows.Media.Capture.CameraCaptureUIMode.Photo);

                if (photo != null)
                {
                    // Photo capture was successful (user chose "Use Photo")
                    currentImageFile = photo;
                    await LoadAndDisplayImage(photo);
                }
                // If photo is null, user either cancelled or chose "Retake"
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error with camera: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadAndDisplayImage(StorageFile imageFile)
        {
            // Load and display image
            using var stream = await imageFile.OpenAsync(FileAccessMode.Read);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);

            OriginalImage.Source = bitmap;
            OriginalImage.Visibility = Visibility.Visible;
            OriginalPlaceholder.Visibility = Visibility.Collapsed;

            // Enable remove background button
            RemoveBackgroundButton.IsEnabled = true;

            // Reset result side
            ResultImage.Visibility = Visibility.Collapsed;
            ResultPlaceholder.Visibility = Visibility.Visible;
            SaveImageButton.IsEnabled = false;

            // Clear any previous processed image
            processedImageStream?.Dispose();
            processedImageStream = null;
        }

        private async void RemoveBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentImageFile == null) return;

            try
            {
                // Show processing indicator
                ProcessingIndicator.Visibility = Visibility.Visible;
                ResultPlaceholder.Visibility = Visibility.Collapsed;
                RemoveBackgroundButton.IsEnabled = false;

                // Process image with U2Net AI model
                processedImageStream = await ProcessImageWithU2Net(currentImageFile);

                if (processedImageStream != null)
                {
                    // Display result
                    var bitmap = new BitmapImage();
                    processedImageStream.Seek(0, SeekOrigin.Begin);
                    await bitmap.SetSourceAsync(processedImageStream.AsRandomAccessStream());

                    ResultImage.Source = bitmap;
                    ResultImage.Visibility = Visibility.Visible;
                    CheckerboardBackground.Visibility = Visibility.Visible;
                    SaveImageButton.IsEnabled = true;
                }

                ProcessingIndicator.Visibility = Visibility.Collapsed;
                RemoveBackgroundButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ProcessingIndicator.Visibility = Visibility.Collapsed;
                ResultPlaceholder.Visibility = Visibility.Visible;
                RemoveBackgroundButton.IsEnabled = true;
                await ShowErrorDialog($"Error processing image: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task<MemoryStream> ProcessImageWithU2Net(StorageFile imageFile)
        {
            try
            {
                // Load the image
                using var fileStream = await imageFile.OpenStreamForReadAsync();
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(fileStream);

                // Resize to 320x320 (U2Net input size)
                var originalWidth = image.Width;
                var originalHeight = image.Height;
                image.Mutate(x => x.Resize(320, 320));

                // Convert to tensor
                var input = new DenseTensor<float>(new[] { 1, 3, 320, 320 });

                // Normalize and populate tensor
                for (int y = 0; y < 320; y++)
                {
                    for (int x = 0; x < 320; x++)
                    {
                        var pixel = image[x, y];
                        input[0, 0, y, x] = (pixel.R / 255f - 0.485f) / 0.229f; // Red
                        input[0, 1, y, x] = (pixel.G / 255f - 0.456f) / 0.224f; // Green  
                        input[0, 2, y, x] = (pixel.B / 255f - 0.406f) / 0.225f; // Blue
                    }
                }

                // Run inference
                var modelPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "u2net.onnx");
                using var session = new InferenceSession(modelPath);

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input", input)
                };

                using var results = session.Run(inputs);
                var output = results.First().AsTensor<float>();

                // Process mask
                var mask = new float[320, 320];
                for (int y = 0; y < 320; y++)
                {
                    for (int x = 0; x < 320; x++)
                    {
                        mask[y, x] = Math.Max(0, Math.Min(1, output[0, 0, y, x])); // Normalize
                    }
                }

                // Load original image again for processing
                fileStream.Seek(0, SeekOrigin.Begin);
                using var originalImage = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream);

                // Resize mask to original dimensions
                using var maskImage = new SixLabors.ImageSharp.Image<L8>(320, 320);
                for (int y = 0; y < 320; y++)
                {
                    for (int x = 0; x < 320; x++)
                    {
                        maskImage[x, y] = new L8((byte)(mask[y, x] * 255));
                    }
                }

                maskImage.Mutate(x => x.Resize(originalWidth, originalHeight));

                // Apply mask to create transparent background
                for (int y = 0; y < originalImage.Height; y++)
                {
                    for (int x = 0; x < originalImage.Width; x++)
                    {
                        var maskValue = maskImage[x, y].PackedValue / 255f;
                        var pixel = originalImage[x, y];
                        originalImage[x, y] = new Rgba32(pixel.R, pixel.G, pixel.B, (byte)(maskValue * 255));
                    }
                }

                // Save to memory stream
                var resultStream = new MemoryStream();
                await originalImage.SaveAsPngAsync(resultStream);
                return resultStream;
            }
            catch (Exception ex)
            {
                throw new Exception($"U2Net processing failed: {ex.Message}");
            }
        }

        private async void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (processedImageStream == null)
                {
                    await ShowErrorDialog("No processed image to save!");
                    return;
                }

                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add("PNG Image", new[] { ".png" });
                picker.SuggestedFileName = $"background_removed_{DateTime.Now:yyyyMMdd_HHmmss}";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    // Save the processed image
                    processedImageStream.Seek(0, SeekOrigin.Begin);
                    using var fileStream = await file.OpenStreamForWriteAsync();
                    await processedImageStream.CopyToAsync(fileStream);

                    await ShowInfoDialog("Image saved successfully!");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error saving image: {ex.Message}");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset everything
            currentImageFile = null;
            processedImageStream?.Dispose();
            processedImageStream = null;

            OriginalImage.Source = null;
            OriginalImage.Visibility = Visibility.Collapsed;
            OriginalPlaceholder.Visibility = Visibility.Visible;

            ResultImage.Source = null;
            ResultImage.Visibility = Visibility.Collapsed;
            ResultPlaceholder.Visibility = Visibility.Visible;
            ProcessingIndicator.Visibility = Visibility.Collapsed;
            CheckerboardBackground.Visibility = Visibility.Collapsed;

            RemoveBackgroundButton.IsEnabled = false;
            SaveImageButton.IsEnabled = false;
        }

        private async System.Threading.Tasks.Task ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async System.Threading.Tasks.Task ShowInfoDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();

        }
    }
}