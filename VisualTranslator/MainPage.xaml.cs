﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VisualTranslator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Microsoft cognitive service - Translator Text and Text Analysis Api
        // The subscriptionKey string key and the uri base
        private const string accesskeyTL = "34e85ab9d8624f6eafe5e44cccf1a38d";                
        private const string uriBaseTranslation = "https://api.microsofttranslator.com/V2/Http.svc/";
        private const string accesskeyTA = "71c90e3bc74b4548878e5d4d2f5c0b1e";
        private const string uriBaseAnalysis = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/";

        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;
        private VideoEncodingProperties _previewProperties;
        private bool _isPreviewing;

        // Language for OCR.
        private Language _ocrLanguage = new Language("en");

        // Recognized words ovelay boxes.
        private List<WordOverlay> _wordBoxes = new List<WordOverlay>();

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        // Display size        
        private int _videoFrameWidth;
        private int _videoFrameHeight;

        // Bitmap holder
        private SoftwareBitmap _softwareBitmap;        

        // Image byte array
        private byte[] _byteData;
        
        private OcrEngine _engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
        
        // array of language codes
        private string[] _languageCodes;

        // Dictionary to map language code from friendly name (sorted case-insensitively on language name)
        private SortedDictionary<string, string> _languageCodesAndTitles =
            new SortedDictionary<string, string>(Comparer<string>.Create((a, b) => string.Compare(a, b, true)));


        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            this.InitializeComponent();
            this.ViewModel = new ImageInfoViewModel();
            Application.Current.Suspending += Application_Suspending;
        }
        public ImageInfoViewModel ViewModel { get; set; }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await InitialiseCameraAsync();
                deferral.Complete();
            }
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!OcrEngine.IsLanguageSupported(_ocrLanguage))
            {
                this.NotifyUser(_ocrLanguage.DisplayName + " is not supported.", NotifyType.ErrorMessage);

                return;
            }

            await InitialiseCameraAsync();
            await GetLanguagesForTranslate(); // Get codes of languages that can be translated
            await GetLanguageNames(); // Get friendly names of languages
            PopulateLanguageMenus(); // Fill the drop-down language lists
        }

        #endregion Constructor, lifecycle and navigation



        #region Event handlers

        private async void PreviewMediaButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await InitialiseCameraAsync();

            // Visibility change
            ImagePreview.Visibility = Visibility.Visible;
            ImageView.Visibility = Visibility.Collapsed;
            //processConfirmButton.Visibility = Visibility.Collapsed;
            //processCancelButton.Visibility = Visibility.Collapsed;
        }
        private async void OpenFileButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" },
            };

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                await CleanupPreviewAndBitmapAsync();
                await LoadImageAsync(file);

                // Visibility change
                ImagePreview.Visibility = Visibility.Collapsed;
                ImageView.Visibility = Visibility.Visible;
                //processConfirmButton.Visibility = Visibility.Visible;
                //processCancelButton.Visibility = Visibility.Visible;
            }
        }
        private async void QueryImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Capture the image frame, if previewing
            if (_isPreviewing)
            {
                await CaptureImageAsync();
            }

            // Before request start, show progress ring                                                
            ProgressBackground.Width = OCRControlPanel.ActualWidth;
            ProgressBackground.Height = OCRControlPanel.ActualHeight;
            ProgresRing.IsActive = true;
            ProgressControlPanel.Visibility = Visibility.Visible;            

            // Start managing bitmap image source
            // Get image analysis as string
            //var imageAnalysis = await MakeAnalysisRequest(_byteData);
            //var searchResult = BingImageSearch(imageAnalysis);

            var ocrResult = await ProcessOCRAsync(_softwareBitmap);
            await ProcessTranslationAsync(ocrResult);
            // 
            //ProcessSearchResult(searchResult);

            // Change/Update visibility                        
            ProgresRing.IsActive = false;
            ProgressControlPanel.Visibility = Visibility.Collapsed;


            // Visibility change
            ImagePreview.Visibility = Visibility.Collapsed;
            ImageView.Visibility = Visibility.Visible;
            //processConfirmButton.Visibility = Visibility.Visible;
            //processCancelButton.Visibility = Visibility.Visible;
        }        
        private async void CloudButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // upload image file to azure cloud storage and other services
            // *azure storage
            // *customm computer vision service
            // is previewing?

        }
        //private async void processConfirmButton_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    // Before request start, show progress ring
        //    ProgressControlPanel.Visibility = Visibility.Visible;
        //    ProgresRing.IsActive = true;

        //    // Start managing image source
        //    // Get image analysis as string
        //    var imageAnalysis = await MakeAnalysisRequest(_byteData);
        //    var searchResult = BingImageSearch(imageAnalysis);

        //    // 
        //    ProcessSearchResult(searchResult);

        //    // Change/Update visibility
        //    //processConfirmButton.Visibility = Visibility.Collapsed;
        //    //processCancelButton.Visibility = Visibility.Collapsed;
        //    ProgressControlPanel.Visibility = Visibility.Collapsed; ;
        //    ProgresRing.IsActive = false;
        //}
        private async void processCancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Process Cancelled");
            await CleanupPreviewAndBitmapAsync();
        }
        private void ImageItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Move to the detail page
            //this.Frame.Navigate(typeof(Page1));
            Debug.WriteLine("ImageInfoGridView tapped");
        }
        private void Rect_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            //rectangle.Opacity = 0.5;
        }
        private void Rect_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //, PointerRoutedEventArgs e2
            //_pt = e2.GetCurrentPoint(this);
            // translate
            //rectTransform.TranslateX += e.Delta.Translation.X;
            //rectTransform.TranslateY += e.Delta.Translation.Y;
            // scale
            //rectTransform.ScaleX += 0.01;
            //rectTransform.ScaleY += 0.01;
        }
        private void Rect_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            //rectangle.Opacity = 0.3;
        }

        #endregion Event handlers



        #region MediaCapture methods

        private async Task InitialiseCameraAsync()
        {
            await CleanupPreviewAndBitmapAsync();

            if (_mediaCapture == null)
            {
                await StartPreviewAsync();
            }

        }
        // Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on
        private async Task StartPreviewAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { });
                var previewResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
                var photoResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);

                VideoEncodingProperties allResolutionsAvailable;
                uint height, width;
                //use debugger at the following line to check height & width for video preview resolution
                for (int i = 0; i < previewResolution.Count; i++)
                {
                    allResolutionsAvailable = previewResolution[i] as VideoEncodingProperties;
                    height = allResolutionsAvailable.Height;
                    width = allResolutionsAvailable.Width;
                }
                //use debugger at the following line to check height & width for captured photo resolution
                for (int i = 0; i < photoResolution.Count; i++)
                {
                    allResolutionsAvailable = photoResolution[i] as VideoEncodingProperties;
                    height = allResolutionsAvailable.Height;
                    width = allResolutionsAvailable.Width;
                }

                // Prevent the device from sleeping while previewing
                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                // Video Preview resolution 8-th Height: 480, Width: 640
                // Captured Photo resolution 8-th Height: 480, Width: 640
                var selectedPreviewResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).ElementAt(8);
                var selectedPhotoResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).ElementAt(8);

                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, selectedPreviewResolution);
                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, selectedPhotoResolution);

                // Get information about the current preview
                _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                // Set the camera preview and the size
                ImagePreview.Source = _mediaCapture;

                _videoFrameHeight = (int)_previewProperties.Height;
                _videoFrameWidth = (int)_previewProperties.Width;

                // Set the root grid size as previewing size
                //ApplicationView.GetForCurrentView().TryResizeView(new Size
                //{
                //    Height = _videoFrameHeight + commandBarPanel.ActualHeight,
                //    Width = _videoFrameWidth
                //});

                await _mediaCapture.StartPreviewAsync();

                _isPreviewing = true;
                NotifyUser("Camera started.", NotifyType.StatusMessage);
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                Debug.WriteLine("The app was denied access to the camera");
                return;
            }
            catch (System.IO.FileLoadException)
            {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }
        private async Task CleanupPreviewAndBitmapAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // Cleanup the UI
                    OCRTextOverlay.Children.Clear();
                    _wordBoxes.Clear();
                    ImagePreview.Source = null;                    

                    if (_displayRequest != null)
                    {
                        // Allow the device screen to sleep now that the preview is stopped
                        _displayRequest.RequestRelease();
                    }

                    // Cleanup the media capture
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });

                _isPreviewing = false;
            }

            if (_softwareBitmap != null)
            {
                _softwareBitmap.Dispose();
                _softwareBitmap = null;
            }
        }
        private async Task CaptureImageAsync()
        {
            // Create the video frame to request a SoftwareBitmap preview frame.
            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, _videoFrameWidth, _videoFrameHeight);

            // Capture the preview frame.
            using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
            {
                // Create softwarebitmap from current video frame
                _softwareBitmap = currentFrame.SoftwareBitmap;
                // Set image source
                await SetImageViewSource(_softwareBitmap);
            }
        }
        private async Task LoadImageAsync(StorageFile file)
        {
            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(fileStream);
                // Create software bitmap from file stream
                _softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                // Set image source
                await SetImageViewSource(_softwareBitmap);
            }
        }

        #endregion MediaCapture methods



        #region Helper functions

        // Set the image view source
        private async Task SetImageViewSource(SoftwareBitmap softwareBitmap)
        {
            // Get byte array from software bitmap
            _byteData = await EncodedBytes(softwareBitmap, BitmapEncoder.JpegEncoderId);
            // Create writeable bitmap from software bitmap
            var imgSource = new WriteableBitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
            // Copy software bitmap buffer to writeable bitmap
            softwareBitmap.CopyToBuffer(imgSource.PixelBuffer);
            // Set UI source source
            ImageView.Source = imgSource;
            // Set OCR view source
            OCRImageView.Source = imgSource;
        }
        // Convert bitmap to byte array
        private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
        {
            byte[] array = null;

            // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
            // Next:  Use ReadAsync on the in-mem stream to get byte[] array

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex) { return new byte[0]; }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }
            return array;
        }
        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                Debug.WriteLine("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !_isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }
        // Gets the tanslation of texts using the Computer Vision REST API.                                        
        private async Task<string> ProcessOCRAsync(SoftwareBitmap softwareBitmap)
        {
            OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(_ocrLanguage);

            if (ocrEngine == null)
            {
                NotifyUser(_ocrLanguage.DisplayName + " is not supported.", NotifyType.ErrorMessage);

                return string.Empty;
            }
        
            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            // Used for text overlay.
            // Prepare scale transform for words since image is not displayed in original format.
            var scaleTrasform = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = ImagePreview.ActualWidth / softwareBitmap.PixelWidth,
                ScaleY = ImagePreview.ActualHeight / softwareBitmap.PixelHeight
            };

            if (ocrResult.TextAngle != null)
            {
                // If text is detected under some angle in this sample scenario we want to
                // overlay word boxes over original image, so we rotate overlay boxes.
                OCRTextOverlay.RenderTransform = new RotateTransform
                {
                    Angle = (double)ocrResult.TextAngle,
                    CenterX = OCRImageView.ActualWidth / 2,
                    CenterY = OCRImageView.ActualHeight / 2
                };
            }

            // Iterate over recognized lines of text.
            foreach (var line in ocrResult.Lines)
            {
                // Iterate over words in line.
                foreach (var word in line.Words)
                {
                    // Define the TextBlock.
                    var wordTextBlock = new TextBlock()
                    {
                        Text = word.Text,
                        Style = (Style)this.Resources["ExtractedWordTextStyle"]
                    };

                    WordOverlay wordBoxOverlay = new WordOverlay(word);

                    // Keep references to word boxes.
                    _wordBoxes.Add(wordBoxOverlay);

                    // Define position, background, etc.
                    var overlay = new Border()
                    {
                        Child = wordTextBlock,
                        Style = (Style)this.Resources["HighlightedWordBoxHorizontalLine"]
                    };

                    // Bind word boxes to UI.
                    overlay.SetBinding(Border.MarginProperty, wordBoxOverlay.CreateWordPositionBinding());
                    overlay.SetBinding(Border.WidthProperty, wordBoxOverlay.CreateWordWidthBinding());
                    overlay.SetBinding(Border.HeightProperty, wordBoxOverlay.CreateWordHeightBinding());

                    // Put the filled textblock in the results grid.
                    OCRTextOverlay.Children.Add(overlay);
                }
            }
            // Get original text recognised
            OriginalTextBlock.Text = ocrResult.Text;
            NotifyUser("Image processed using " + ocrEngine.RecognizerLanguage.DisplayName + " language.", NotifyType.StatusMessage);

            UpdateWordBoxTransform();

            return ocrResult.Text;
        }
        private void UpdateWordBoxTransform()
        {
            WriteableBitmap bitmap = OCRImageView.Source as WriteableBitmap;

            if (bitmap != null)
            {
                // Used for text overlay.
                // Prepare scale transform for words since image is not displayed in original size.
                ScaleTransform scaleTrasform = new ScaleTransform
                {
                    CenterX = 0,
                    CenterY = 0,
                    ScaleX = OCRImageView.ActualWidth / bitmap.PixelWidth,
                    ScaleY = OCRImageView.ActualHeight / bitmap.PixelHeight
                };

                foreach (var item in _wordBoxes)
                {
                    item.Transform(scaleTrasform);
                }
            }
        }
        private void PreviewImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWordBoxTransform();

            // Update image rotation center.
            var rotate = OCRImageView.RenderTransform as RotateTransform;
            if (rotate != null)
            {
                rotate.CenterX = OCRImageView.ActualWidth / 2;
                rotate.CenterY = OCRImageView.ActualHeight / 2;
            }
        }
        public void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }
        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;                
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;                
            }
        }
        private async Task ProcessTranslationAsync(string ocrResult)
        {
            string textToTranslate = ocrResult.Trim();
            // Get from language from the combobox
            var fromLanguage = FromLanguageComboBox.SelectedValue.ToString();
            
            string fromLanguageCode;

            // auto-detect source language if requested
            //if (fromLanguage == "Detect")
            //{
            //    fromLanguageCode = DetectLanguage(textToTranslate);
            //    if (!languageCodes.Contains(fromLanguageCode))
            //    {
            //        MessageBox.Show("The source language could not be detected automatically " +
            //            "or is not supported for translation.", "Language detection failed",
            //            MessageBoxButton.OK, MessageBoxImage.Error);
            //        return;
            //    }
            //}
            //else
                fromLanguageCode = _languageCodesAndTitles[fromLanguage];

            // Get to language from the combobox
            var toLanguage = ToLanguageComboBox.SelectedValue.ToString();
            var toLanguageCode = _languageCodesAndTitles[toLanguage];

            // handle null operations: no text or same source/target languages
            if (textToTranslate == "" || fromLanguageCode == toLanguageCode)
            {
                TranslatedTextBlock.Text = textToTranslate;
                return;
            }

            string uri = string.Format(uriBaseTranslation + "Translate?text=" +
                System.Net.WebUtility.UrlEncode(ocrResult) + "&from={0}&to={1}", fromLanguageCode, toLanguageCode);

            // send HTTP request to perform the translation
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", accesskeyTL);
            var response = await client.GetAsync(uri);

            // Parse the response XML
            Stream stream = await response.Content.ReadAsStreamAsync();
            StreamReader translatedStream = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
            System.Xml.XmlDocument xmlResponse = new System.Xml.XmlDocument();
            xmlResponse.LoadXml(translatedStream.ReadToEnd());

            // Update the translation field
            TranslatedTextBlock.Text = xmlResponse.InnerText;            
        }
        // DETECT LANGUAGE OF TEXT TO BE TRANSLATED
        //private string DetectLanguage(string text)
        //{
        //    string uri = uriBaseAnalysis + "languages?numberOfLanguagesToDetect=1";

        //    // create request to Text Analytics API
        //    HttpWebRequest detectLanguageWebRequest = (HttpWebRequest)WebRequest.Create(uri);
        //    detectLanguageWebRequest.Headers["Ocp-Apim-Subscription-Key"] = accesskeyTA;
        //    detectLanguageWebRequest.Method = "POST";

        //    // create and send body of request
        //    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        //    string jsonText = serializer.Serialize(text);

        //    string body = "{ \"documents\": [ { \"id\": \"0\", \"text\": " + jsonText + "} ] }";
        //    byte[] data = Encoding.UTF8.GetBytes(body);
        //    detectLanguageWebRequest.ContentLength = data.Length;

        //    using (var requestStream = detectLanguageWebRequest.GetRequestStream())
        //        requestStream.Write(data, 0, data.Length);

        //    HttpWebResponse response = (HttpWebResponse)detectLanguageWebRequest.GetResponse();

        //    // read and and parse JSON response
        //    var responseStream = response.GetResponseStream();
        //    var jsonString = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
        //    dynamic jsonResponse = serializer.DeserializeObject(jsonString);

        //    // fish out the detected language code
        //    var languageInfo = jsonResponse["documents"][0]["detectedLanguages"][0];
        //    if (languageInfo["score"] > (decimal)0.5)
        //        return languageInfo["iso6391Name"];
        //    else
        //        return "";
        //}
        // GET translatable langue codes
        private async Task GetLanguagesForTranslate()
        {
            // send request to get supported language codes
            string uri = uriBaseTranslation + "GetLanguagesForTranslate?scope=text";
            WebRequest request = WebRequest.Create(uri);
            request.Headers["Ocp-Apim-Subscription-Key"] = accesskeyTL;
            
            // read and parse the XML response
            WebResponse response = await request.GetResponseAsync();
            using (Stream stream = response.GetResponseStream())
            {
                System.Runtime.Serialization.DataContractSerializer dcs =
                    new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>));
                List<string> languagesForTranslate = (List<string>)dcs.ReadObject(stream);
                _languageCodes = languagesForTranslate.ToArray();
            }
        }
        private async Task GetLanguageNames()
        {
            // send request to get supported language names in English
            string uri = uriBaseTranslation + "GetLanguageNames?locale=en";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers["Ocp-Apim-Subscription-Key"] = accesskeyTL;
            request.ContentType = "text/xml";
            request.Method = "POST";
            System.Runtime.Serialization.DataContractSerializer dcs =
                new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String[]"));
            using (Stream stream = await request.GetRequestStreamAsync())
            {
                dcs.WriteObject(stream, _languageCodes);
            }                
            // read and parse the XML response
            var response = await request.GetResponseAsync();
            string[] languageNames;
            using (Stream stream = response.GetResponseStream())
            {
                languageNames = (string[])dcs.ReadObject(stream);
            }                
            //load the dictionary for the combo box
            for (int i = 0; i < languageNames.Length; i++)
                _languageCodesAndTitles.Add(languageNames[i], _languageCodes[i]);
        }
        private void PopulateLanguageMenus()
        {
            // Add option to automatically detect the source language
            FromLanguageComboBox.Items.Add("Detect");

            int count = _languageCodesAndTitles.Count;
            foreach (string menuItem in _languageCodesAndTitles.Keys)
            {
                FromLanguageComboBox.Items.Add(menuItem);
                ToLanguageComboBox.Items.Add(menuItem);
            }

            // set default languages
            FromLanguageComboBox.SelectedItem = "Detect";
            ToLanguageComboBox.SelectedItem = "English";
        }

        #endregion Helper functions

    }
    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };
    public enum CountryCode
    {

    }
    public class ComboBoxItem
    {
        // Auto-Impl Properties for trivial get and set
        public string Name { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ContentUrl { get; set; }
        public string HostPageUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Size { get { return Width + " x " + Height; } }
        public Thumbnail Thumbnail { get; set; }
    }
    public class ImageInfoViewModel
    {
        private ObservableCollection<ComboBoxItem> imageInfoCVS = new ObservableCollection<ComboBoxItem>();

        // Default Constructor
        public ImageInfoViewModel()
        {
            var defaultCount = 40;
            for (int i = 0; i < defaultCount; i++)
            {
                this.imageInfoCVS.Add(new ComboBoxItem()
                {
                    Name = "Default Box",
                    ThumbnailUrl = @"Assets\Square150x150Logo.scale-200.png"
                });
            }
        }
        // Add each imageResource into observable collection
        public ImageInfoViewModel(List<ComboBoxItem> imageResourceList)
        {
            foreach (ComboBoxItem imageResource in imageResourceList)
            {
                this.imageInfoCVS.Add(imageResource);
            }
        }

        public ObservableCollection<ComboBoxItem> ImageInfoCVS { get { return imageInfoCVS; } }
    }
    public class Thumbnail
    {
        public int width { get; set; }
        public int height { get; set; }
    }
}
