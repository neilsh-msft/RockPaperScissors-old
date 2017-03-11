﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Media.Capture;
using Windows.Storage;

using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

using Windows.UI.Xaml.Media.Imaging;


using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Media.MediaProperties;

using OpenCvSharp;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RockPaperScissors
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture _mediaCapture;
        private bool _isPreviewing;
        DisplayRequest _displayRequest;
        DispatcherTimer _dispatcherTimer;
        int _countDown;

        public MainPage()
        {
            this.InitializeComponent();
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            throw new NotImplementedException();
        }

        private async void dispatcherTimer_Tick(object sender, object e)
        {
            _countDown -= 200;
            textBlock.Text = _countDown.ToString();
            button.Content = Math.Ceiling(_countDown / 1000.0).ToString();

            if (_countDown <= -200)
            {
                _dispatcherTimer.Stop();

                // Prepare and capture photo
                var lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

                var capturedPhoto = await lowLagCapture.CaptureAsync();
                var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;
                await lowLagCapture.FinishAsync();

                SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied);

                Mat mat = SoftwareBitmapToMat(softwareBitmapBGR8);

                HandDetect detector = new HandDetect();

                CascadeClassifier faceClassifier;

                detector.myframe = mat;
                var haarCascade = new CascadeClassifier("Assets\\filters\\haarcascade_frontalface_alt.xml");

                // detect the hand
                Vec3i minYCrCb, maxYCrCb;
                Vec3i minRgb, maxRgb;

                Rect? faceRegion = detector.FaceDetect(mat, haarCascade);
                if (faceRegion.HasValue)
                {
                    detector.SkinColorModel(mat, faceRegion, out maxYCrCb, out minYCrCb);
                    detector.HandDetection(mat, faceRegion, out maxRgb, out minRgb);
                    detector.GetPalmCenter();
                    detector.GetFingerTips();
                }

                
                SoftwareBitmap result = MatToSoftwareBitmap(mat);
                SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
                await bitmapSource.SetBitmapAsync(result);
                capture.Source = bitmapSource;

                button.IsEnabled = true;
                button.Content = "Play";
            }
        }

        SoftwareBitmap MatToSoftwareBitmap(Mat image)
        {
            // Create the WriteableBitmap
            SoftwareBitmap result = new SoftwareBitmap(BitmapPixelFormat.Bgra8, image.Cols, image.Rows, BitmapAlphaMode.Premultiplied);
            
            byte[] bytes = new byte[image.Cols * image.Rows * 4];

            System.Runtime.InteropServices.Marshal.Copy(image.Data, bytes, 0, bytes.Length);
            
            result.CopyFromBuffer(bytes.AsBuffer());

            return result;
        }

        Mat SoftwareBitmapToMat(SoftwareBitmap image)
        {
            byte[] bytes = new byte[image.PixelHeight * image.PixelWidth * 4];
            image.CopyToBuffer(bytes.AsBuffer());
            Mat result = new Mat(image.PixelHeight, image.PixelWidth, MatType.CV_8UC4, bytes);

            return result;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                _mediaCapture.Failed += MediaCapture_Failed;

                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;

                //_displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                // start timer.  Period = 3 seconds, subtract 1 every seconds, capture at -200ms.
                _countDown = 3000;
                _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);

                _dispatcherTimer.Start();
                button.IsEnabled = false;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
            }
        }
    }
}
