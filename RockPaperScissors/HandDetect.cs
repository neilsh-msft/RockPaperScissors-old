// https://github.com/zeruniverse/Gesture_Recognition/blob/master/Gesture_Recognition/handdetect.cpp

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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

namespace RockPaperScissors
{
    public static class CvExtensions
    {
        public static int Area(this Rect rect)
        {
            return rect.Height * rect.Width;
        }
    }

    class HandDetect
    {
        private Rect[] faces;

        public Mat myframe;

        public Rect? FaceDetect(Mat frame, CascadeClassifier faceClassifier)
        {
            Mat frame_gray;
            Rect? p = null;

            frame_gray = frame.CvtColor(ColorConversionCodes.BGR2GRAY).EqualizeHist();
            faces = faceClassifier.DetectMultiScale(frame_gray, 1.1, 2, HaarDetectionType.ScaleImage, new Size(30, 30));
            foreach (Rect face in faces)
            {
                Point center = new Point(face.X + face.Width * 0.5, face.Y + face.Height * 0.5);

                if (!p.HasValue)
                {
                    p = face;
                }
                else
                {
                    if (face.Area() > p.Value.Area())
                    {
                        p = face;
                    }

                    myframe.Ellipse(center, new Size(face.Width * 0.5, face.Height * 0.5), 0, 0, 360, new Scalar(255, 0, 255), 4, LineTypes.Link8, 0);
                }
            }
            return p;
        }

        public void SkinColorModel(Mat frame, Rect faceregion, out Vec3i max, out Vec3i min)
        {
            Mat p = frame.CvtColor(ColorConversionCodes.BGR2YCrCb);

            max = new Vec3i(295, -1, -1);
            min = new Vec3i(-1, 295, 295);

            if (faceregion.Area() > 5)
            {
                for (int i = faceregion.X; i < faceregion.X + faceregion.Width && i < frame.Cols; i++)
                {
                    for (int j = faceregion.Y; j < faceregion.Y + faceregion.Height && j < frame.Rows; j++)
                    {
                        Vec3i bgr = frame.At<Vec3i>(j, i);
                        Vec3i yCrCb = p.At<Vec3i>(j, i);
                        int gray = (int)(0.2989 * bgr.Item2 + 0.5870 * bgr.Item1 + 0.1140 * bgr.Item0);
                        if (gray < 200 && gray > 40 && bgr.Item2 > bgr.Item1 && bgr.Item2 > bgr.Item0)
                        {
                            max.Item0 = Math.Max(max.Item0, yCrCb.Item0);
                            max.Item1 = Math.Max(max.Item1, yCrCb.Item1);
                            max.Item2 = Math.Max(max.Item2, yCrCb.Item2);

                            min.Item0 = Math.Min(min.Item0, yCrCb.Item0);
                            min.Item1 = Math.Min(min.Item1, yCrCb.Item1);
                            min.Item2 = Math.Min(min.Item2, yCrCb.Item2);
                        }
                    }
                }
            }
            else
            {
                max = new Vec3i(255, 173, 127);
                min = new Vec3i(0, 133, 77);
            }
        }

        public void HandDetection(Mat frame, Rect faceRegion, out Vec3i maxYCrCb, out Vec3i minYCrCb)
        {
            maxRgb = new Vec3i(0, 0, 0);
            minRgp = new Vec3i(255, 255, 255);

            Size size = frame.Size();

            Mat mask = Mat.Zeros(size, MatType.CV_8UC1);

            if (faceRegion.Area() > 5)
            {
                if (faceRegion.Y > faceRegion.Height / 4)
                {
                    faceRegion.Y -= faceRegion.Height / 4;
                    faceRegion.Height += faceRegion.Height / 4;
                }
                else
                {
                    faceRegion.Height += faceRegion.Y;
                    faceRegion.Y = 0;
                }
                // avoid noise for T-shirt
                faceRegion.Height += faceRegion.Height / 2;
            }

            // Turn to YCrCb
            int y, cr, cb;
            Mat p, b;

            p = frame.CvtColor(ColorConversionCodes.BGR2YCrCb);

            for (int i = 0; i < frame.Cols; i++)
            {
                for (int j = 0; j < frame.Rows; j++)
                {
                    y = p.At<Vec3b>(j, i)[0];
                    cr = p.At<Vec3b>(j, i)[1];
                    cb = p.At<Vec3b>(j, i)[2];
                    if (y > minYCrCb.Item0 && y < maxYCrCb.Item0 &&
                        cr > minYCrCb.Item1 && cr < maxYCrCb.Item1 &&
                        cb > minYCrCb.Item2 && cb < maxYCrCb.Item2)
                    {
                        mask.Set<byte>(j, i, 255);
                    }

                    if (mybackground != null)
                    {
                        b = mybackground;
                        if (Math.Abs((int)frame.At<Vec3b>(j, i)[0] - (int)b.At<Vec3b>(j, i)[0]) < 10 &&
                            Math.Abs((int)frame.At<Vec3b>(j, i)[1] - (int)b.At<Vec3b>(j, i)[1]) < 10 &&
                            Math.Abs((int)frame.At<Vec3b>(j, i)[2] - (int)b.At<Vec3b>(j, i)[2]) < 10)
                        {
                            mask.Set<byte>(j, i, 0);
                        }
                    }
                }
            }

            foreach (Rect face in faces)
            {
                Cv2.Rectangle(mask, face, Scalar.Black);
            }

//            Cv2.Erode(mask, mask, )
        }

        public void GetPalmCenter()
        {

        }

        public void GetFingerTips()
        {

        }




    }
}
    